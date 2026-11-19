using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵のHPとダメージ処理を管理する。
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public static event System.Action<EnemyHealth> EnemyDefeated;

    /// <summary>
    /// 現在シーンに存在する敵の一覧。
    /// 攻撃スクリプトが毎回 FindObjectsByType で全検索しなくて済むようにする登録簿。
    /// </summary>
    public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveEnemies.Clear();
        cachedExperience = null;
    }

    private static PlayerExperience cachedExperience;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int experienceValue = 1;

    [Header("Hit Feedback")]
    [SerializeField] private float flashDuration = 0.08f;

    private int currentHealth;
    private Renderer[] cachedRenderers;
    private Material[][] cachedMaterials;
    private Color[][] originalColors;
    private Color[][] originalEmissions;
    private string[][] colorProperties;
    private Coroutine flashCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        float difficultyMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.EnemyHealthMultiplier
            : 1f;

        currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * difficultyMultiplier));
        CacheRendererMaterials();
    }

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    public void ApplyRunHealthMultiplier(float runMultiplier)
    {
        float difficultyMultiplier = GameSettings.Instance != null ? GameSettings.Instance.EnemyHealthMultiplier : 1f;
        currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * difficultyMultiplier * runMultiplier));
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 表示モデル（NeonCharacterVisuals）を生成した後に呼び、被弾フラッシュの対象を取り直す。
    /// Awake時点では表示モデルがまだ存在しないため、これがないとフラッシュが見えない。
    /// </summary>
    public void RefreshRenderers()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        ReleaseCachedMaterials();
        CacheRendererMaterials();
    }

    private void CacheRendererMaterials()
    {
        // 非表示にされた元のRendererはフラッシュ対象から外す。
        List<Renderer> visibleRenderers = new List<Renderer>();
        foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
        {
            if (candidate.enabled) visibleRenderers.Add(candidate);
        }

        cachedRenderers = visibleRenderers.ToArray();
        cachedMaterials = new Material[cachedRenderers.Length][];
        originalColors = new Color[cachedRenderers.Length][];
        originalEmissions = new Color[cachedRenderers.Length][];
        colorProperties = new string[cachedRenderers.Length][];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            // materialsを取得して、この敵専用のマテリアルとして扱う。
            cachedMaterials[i] = cachedRenderers[i].materials;
            originalColors[i] = new Color[cachedMaterials[i].Length];
            originalEmissions[i] = new Color[cachedMaterials[i].Length];
            colorProperties[i] = new string[cachedMaterials[i].Length];

            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];

                if (material.HasProperty("_BaseColor"))
                {
                    colorProperties[i][j] = "_BaseColor";
                    originalColors[i][j] = material.GetColor("_BaseColor");
                }
                else if (material.HasProperty("_Color"))
                {
                    colorProperties[i][j] = "_Color";
                    originalColors[i][j] = material.GetColor("_Color");
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    originalEmissions[i][j] = material.GetColor("_EmissionColor");
                }
            }
        }
    }

    /// <summary>
    /// 指定された量のダメージを受ける。
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            SoundManager.Play(SoundManager.Effect.EnemyDefeated);
            Die();
            return;
        }

        SoundManager.Play(SoundManager.Effect.EnemyHit);

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            RestoreRendererColors();
        }

        flashCoroutine = StartCoroutine(FlashWhenDamaged());
    }

    private IEnumerator FlashWhenDamaged()
    {
        SetRendererColors(Color.white);
        yield return new WaitForSeconds(flashDuration);
        RestoreRendererColors();
        flashCoroutine = null;
    }

    private void SetRendererColors(Color color)
    {
        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];
                if (material == null) continue;

                string property = colorProperties[i][j];
                if (!string.IsNullOrEmpty(property))
                {
                    material.SetColor(property, color);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 1.5f);
                }
            }
        }
    }

    private void RestoreRendererColors()
    {
        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];
                if (material == null) continue;

                string property = colorProperties[i][j];
                if (!string.IsNullOrEmpty(property))
                {
                    material.SetColor(property, originalColors[i][j]);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", originalEmissions[i][j]);
                }
            }
        }
    }

    private void Die()
    {
        EnemyDefeated?.Invoke(this);
        NeonEffects.EnemyBurst(transform.position + Vector3.up * 0.5f, GetComponent<BossAI>() != null);

        // 敵を倒した瞬間に経験値を直接加算する。検索結果は使い回す。
        if (cachedExperience == null)
        {
            cachedExperience = FindFirstObjectByType<PlayerExperience>();
        }

        if (cachedExperience != null)
        {
            cachedExperience.AddExperience(experienceValue);
        }
        else
        {
            Debug.LogWarning("PlayerExperienceが見つからないため、経験値を加算できません。");
        }

        Destroy(gameObject);
    }

    private void ReleaseCachedMaterials()
    {
        // Renderer.materialsで生成した敵専用マテリアルを解放する。
        if (cachedMaterials == null) return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null) continue;

            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        cachedMaterials = null;
    }

    private void OnDestroy()
    {
        ReleaseCachedMaterials();
    }
}
