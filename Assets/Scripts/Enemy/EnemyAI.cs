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

    /// <summary>通常敵の数（ボスは含まない）。スポーン数の制限に使う。</summary>
    public static int ActiveCount { get; private set; }

    private Rigidbody rb;
    private Transform target;
    private PlayerHealth targetHealth;
    private PlayerController targetController;
    private float nextContactAttackTime;
    private float runSpeedMultiplier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ActiveCount++;
    }

    private void OnDisable()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // 毎フレームGetComponentしないよう、参照を保持する。
        targetHealth = newTarget != null ? newTarget.GetComponent<PlayerHealth>() : null;
        targetController = newTarget != null ? newTarget.GetComponent<PlayerController>() : null;
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
                if (targetHealth == null) targetHealth = target.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(contactDamage);
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
        if (targetController == null) targetController = target.GetComponent<PlayerController>();
        if (targetController != null)
        {
            currentMoveSpeed = Mathf.Min(currentMoveSpeed, targetController.MoveSpeed * 0.9f);
        }

        rb.linearVelocity = new Vector3(
            direction.x * currentMoveSpeed,
            rb.linearVelocity.y,
            direction.z * currentMoveSpeed
        );

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
