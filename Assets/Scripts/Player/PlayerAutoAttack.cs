using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 操作難易度に応じて攻撃方式を切り替える。
/// EASY: 最も近い敵を自動攻撃
/// NORMAL: マウス方向へ自動攻撃
/// HARD: マウス方向へ左クリックで攻撃
/// </summary>
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 14f;
    [SerializeField] private float attackInterval = 0.8f;
    [SerializeField] private int attackDamage = 1;

    private float nextAttackTime;

    private void Update()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        GameSettings.OperationDifficulty mode = GameSettings.Instance != null
            ? GameSettings.Instance.OperationMode
            : GameSettings.OperationDifficulty.Normal;

        // HARDでは左クリックした瞬間だけ攻撃する。
        if (mode == GameSettings.OperationDifficulty.Hard)
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }
        }

        EnemyHealth target;

        if (mode == GameSettings.OperationDifficulty.Easy)
        {
            target = FindNearestEnemy();
        }
        else
        {
            target = FindAimedEnemy();
        }

        if (target == null)
        {
            return;
        }

        target.TakeDamage(attackDamage);
        nextAttackTime = Time.time + attackInterval;
    }

    /// <summary>
    /// 攻撃範囲内で最も近い敵を探す。
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

    /// <summary>
    /// マウスで指定した方向にいる敵を探す。
    /// NORMALは照準補助が広く、HARDは狭い。
    /// </summary>
    private EnemyHealth FindAimedEnemy()
    {
        Vector3 aimDirection = GetMouseAimDirection();

        if (aimDirection.sqrMagnitude < 0.001f)
        {
            return null;
        }

        float assistAngle = GameSettings.Instance != null
            ? GameSettings.Instance.AimAssistAngle
            : 14f;

        float minimumDot = Mathf.Cos(assistAngle * Mathf.Deg2Rad);
        float nearestDistanceSqr = attackRange * attackRange;
        EnemyHealth nearestEnemy = null;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;

            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > nearestDistanceSqr || distanceSqr < 0.001f)
            {
                continue;
            }

            Vector3 directionToEnemy = offset.normalized;
            float dot = Vector3.Dot(aimDirection, directionToEnemy);

            if (dot >= minimumDot)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// カメラからマウス位置へRayを飛ばし、プレイヤーと同じ高さの平面との交点を求める。
    /// </summary>
    private Vector3 GetMouseAimDirection()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return transform.forward;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        if (!aimPlane.Raycast(ray, out float enter))
        {
            return transform.forward;
        }

        Vector3 aimPoint = ray.GetPoint(enter);
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
