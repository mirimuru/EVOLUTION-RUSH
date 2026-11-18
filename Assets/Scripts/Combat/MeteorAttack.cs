using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵が密集している場所を狙って隕石を落とし、着弾地点に範囲ダメージを与える。
/// </summary>
public class MeteorAttack : MonoBehaviour
{
    [Header("Meteor Settings")]
    [SerializeField] private int damage = 3;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private int meteorCount = 1;
    [SerializeField] private float targetRange = 14f;   // プレイヤーからこの距離内の敵を狙う
    [SerializeField] private float warningTime = 0.8f;  // 着弾予告の表示時間
    [SerializeField] private float fallTime = 0.35f;    // 隕石が落ちる時間
    [SerializeField] private float spawnHeight = 14f;

    private float nextAttackTime;

    public int CountUpgradeLevel { get; private set; }
    public int RadiusUpgradeLevel { get; private set; }
    public int DamageUpgradeLevel { get; private set; }
    public int CooldownUpgradeLevel { get; private set; }
    public bool BranchChosen { get; private set; }
    public bool IsEvolved { get; private set; }
    public int TotalUpgradeLevel => CountUpgradeLevel + RadiusUpgradeLevel + DamageUpgradeLevel + CooldownUpgradeLevel;

    private void Start()
    {
        nextAttackTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (Time.time < nextAttackTime) return;

        if (LaunchMeteors())
        {
            nextAttackTime = Time.time + cooldown;
        }
        else
        {
            // 敵がいなければ少し待って再判定する。
            nextAttackTime = Time.time + 0.5f;
        }
    }

    private bool LaunchMeteors()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        List<Vector3> candidates = new List<Vector3>();
        foreach (EnemyHealth e in enemies)
        {
            Vector3 p = e.transform.position;
            p.y = 0f;
            Vector3 self = transform.position;
            self.y = 0f;
            if ((p - self).sqrMagnitude <= targetRange * targetRange) candidates.Add(p);
        }
        if (candidates.Count == 0) return false;

        List<Vector3> usedTargets = new List<Vector3>();
        for (int n = 0; n < meteorCount; n++)
        {
            Vector3 best = Vector3.zero;
            int bestScore = -1;

            foreach (Vector3 c in candidates)
            {
                // すでに別のメテオが狙った場所の近くは避ける。
                bool tooClose = false;
                foreach (Vector3 u in usedTargets)
                {
                    if ((u - c).sqrMagnitude < explosionRadius * explosionRadius) { tooClose = true; break; }
                }
                if (tooClose) continue;

                int score = 0;
                foreach (Vector3 other in candidates)
                {
                    if ((other - c).sqrMagnitude <= explosionRadius * explosionRadius) score++;
                }
                if (score > bestScore) { bestScore = score; best = c; }
            }

            if (bestScore < 0) break;
            usedTargets.Add(best);
            StartCoroutine(MeteorRoutine(best));
        }

        return usedTargets.Count > 0;
    }

    private IEnumerator MeteorRoutine(Vector3 target)
    {
        // 着弾予告の紫色ネオンリング
        GameObject warning = new GameObject("MeteorWarning");
        warning.transform.position = new Vector3(target.x, 0.2f, target.z);
        LineRenderer line = warning.AddComponent<LineRenderer>();
        SetupLine(line, new Color(0.72f, 0.12f, 1f, 0.95f), 0.13f);
        DrawCircle(line, explosionRadius);

        // 落下する隕石
        GameObject meteor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meteor.name = "Meteor";
        Collider col = meteor.GetComponent<Collider>();
        if (col != null) Destroy(col);
        meteor.transform.localScale = Vector3.one * 1.2f;
        SetColor(meteor, new Color(0.45f, 0.12f, 0.95f));
        Vector3 start = new Vector3(target.x, spawnHeight, target.z);
        meteor.transform.position = start;

        // 予告時間の間、予告円を点滅させる。
        float t = 0f;
        while (t < warningTime)
        {
            t += Time.deltaTime;
            float a = 0.4f + 0.5f * Mathf.Abs(Mathf.Sin(t * 12f));
            line.startColor = line.endColor = new Color(0.65f, 0.12f, 1f, a);
            yield return null;
        }

        // 落下
        t = 0f;
        while (t < fallTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fallTime);
            meteor.transform.position = Vector3.Lerp(start, new Vector3(target.x, 0.5f, target.z), k * k);
            yield return null;
        }

        Destroy(meteor);
        Destroy(warning);
        Explode(target);
    }

    private void Explode(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius);
        HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();
        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !damaged.Add(enemy)) continue;
            enemy.TakeDamage(damage);
        }

        StartCoroutine(ExplosionEffect(center));
    }

    private IEnumerator ExplosionEffect(Vector3 center)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "MeteorExplosion";
        Collider col = fx.GetComponent<Collider>();
        if (col != null) Destroy(col);
        fx.transform.position = new Vector3(center.x, 0.4f, center.z);

        float duration = 0.35f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fx.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, explosionRadius * 2f, k);
            SetColor(fx, Color.Lerp(new Color(0.2f, 0.9f, 1f), new Color(0.65f, 0.08f, 1f), k));
            yield return null;
        }
        Destroy(fx);
    }

    private static void SetupLine(LineRenderer line, Color color, float width)
    {
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 49;
        line.widthMultiplier = width;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) line.material = new Material(shader);
        line.startColor = color;
        line.endColor = color;
    }

    private static void DrawCircle(LineRenderer line, float radius)
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / (line.positionCount - 1);
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private static void SetColor(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 2.5f);
        }
    }

    public bool ApplyBranchUpgrade(bool rain)
    {
        if (BranchChosen) return false;
        if (rain) meteorCount = Mathf.Min(6, meteorCount + 2);
        else explosionRadius += 1.5f;
        BranchChosen = true;
        return true;
    }

    public bool ApplyEvolution()
    {
        if (!BranchChosen || IsEvolved) return false;
        damage += 4; explosionRadius += 1f;
        meteorCount = Mathf.Min(8, meteorCount + 1);
        cooldown = Mathf.Max(1.0f, cooldown * 0.8f);
        IsEvolved = true;
        return true;
    }

    public bool ApplyCountUpgrade()
    {
        if (CountUpgradeLevel >= 3) return false;
        meteorCount++;
        CountUpgradeLevel++;
        return true;
    }

    public bool ApplyRadiusUpgrade()
    {
        if (RadiusUpgradeLevel >= 4) return false;
        explosionRadius += 0.5f;
        RadiusUpgradeLevel++;
        return true;
    }

    public bool ApplyDamageUpgrade()
    {
        if (DamageUpgradeLevel >= 5) return false;
        damage += 2;
        DamageUpgradeLevel++;
        return true;
    }

    public bool ApplyCooldownUpgrade()
    {
        if (CooldownUpgradeLevel >= 5) return false;
        cooldown = Mathf.Max(1.5f, cooldown * 0.85f);
        CooldownUpgradeLevel++;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, targetRange);
    }
}
