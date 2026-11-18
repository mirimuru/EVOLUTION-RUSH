using UnityEngine;

/// <summary>
/// プレイヤーのHPを管理する。敵との接触でダメージを受ける。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || GameSessionManager.Instance == null ||
            !GameSessionManager.Instance.IsRunning)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            isDead = true;
            GameSessionManager.Instance.EndRun(false);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }
}
