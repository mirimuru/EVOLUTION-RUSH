using UnityEngine;

/// <summary>
/// プレイヤーを追いかける敵の基本AI。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float contactAttackRange = 1.25f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactAttackInterval = 1f;

    private Rigidbody rb;
    private Transform target;
    private float nextContactAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
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

        if (direction.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

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

        float currentMoveSpeed = moveSpeed * difficultyMultiplier;

        rb.linearVelocity = new Vector3(
            direction.x * currentMoveSpeed,
            rb.linearVelocity.y,
            direction.z * currentMoveSpeed
        );

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
