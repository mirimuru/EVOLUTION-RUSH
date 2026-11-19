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
    // 弾ごとにMaterialを作ると解放されず増え続けるため、1つを共有する。
    private static Material trailMaterial;

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

        CreateNeonTrail();
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

    private void CreateNeonTrail()
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = 0.16f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0.015f;
        trail.minVertexDistance = 0.04f;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;

        if (trailMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                trailMaterial = new Material(shader);
                if (trailMaterial.HasProperty("_BaseColor"))
                    trailMaterial.SetColor("_BaseColor", new Color(0.05f, 0.75f, 1f, 0.85f));
                if (trailMaterial.HasProperty("_Color"))
                    trailMaterial.SetColor("_Color", new Color(0.05f, 0.75f, 1f, 0.85f));
            }
        }

        if (trailMaterial != null) trail.sharedMaterial = trailMaterial;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.05f, 0.35f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
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
        EnemyHitEffect.Create(other.ClosestPoint(transform.position));
        Destroy(gameObject);
    }
}
