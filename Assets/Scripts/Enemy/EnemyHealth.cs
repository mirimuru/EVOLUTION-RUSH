using System.Collections;
using UnityEngine;

/// <summary>
/// 敵のHPとダメージ処理を管理する。
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public static event System.Action<EnemyHealth> EnemyDefeated;
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int experienceValue = 1;

    [Header("Hit Feedback")]
    [SerializeField] private float flashDuration = 0.08f;

    private int currentHealth;
    private Renderer[] cachedRenderers;
    private Material[][] cachedMaterials;
    private Color[][] originalColors;
    private string[][] colorProperties;
    private Coroutine flashCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        float difficultyMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.EnemyHealthMultiplier
            : 1f;

        currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * difficultyMultiplier));
        CacheRendererMaterials();
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

    private void CacheRendererMaterials()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedMaterials = new Material[cachedRenderers.Length][];
        originalColors = new Color[cachedRenderers.Length][];
        colorProperties = new string[cachedRenderers.Length][];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            // materialsを取得して、この敵専用のマテリアルとして扱う。
            cachedMaterials[i] = cachedRenderers[i].materials;
            originalColors[i] = new Color[cachedMaterials[i].Length];
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
        Debug.Log($"{gameObject.name} は {damage} ダメージを受けた。残りHP: {currentHealth}");

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
                string property = colorProperties[i][j];
                if (!string.IsNullOrEmpty(property))
                {
                    cachedMaterials[i][j].SetColor(property, color);
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
                string property = colorProperties[i][j];
                if (!string.IsNullOrEmpty(property))
                {
                    cachedMaterials[i][j].SetColor(property, originalColors[i][j]);
                }
            }
        }
    }

    private void Die()
    {
        EnemyDefeated?.Invoke(this);

        // 敵を倒した位置に経験値オーブを出現させる。
        ExperienceOrb.Create(transform.position + Vector3.up * 0.5f, experienceValue);
        Destroy(gameObject);
    }

    private void OnDestroy()
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
    }
}
