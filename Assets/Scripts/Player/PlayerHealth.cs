using UnityEngine;

/// <summary>
/// プレイヤーのHPを管理する。敵との接触でダメージを受ける。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageInvulnerabilityTime = 0.35f;

    private int currentHealth;
    private float nextDamageTime;
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
        if (damage <= 0 || isDead || Time.time < nextDamageTime ||
            GameSessionManager.Instance == null || !GameSessionManager.Instance.IsRunning)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        nextDamageTime = Time.time + damageInvulnerabilityTime;

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
        nextDamageTime = 0f;
    }
}
