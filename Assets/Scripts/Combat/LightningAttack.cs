using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最も近い敵に雷撃し、近くの敵へ連鎖する自動攻撃。
/// </summary>
public class LightningAttack : MonoBehaviour
{
    [Header("Lightning Settings")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float chainRange = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int chainCount = 2;
    [SerializeField] private float cooldown = 2.5f;
    [SerializeField] private float visualDuration = 0.18f;
    [SerializeField] private float lineWidth = 0.09f;

    private float nextAttackTime;

    public int DamageUpgradeLevel { get; private set; }
    public int ChainUpgradeLevel { get; private set; }
    public int CooldownUpgradeLevel { get; private set; }
    public bool BranchChosen { get; private set; }
    public bool IsEvolved { get; private set; }
    public int TotalUpgradeLevel => DamageUpgradeLevel + ChainUpgradeLevel + CooldownUpgradeLevel;

    private void Start()
    {
        nextAttackTime = Time.time + 1f;
    }

    private void Update()
    {
        if (Time.time < nextAttackTime) return;

        // 1回の雷撃中は敵一覧を使い回し、連鎖ごとの全件検索を避ける。
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth firstTarget = FindNearestEnemy(enemies, transform.position, attackRange, null);
        if (firstTarget == null) return;

        List<EnemyHealth> targets = new List<EnemyHealth>();
        HashSet<EnemyHealth> selected = new HashSet<EnemyHealth>();

        EnemyHealth current = firstTarget;
        targets.Add(current);
        selected.Add(current);

        for (int i = 0; i < chainCount; i++)
        {
            EnemyHealth next = FindNearestEnemy(enemies, current.transform.position, chainRange, selected);
            if (next == null) break;

            targets.Add(next);
            selected.Add(next);
            current = next;
        }

        SoundManager.Play(SoundManager.Effect.Lightning);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null) targets[i].TakeDamage(damage);
        }

        StartCoroutine(ShowLightning(targets));
        nextAttackTime = Time.time + cooldown;
    }

    private EnemyHealth FindNearestEnemy(
        EnemyHealth[] enemies,
        Vector3 origin,
        float range,
        HashSet<EnemyHealth> excluded)
    {
        EnemyHealth nearest = null;
        float nearestDistanceSqr = range * range;

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || (excluded != null && excluded.Contains(enemy))) continue;

            Vector3 offset = enemy.transform.position - origin;
            offset.y = 0f;
            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr <= nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private IEnumerator ShowLightning(List<EnemyHealth> targets)
    {
        List<GameObject> effects = new List<GameObject>();

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;

            Vector3 start = i == 0
                ? transform.position + Vector3.up * 0.8f
                : targets[i - 1].transform.position + Vector3.up * 0.8f;
            Vector3 end = targets[i].transform.position + Vector3.up * 0.8f;

            GameObject effect = new GameObject("LightningVisual");
            LineRenderer line = effect.AddComponent<LineRenderer>();
            line.positionCount = 7;
            line.useWorldSpace = true;
            line.widthMultiplier = lineWidth * 1.8f;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.12f, 0.45f, 1f);
                line.material = material;
            }

            line.startColor = new Color(0.9f, 0.98f, 1f);
            line.endColor = new Color(0.35f, 0.2f, 1f);

            for (int point = 0; point < line.positionCount; point++)
            {
                float t = point / (float)(line.positionCount - 1);
                Vector3 position = Vector3.Lerp(start, end, t);

                if (point > 0 && point < line.positionCount - 1)
                {
                    position += new Vector3(
                        Random.Range(-0.35f, 0.35f),
                        Random.Range(-0.15f, 0.15f),
                        Random.Range(-0.35f, 0.35f)
                    );
                }

                line.SetPosition(point, position);
            }

            effects.Add(effect);
        }

        yield return new WaitForSeconds(visualDuration);

        foreach (GameObject effect in effects)
        {
            if (effect != null) Destroy(effect);
        }
    }

    public bool ApplyBranchUpgrade(bool chain)
    {
        if (BranchChosen) return false;
        if (chain) chainCount += 3;
        else damage += 3;
        BranchChosen = true;
        return true;
    }

    public bool ApplyEvolution()
    {
        if (!BranchChosen || IsEvolved) return false;
        damage += 3; chainCount += 2; chainRange += 1.5f;
        IsEvolved = true;
        return true;
    }

    public void ApplyPlasmaFusionBonus()
    {
        if (!IsEvolved) return;
        damage += 2;
        chainCount++;
        chainRange += 2f;
    }

    public bool ApplyDamageUpgrade()
    {
        if (DamageUpgradeLevel >= 5) return false;
        damage++;
        DamageUpgradeLevel++;
        return true;
    }

    public bool ApplyChainUpgrade()
    {
        if (ChainUpgradeLevel >= 4) return false;
        chainCount++;
        ChainUpgradeLevel++;
        return true;
    }

    public bool ApplyCooldownUpgrade()
    {
        if (CooldownUpgradeLevel >= 5) return false;
        cooldown = Mathf.Max(0.7f, cooldown * 0.85f);
        CooldownUpgradeLevel++;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chainRange);
    }
}
