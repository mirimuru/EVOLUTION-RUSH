using UnityEngine;

/// <summary>
/// 敵のHPとダメージ処理を管理する。
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;

    private int currentHealth;

    private void Awake()
    {
        float difficultyMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.EnemyHealthMultiplier
            : 1f;

        currentHealth = Mathf.Max(1, Mathf.CeilToInt(maxHealth * difficultyMultiplier));
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
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
