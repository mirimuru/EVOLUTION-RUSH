using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一定間隔でプレイヤーの周囲に衝撃波を発生させる。
/// </summary>
public class ShockwaveAttack : MonoBehaviour
{
    [Header("Shockwave Settings")]
    [SerializeField] private float radius = 4.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float visualDuration = 0.4f;

    private float nextAttackTime;

    public int RadiusUpgradeLevel { get; private set; }
    public int DamageUpgradeLevel { get; private set; }
    public int CooldownUpgradeLevel { get; private set; }
    public bool BranchChosen { get; private set; }
    public bool IsEvolved { get; private set; }
    public int TotalUpgradeLevel => RadiusUpgradeLevel + DamageUpgradeLevel + CooldownUpgradeLevel;

    private void Start()
    {
        nextAttackTime = Time.time + 1f;
    }

    private void Update()
    {
        if (Time.time < nextAttackTime) return;

        PerformShockwave();
        nextAttackTime = Time.time + cooldown;
    }

    private void PerformShockwave()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !damagedEnemies.Add(enemy)) continue;

            enemy.TakeDamage(damage);
        }

        StartCoroutine(ShowShockwave());
    }

    private IEnumerator ShowShockwave()
    {
        GameObject effect = new GameObject("ShockwaveVisual");
        effect.transform.SetParent(transform, false);
        effect.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        LineRenderer line = effect.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 49;
        line.widthMultiplier = 0.12f;
        line.numCapVertices = 4;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = new Color(0.1f, 0.85f, 1f, 0.9f);
            line.material = material;
        }

        line.startColor = new Color(0.1f, 0.85f, 1f, 0.95f);
        line.endColor = new Color(0.1f, 0.85f, 1f, 0.15f);

        float elapsed = 0f;
        while (elapsed < visualDuration)
        {
            elapsed += Time.deltaTime;
            float currentRadius = Mathf.Lerp(0.3f, radius, Mathf.Clamp01(elapsed / visualDuration));

            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / (line.positionCount - 1);
                line.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0f,
                    Mathf.Sin(angle) * currentRadius
                ));
            }

            yield return null;
        }

        Destroy(effect);
    }

    public bool ApplyBranchUpgrade(bool wide)
    {
        if (BranchChosen) return false;
        if (wide) radius += 2f;
        else cooldown = Mathf.Max(0.65f, cooldown * 0.65f);
        BranchChosen = true;
        return true;
    }

    public bool ApplyEvolution()
    {
        if (!BranchChosen || IsEvolved) return false;
        radius += 1.5f; damage += 3;
        cooldown = Mathf.Max(0.5f, cooldown * 0.8f);
        IsEvolved = true;
        return true;
    }

    public bool ApplyRadiusUpgrade()
    {
        if (RadiusUpgradeLevel >= 4) return false;
        radius += 0.75f;
        RadiusUpgradeLevel++;
        return true;
    }

    public bool ApplyDamageUpgrade()
    {
        if (DamageUpgradeLevel >= 5) return false;
        damage++;
        DamageUpgradeLevel++;
        return true;
    }

    public bool ApplyCooldownUpgrade()
    {
        if (CooldownUpgradeLevel >= 5) return false;
        cooldown = Mathf.Max(0.8f, cooldown * 0.85f);
        CooldownUpgradeLevel++;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
