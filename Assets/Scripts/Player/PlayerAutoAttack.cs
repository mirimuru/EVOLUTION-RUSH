using UnityEngine;

/// <summary>
/// 一定範囲内で最も近い敵を自動攻撃する。
/// </summary>
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackInterval = 0.8f;
    [SerializeField] private int attackDamage = 1;

    private float nextAttackTime;

    private void Update()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        EnemyHealth target = FindNearestEnemy();

        if (target == null)
        {
            return;
        }

        target.TakeDamage(attackDamage);
        nextAttackTime = Time.time + attackInterval;
    }

    /// <summary>
    /// 攻撃範囲内にいる最も近い敵を探す。
    /// </summary>
    private EnemyHealth FindNearestEnemy()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        EnemyHealth nearestEnemy = null;
        float nearestDistanceSqr = attackRange * attackRange;

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;

            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr <= nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
