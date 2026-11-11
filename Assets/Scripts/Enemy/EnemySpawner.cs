using UnityEngine;

/// <summary>
/// プレイヤーの周囲に敵を一定間隔で生成する。
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

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            if (FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length < maxEnemies)
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
