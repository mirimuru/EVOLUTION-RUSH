using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 操作難易度に応じてエネルギー弾の発射方式を切り替える。
/// EASY: 最も近い敵へ自動照準・自動発射
/// NORMAL: マウス方向へ照準・自動発射（弱い照準補助あり）
/// HARD: マウス方向へ照準・左クリックで発射
/// </summary>
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private EnergyProjectile projectilePrefab;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private float projectileSpeed = 16f;
    [SerializeField] private float projectileLifetime = 1.2f;
    [SerializeField] private float projectileSpawnOffset = 0.8f;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 14f;
    [SerializeField] private float attackInterval = 0.8f;
    [SerializeField] private int attackDamage = 1;

    private float nextAttackTime;

    private void Update()
    {
        GameSettings.OperationDifficulty mode = GameSettings.Instance != null
            ? GameSettings.Instance.OperationMode
            : GameSettings.OperationDifficulty.Normal;

        if (mode == GameSettings.OperationDifficulty.Hard)
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        Vector3 attackDirection;

        if (mode == GameSettings.OperationDifficulty.Easy)
        {
            EnemyHealth target = FindNearestEnemy();
            if (target == null)
            {
                return;
            }

            attackDirection = GetDirectionTo(target.transform.position);
        }
        else
        {
            attackDirection = GetMouseAimDirection();

            // NORMAL/HARDは照準方向に敵がいれば、少しだけ敵側へ補正する。
            EnemyHealth assistedTarget = FindAimedEnemy(attackDirection);
            if (assistedTarget != null)
            {
                attackDirection = GetDirectionTo(assistedTarget.transform.position);
            }
        }

        if (attackDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        FireProjectile(attackDirection);
        nextAttackTime = Time.time + attackInterval;
    }

    private void FireProjectile(Vector3 direction)
    {
        Vector3 spawnPosition;

        if (muzzlePoint != null)
        {
            spawnPosition = muzzlePoint.position;
        }
        else
        {
            spawnPosition = transform.position + Vector3.up * 0.7f + direction * projectileSpawnOffset;
        }

        EnergyProjectile projectile;

        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        }
        else
        {
            // Prefab未設定でも動作確認できるよう、青い球体をその場で生成する。
            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "EnergyProjectile_Runtime";
            projectileObject.transform.position = spawnPosition;
            projectileObject.transform.localScale = Vector3.one * 0.35f;

            Renderer projectileRenderer = projectileObject.GetComponent<Renderer>();
            if (projectileRenderer != null)
            {
                Shader projectileShader = Shader.Find("Universal Render Pipeline/Lit");
                if (projectileShader == null)
                {
                    projectileShader = Shader.Find("Standard");
                }

                if (projectileShader != null)
                {
                    Material material = new Material(projectileShader);
                    material.color = new Color(0.1f, 0.65f, 1f);

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(0.05f, 0.35f, 1f) * 2f);
                    }

                    projectileRenderer.material = material;
                }
            }

            SphereCollider sphereCollider = projectileObject.GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            projectileObject.AddComponent<Rigidbody>();
            projectile = projectileObject.AddComponent<EnergyProjectile>();
        }

        // 自分自身に弾が当たらないようにする。
        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = projectile.GetComponents<Collider>();

        foreach (Collider playerCollider in playerColliders)
        {
            foreach (Collider projectileCollider in projectileColliders)
            {
                Physics.IgnoreCollision(playerCollider, projectileCollider);
            }
        }

        projectile.Initialize(direction, projectileSpeed, attackDamage, projectileLifetime);
    }

    private Vector3 GetDirectionTo(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

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

    private EnemyHealth FindAimedEnemy(Vector3 aimDirection)
    {
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

            float dot = Vector3.Dot(aimDirection, offset.normalized);
            if (dot >= minimumDot && distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (muzzlePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(muzzlePoint.position, 0.12f);
        }
    }
}
