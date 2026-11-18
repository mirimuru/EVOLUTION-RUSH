using UnityEngine;

/// <summary>
/// プレイヤーの周囲に敵を一定間隔で生成する。
/// ゲーム難易度に応じて生成間隔と最大数を調整する。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnMinDistance = 9f;
    [SerializeField] private float spawnMaxDistance = 12f;
    [SerializeField] private int maxEnemies = 12;

    [Header("Boss Settings")]
    [SerializeField] private int bossHealth = 120;
    [SerializeField] private float bossSpawnDistance = 15f;

    private float spawnTimer;
    private bool bossSpawned;

    private void Update()
    {
        if (enemyPrefab == null || player == null)
        {
            return;
        }

        GameSessionManager session = GameSessionManager.Instance;
        if (session != null && !session.IsRunning) return;

        float progress = session != null ? session.Progress : 0f;
        if (!bossSpawned && session != null && session.BossPhaseStarted)
        {
            SpawnBoss();
            bossSpawned = true;
        }

        spawnTimer += Time.deltaTime;
        float intervalMultiplier = GameSettings.Instance != null ? GameSettings.Instance.SpawnIntervalMultiplier : 1f;
        float countMultiplier = GameSettings.Instance != null ? GameSettings.Instance.EnemyCountMultiplier : 1f;
        float timeSpawnMultiplier = Mathf.Lerp(1f, 0.55f, progress);
        float timeCountMultiplier = Mathf.Lerp(1f, 1.8f, progress);
        float currentSpawnInterval = Mathf.Max(0.25f, spawnInterval * intervalMultiplier * timeSpawnMultiplier);
        int currentMaxEnemies = Mathf.Max(1, Mathf.CeilToInt(maxEnemies * countMultiplier * timeCountMultiplier));

        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;

            if (FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length < currentMaxEnemies)
            {
                SpawnEnemy(progress);
            }
        }
    }

    public void ResetSpawner()
    {
        spawnTimer = 0f;
        bossSpawned = false;
    }

    private void SpawnBoss()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        Vector3 position = player.position + new Vector3(direction.x * bossSpawnDistance, 1.5f, direction.y * bossSpawnDistance);

        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boss.name = "EVOLUTION RUSH BOSS";
        boss.transform.position = position;
        boss.transform.localScale = new Vector3(2.4f, 1.5f, 2.4f);
        Renderer renderer = boss.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = new Color(0.55f, 0.12f, 0.85f);

        Rigidbody body = boss.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        NeonCharacterVisuals bossVisuals = boss.AddComponent<NeonCharacterVisuals>();
        bossVisuals.BuildBoss();

        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.SetMaxHealth(bossHealth);
        BossAI bossAI = boss.AddComponent<BossAI>();
        bossAI.SetTarget(player);
    }

    private void SpawnEnemy(float progress)
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection.sqrMagnitude < 0.01f)
        {
            randomDirection = Vector2.right;
        }

        float distance = Random.Range(spawnMinDistance, spawnMaxDistance);
        Vector3 spawnPosition = player.position + new Vector3(
            randomDirection.x * distance,
            1f,
            randomDirection.y * distance
        );

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        NeonCharacterVisuals visuals = enemy.GetComponent<NeonCharacterVisuals>();
        if (visuals == null) visuals = enemy.AddComponent<NeonCharacterVisuals>();
        visuals.BuildEnemy();

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.SetTarget(player);
            enemyAI.SetRunSpeedMultiplier(Mathf.Lerp(1f, 1.35f, progress));
        }

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.ApplyRunHealthMultiplier(Mathf.Lerp(1f, 1.8f, progress));
        }
    }
}
