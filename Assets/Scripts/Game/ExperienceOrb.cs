using UnityEngine;

/// <summary>
/// 敵を倒したときに出現し、プレイヤーが触れると経験値を与えるオーブ。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ExperienceOrb : MonoBehaviour
{
    [SerializeField] private int experienceAmount = 1;
    [SerializeField] private float rotationSpeed = 100f;

    public void Initialize(int amount)
    {
        experienceAmount = Mathf.Max(1, amount);

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.7f;

        Rigidbody body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        transform.localScale = Vector3.one * 0.35f;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerExperience player = other.GetComponentInParent<PlayerExperience>();
        if (player == null)
        {
            return;
        }

        player.AddExperience(experienceAmount);
        Destroy(gameObject);
    }

    /// <summary>
    /// 経験値オーブを設定不要で生成する。
    /// </summary>
    public static void Create(Vector3 position, int amount)
    {
        GameObject orbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbObject.name = "ExperienceOrb";
        orbObject.transform.position = position;
        orbObject.transform.localScale = Vector3.one * 0.35f;

        Renderer orbRenderer = orbObject.GetComponent<Renderer>();
        if (orbRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.15f, 1f, 0.55f);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(0.05f, 1f, 0.25f) * 1.5f);
                }
                orbRenderer.material = material;
            }
        }

        ExperienceOrb orb = orbObject.AddComponent<ExperienceOrb>();
        orb.Initialize(amount);
    }
}
