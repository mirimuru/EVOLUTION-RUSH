using UnityEngine;

/// <summary>
/// シューターが撃つ弾。まっすぐ飛び、プレイヤーに近づいたらダメージを与える。
/// 当たり判定は距離で行うため、Colliderは不要。
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private const float Speed = 9f;
    private const float Lifetime = 4f;
    private const float HitRadius = 0.85f;

    private static Material sharedMaterial;

    private Vector3 direction;
    private Transform target;
    private PlayerHealth targetHealth;
    private int damage;
    private float age;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sharedMaterial = null;
    }

    public static void Fire(Vector3 position, Vector3 direction, Transform target, PlayerHealth targetHealth, int damage)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "EnemyProjectile";
        bullet.transform.position = position;
        bullet.transform.localScale = Vector3.one * 0.45f;

        Collider collider = bullet.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer bulletRenderer = bullet.GetComponent<Renderer>();
        Material material = GetMaterial();
        if (bulletRenderer != null && material != null) bulletRenderer.sharedMaterial = material;

        EnemyProjectile projectile = bullet.AddComponent<EnemyProjectile>();
        projectile.direction = direction.normalized;
        projectile.target = target;
        projectile.targetHealth = targetHealth;
        projectile.damage = damage;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += direction * (Speed * Time.deltaTime);

        if (target == null) return;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= HitRadius * HitRadius)
        {
            if (targetHealth != null) targetHealth.TakeDamage(damage);
            NeonEffects.Ring(transform.position, new Color(1f, 0.7f, 0.15f), 1.5f, 0.2f, 0.08f);
            Destroy(gameObject);
        }
    }

    private static Material GetMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        sharedMaterial = new Material(shader);
        Color baseColor = new Color(1f, 0.7f, 0.15f);
        sharedMaterial.color = baseColor;
        if (sharedMaterial.HasProperty("_BaseColor")) sharedMaterial.SetColor("_BaseColor", baseColor);
        if (sharedMaterial.HasProperty("_EmissionColor"))
        {
            sharedMaterial.EnableKeyword("_EMISSION");
            sharedMaterial.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.05f) * 3f);
        }

        return sharedMaterial;
    }
}
