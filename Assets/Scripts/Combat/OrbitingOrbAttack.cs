using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの周囲を回転するオーブによる接触攻撃。
/// </summary>
public class OrbitingOrbAttack : MonoBehaviour
{
    [Header("Orb Settings")]
    [SerializeField] private int orbCount = 1;
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float orbSize = 0.65f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitInterval = 0.6f;

    private readonly List<GameObject> orbs = new List<GameObject>();
    private readonly Dictionary<EnemyHealth, float> nextHitTimes = new Dictionary<EnemyHealth, float>();
    private float angle;

    public int OrbCountUpgradeLevel { get; private set; }
    public int DamageUpgradeLevel { get; private set; }
    public int SpeedUpgradeLevel { get; private set; }
    public bool BranchChosen { get; private set; }
    public bool IsEvolved { get; private set; }
    public int TotalUpgradeLevel => OrbCountUpgradeLevel + DamageUpgradeLevel + SpeedUpgradeLevel;

    private void Start()
    {
        RebuildOrbs();
    }

    private void Update()
    {
        if (orbs.Count == 0)
        {
            RebuildOrbs();
        }

        angle += rotationSpeed * Time.deltaTime;

        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;

            float orbAngle = (angle + 360f * i / orbs.Count) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(orbAngle), 0.65f, Mathf.Sin(orbAngle)) * orbitRadius;
            orbs[i].transform.position = transform.position + offset;
        }
    }

    private void RebuildOrbs()
    {
        foreach (GameObject orb in orbs)
        {
            if (orb != null) Destroy(orb);
        }
        orbs.Clear();

        for (int i = 0; i < orbCount; i++)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "OrbitingOrb";
            orb.transform.localScale = Vector3.one * orbSize;

            Renderer renderer = orb.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.color = new Color(0.1f, 0.85f, 1f);
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(0.05f, 0.5f, 1f) * 2f);
                    }
                    renderer.material = material;
                }
            }

            SphereCollider collider = orb.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody body = orb.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;

            OrbitingOrbHitbox hitbox = orb.AddComponent<OrbitingOrbHitbox>();
            hitbox.Initialize(this);

            orbs.Add(orb);
        }
    }

    private void OnDisable()
    {
        foreach (GameObject orb in orbs)
        {
            if (orb != null) Destroy(orb);
        }
        orbs.Clear();
    }

    public void HandleOrbContact(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;

        if (nextHitTimes.TryGetValue(enemy, out float nextTime) && Time.time < nextTime)
        {
            return;
        }

        enemy.TakeDamage(damage);
        nextHitTimes[enemy] = Time.time + hitInterval;
    }

    public bool ApplyBranchUpgrade(bool addOrbs)
    {
        if (BranchChosen) return false;
        if (addOrbs) { orbCount = Mathf.Min(8, orbCount + 2); RebuildOrbs(); }
        else { damage += 3; orbitRadius += 0.4f; }
        BranchChosen = true;
        return true;
    }

    public bool ApplyEvolution()
    {
        if (!BranchChosen || IsEvolved) return false;
        damage += 3; orbitRadius += 0.6f;
        rotationSpeed = Mathf.Min(480f, rotationSpeed * 1.35f);
        IsEvolved = true;
        return true;
    }

    public bool ApplyOrbCountUpgrade()
    {
        if (OrbCountUpgradeLevel >= 3) return false;
        orbCount++;
        OrbCountUpgradeLevel++;
        RebuildOrbs();
        return true;
    }

    public bool ApplyDamageUpgrade()
    {
        if (DamageUpgradeLevel >= 5) return false;
        damage++;
        DamageUpgradeLevel++;
        return true;
    }

    public bool ApplySpeedUpgrade()
    {
        if (SpeedUpgradeLevel >= 5) return false;
        rotationSpeed = Mathf.Min(360f, rotationSpeed * 1.2f);
        SpeedUpgradeLevel++;
        return true;
    }

}
