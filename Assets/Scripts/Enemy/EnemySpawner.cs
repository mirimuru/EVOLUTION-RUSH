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
    [SerializeField] private float baseSpawnInterval = 1.5f;
    [SerializeField] private float spawnMinDistance = 9f;
    [SerializeField] private float spawnMaxDistance = 12f;
    [SerializeField] private int baseMaxEnemies = 14;

    [Header("Difficulty Ramp")]
    // 敵が強くなる速さ。2なら、制限時間の半分（90秒）で最大の強さに達する。
    [SerializeField] private float difficultyRampSpeed = 1.7f;
    // 最大の強さに達したときの倍率（生成間隔は小さいほど速く出現する）。
    [SerializeField] private float finalSpawnIntervalScale = 0.35f;
    [SerializeField] private float finalEnemyCountScale = 2.5f;
    [SerializeField] private float finalEnemyHealthScale = 2.8f;
    [SerializeField] private float finalEnemySpeedScale = 1.35f;

    [Header("Enemy Variety")]
    // 各種類が出始める進行度（0～1、加速後）。
    [SerializeField] private float runnerStartProgress = 0.12f;
    [SerializeField] private float shooterStartProgress = 0.30f;
    [SerializeField] private float bruteStartProgress = 0.50f;

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
        // 時間の経過より速く強くなるよう、進行度を加速させる。
        float ramp = Mathf.Clamp01(progress * difficultyRampSpeed);
        if (!bossSpawned && session != null && session.BossPhaseStarted)
        {
            SpawnBoss();
            bossSpawned = true;
        }

        spawnTimer += Time.deltaTime;
        float intervalMultiplier = GameSettings.Instance != null ? GameSettings.Instance.SpawnIntervalMultiplier : 1f;
        float countMultiplier = GameSettings.Instance != null ? GameSettings.Instance.EnemyCountMultiplier : 1f;
        float timeSpawnMultiplier = Mathf.Lerp(1f, finalSpawnIntervalScale, ramp);
        float timeCountMultiplier = Mathf.Lerp(1f, finalEnemyCountScale, ramp);
        float currentSpawnInterval = Mathf.Max(0.2f, baseSpawnInterval * intervalMultiplier * timeSpawnMultiplier);
        int currentMaxEnemies = Mathf.Max(1, Mathf.CeilToInt(baseMaxEnemies * countMultiplier * timeCountMultiplier));

        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;

            if (EnemyAI.ActiveCount < currentMaxEnemies)
            {
                SpawnEnemy(ramp);
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
        SoundManager.Play(SoundManager.Effect.BossSpawn);
        ScreenFlash.Trigger(new Color(0.7f, 0.15f, 1f), 0.6f, 0.4f);
    }

    /// <summary>進行度に応じて、出現させる敵の種類を抽選する。</summary>
    private EnemyType ChooseEnemyType(float progress)
    {
        float runner = progress >= runnerStartProgress ? Mathf.Lerp(0.10f, 0.25f, progress) : 0f;
        float shooter = progress >= shooterStartProgress ? Mathf.Lerp(0.08f, 0.18f, progress) : 0f;
        float brute = progress >= bruteStartProgress ? Mathf.Lerp(0.05f, 0.12f, progress) : 0f;

        float roll = Random.value;
        if (roll < runner) return EnemyType.Runner;
        roll -= runner;
        if (roll < shooter) return EnemyType.Shooter;
        roll -= shooter;
        if (roll < brute) return EnemyType.Brute;
        return EnemyType.Grunt;
    }

    private static float GetBodyScale(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Runner: return 0.8f;
            case EnemyType.Brute: return 1.7f;
            default: return 1f;
        }
    }

    private static float GetHealthScale(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Runner: return 0.5f;
            case EnemyType.Shooter: return 0.8f;
            case EnemyType.Brute: return 4f;
            default: return 1f;
        }
    }

    private static int GetExperienceValue(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Brute: return 4;
            case EnemyType.Shooter: return 2;
            default: return 1;
        }
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

        EnemyType type = ChooseEnemyType(progress);
        float bodyScale = GetBodyScale(type);
        spawnPosition.y = bodyScale;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.transform.localScale = Vector3.one * bodyScale;

        NeonCharacterVisuals visuals = enemy.GetComponent<NeonCharacterVisuals>();
        if (visuals == null) visuals = enemy.AddComponent<NeonCharacterVisuals>();
        visuals.BuildEnemy(type);

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Configure(type);
            enemyAI.SetTarget(player);
            enemyAI.SetRunSpeedMultiplier(Mathf.Lerp(1f, finalEnemySpeedScale, progress));
        }

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // ネオンモデル生成後に被弾フラッシュの対象を取り直す。
            enemyHealth.RefreshRenderers();
            enemyHealth.ApplyRunHealthMultiplier(Mathf.Lerp(1f, finalEnemyHealthScale, progress) * GetHealthScale(type));
            enemyHealth.SetExperienceValue(GetExperienceValue(type));
        }
    }
}
