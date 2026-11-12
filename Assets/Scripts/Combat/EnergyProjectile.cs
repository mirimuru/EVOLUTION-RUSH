using UnityEngine;

/// <summary>
/// プレイヤーが発射するエネルギー弾。
/// 敵に触れるとダメージを与えて消滅する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class EnergyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 16f;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private int damage = 1;

    private Rigidbody rb;
    private bool initialized;
    private bool hasHit;

    private void Awake()
    {
        SetupPhysics();
    }

    private void SetupPhysics()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider hitbox in colliders)
        {
            hitbox.isTrigger = true;
        }
    }

    /// <summary>
    /// 発射時に攻撃方向と威力を設定する。
    /// </summary>
    public void Initialize(Vector3 direction, float projectileSpeed, int projectileDamage, float projectileLifetime)
    {
        if (rb == null)
        {
            SetupPhysics();
        }

        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = projectileLifetime;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction);
        rb.linearVelocity = direction * speed;

        initialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        // PrefabをInspectorで設定し忘れた場合にも停止しないための保険。
        if (!initialized)
        {
            Initialize(transform.forward, speed, damage, lifetime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
        {
            return;
        }

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null)
        {
            return;
        }

        hasHit = true;
        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}
