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

    private float spawnTimer;

    private void Update()
    {
        if (enemyPrefab == null || player == null)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        float intervalMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.SpawnIntervalMultiplier
            : 1f;

        float countMultiplier = GameSettings.Instance != null
            ? GameSettings.Instance.EnemyCountMultiplier
            : 1f;

        float currentSpawnInterval = Mathf.Max(0.1f, spawnInterval * intervalMultiplier);
        int currentMaxEnemies = Mathf.Max(1, Mathf.CeilToInt(maxEnemies * countMultiplier));

        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;

            if (FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length < currentMaxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
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

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.SetTarget(player);
        }
    }
}
