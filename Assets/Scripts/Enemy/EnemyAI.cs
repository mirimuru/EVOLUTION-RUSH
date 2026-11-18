using UnityEngine;

/// <summary>
/// プレイヤーを追いかける敵の基本AI。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.4f;
    [SerializeField] private float contactAttackRange = 1.25f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactAttackInterval = 1f;

    private Rigidbody rb;
    private Transform target;
    private float nextContactAttackTime;
    private float runSpeedMultiplier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetRunSpeedMultiplier(float multiplier)
    {
        runSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        float distanceSqr = direction.sqrMagnitude;
        if (distanceSqr <= contactAttackRange * contactAttackRange)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (Time.time >= nextContactAttackTime)
            {
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                }

                nextContactAttackTime = Time.time + contactAttackInterval;
            }

            return;
        }

        direction.Normalize();

        float difficultyMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.EnemyMoveSpeedMultiplier
            : 1f;

        float currentMoveSpeed = moveSpeed * difficultyMultiplier * runSpeedMultiplier;

        // プレイヤーより少し遅い速度に制限し、逃げ切れる余地を確保する。
        PlayerController playerController = target.GetComponent<PlayerController>();
        if (playerController != null)
        {
            currentMoveSpeed = Mathf.Min(currentMoveSpeed, playerController.MoveSpeed * 0.9f);
        }

        rb.linearVelocity = new Vector3(
            direction.x * currentMoveSpeed,
            rb.linearVelocity.y,
            direction.z * currentMoveSpeed
        );

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
