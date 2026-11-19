using UnityEngine;

/// <summary>敵の種類。出現する種類は時間とともに増える。</summary>
public enum EnemyType
{
    Grunt,    // 基本の敵：まっすぐ追いかける
    Runner,   // 小型・高速・低HP
    Shooter,  // 距離を取って弾を撃つ
    Brute     // 大型・低速・高HP・高ダメージ
}

/// <summary>
/// プレイヤーを追いかける敵の基本AI。種類（EnemyType）によって動きと能力が変わる。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.4f;
    [SerializeField] private float contactAttackRange = 1.25f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactAttackInterval = 1f;

    [Header("Shooter Settings")]
    [SerializeField] private int projectileDamage = 8;
    [SerializeField] private float shotInterval = 2.2f;
    [SerializeField] private float shooterMinRange = 5.5f;
    [SerializeField] private float shooterMaxRange = 9f;
    [SerializeField] private float shooterFireRange = 13f;

    /// <summary>通常敵の数（ボスは含まない）。スポーン数の制限に使う。</summary>
    public static int ActiveCount { get; private set; }

    private Rigidbody rb;
    private Transform target;
    private PlayerHealth targetHealth;
    private PlayerController targetController;
    private float nextContactAttackTime;
    private float nextShotTime;
    private float runSpeedMultiplier = 1f;
    private float typeSpeedScale = 1f;
    private float speedCapRatio = 0.9f;

    public EnemyType Type { get; private set; } = EnemyType.Grunt;

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

    /// <summary>敵の種類に応じて、速度・攻撃力などを設定する。生成直後に1回呼ぶ。</summary>
    public void Configure(EnemyType newType)
    {
        Type = newType;

        switch (newType)
        {
            case EnemyType.Runner:
                typeSpeedScale = 1.7f;
                speedCapRatio = 1.1f;   // プレイヤーより少し速い。ただし体力は低い。
                contactDamage = Mathf.RoundToInt(contactDamage * 0.6f);
                break;
            case EnemyType.Brute:
                typeSpeedScale = 0.65f;
                contactDamage = Mathf.RoundToInt(contactDamage * 2f);
                break;
            case EnemyType.Shooter:
                typeSpeedScale = 0.85f;
                nextShotTime = Time.time + Random.Range(1f, 2.5f);
                break;
        }
    }

    private float GetCurrentMoveSpeed()
    {
        float difficultyMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.EnemyMoveSpeedMultiplier
            : 1f;

        float speed = moveSpeed * difficultyMultiplier * runSpeedMultiplier * typeSpeedScale;

        // プレイヤーより少し遅い速度に制限し、逃げ切れる余地を確保する。
        if (targetController == null) targetController = target.GetComponent<PlayerController>();
        if (targetController != null)
        {
            speed = Mathf.Min(speed, targetController.MoveSpeed * speedCapRatio);
        }

        return speed;
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

        if (Type == EnemyType.Shooter)
        {
            HandleShooter(direction);
            return;
        }

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
        float currentMoveSpeed = GetCurrentMoveSpeed();

        rb.linearVelocity = new Vector3(
            direction.x * currentMoveSpeed,
            rb.linearVelocity.y,
            direction.z * currentMoveSpeed
        );

        transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>シューター：近すぎれば下がり、遠すぎれば近づき、射程内なら弾を撃つ。</summary>
    private void HandleShooter(Vector3 toTarget)
    {
        float distance = toTarget.magnitude;
        if (distance < 0.01f) return;

        Vector3 direction = toTarget / distance;
        float speed = GetCurrentMoveSpeed();

        Vector3 move = Vector3.zero;
        if (distance > shooterMaxRange) move = direction;
        else if (distance < shooterMinRange) move = -direction;

        rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);
        transform.rotation = Quaternion.LookRotation(direction);

        if (distance <= shooterFireRange && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + shotInterval;
            if (targetHealth == null) targetHealth = target.GetComponent<PlayerHealth>();
            EnemyProjectile.Fire(transform.position + direction * 0.8f + Vector3.up * 0.2f,
                direction, target, targetHealth, projectileDamage);
        }
    }
}
