using UnityEngine;

/// <summary>
/// 1プレイの制限時間、撃破数、終了状態、リザルトを管理する。
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Header("Run Settings")]
    [SerializeField] private float runDuration = 180f;

    private float elapsedTime;
    private int defeatedEnemyCount;
    private bool isRunning;
    private bool hasFinished;
    private bool cleared;

    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private PlayerExperience playerExperience;

    public bool IsRunning => isRunning && !hasFinished;
    public float RemainingTime => Mathf.Max(0f, runDuration - elapsedTime);
    public float ElapsedTime => elapsedTime;
    public float Progress => Mathf.Clamp01(elapsedTime / runDuration);
    public bool BossPhaseStarted => elapsedTime >= 150f;
    public int DefeatedEnemyCount => defeatedEnemyCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (Instance != null) return;

        GameObject managerObject = new GameObject("GameSessionManager");
        managerObject.AddComponent<GameSessionManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EnemyHealth.EnemyDefeated += HandleEnemyDefeated;
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyDefeated -= HandleEnemyDefeated;
    }

    private void Update()
    {
        if (hasFinished) return;

        FindPlayerReferences();

        if (!isRunning)
        {
            if (playerHealth != null)
            {
                isRunning = true;
            }
            else
            {
                return;
            }
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= runDuration)
        {
            elapsedTime = runDuration;
            EndRun(false);
        }
    }

    private void FindPlayerReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            if (playerController == null)
                playerController = playerHealth.GetComponent<PlayerController>();

            if (playerExperience == null)
                playerExperience = playerHealth.GetComponent<PlayerExperience>();
        }
    }

    private void HandleEnemyDefeated(EnemyHealth defeatedEnemy)
    {
        if (!IsRunning) return;
        if (defeatedEnemy != null && defeatedEnemy.GetComponent<BossAI>() != null)
        {
            EndRun(true);
            return;
        }
        defeatedEnemyCount++;
    }

    public void EndRun(bool wasCleared)
    {
        if (hasFinished) return;

        hasFinished = true;
        isRunning = false;
        cleared = wasCleared;
        Time.timeScale = 0f;
    }

    private void OnGUI()
    {
        if (!hasFinished)
        {
            if (playerHealth != null)
            {
                GUI.Box(new Rect(Screen.width - 220f, Screen.height - 92f, 210f, 76f),
                    $"TIME  {FormatTime(RemainingTime)}\nHP  {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}\nKILLS  {defeatedEnemyCount}");
            }
            return;
        }

        float panelWidth = Mathf.Min(440f, Screen.width - 30f);
        float panelHeight = 330f;
        Rect panel = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        GUILayout.BeginArea(panel, GUI.skin.window);
        GUILayout.Space(18f);
        GUILayout.Label(cleared ? "BOSS DEFEATED!" : "GAME OVER", MakeCenteredStyle(30));
        GUILayout.Space(12f);
        GUILayout.Label(cleared
            ? "ボスを撃破しました！"
            : (elapsedTime >= runDuration ? "ボスを時間内に倒せませんでした。" : "HPがなくなりました。"), MakeCenteredStyle(16));
        GUILayout.Space(20f);
        GUILayout.Label($"生存時間：{FormatTime(elapsedTime)}", MakeCenteredStyle(18));
        GUILayout.Label($"敵撃破数：{defeatedEnemyCount}", MakeCenteredStyle(18));
        GUILayout.Label($"到達レベル：{playerExperience != null ? playerExperience.Level : 1}", MakeCenteredStyle(18));
        GUILayout.Space(24f);

        if (GUILayout.Button("もう一度プレイ", GUILayout.Height(48f)))
        {
            RestartRun();
        }

        GUILayout.EndArea();
    }

    private GUIStyle MakeCenteredStyle(int fontSize)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            fontStyle = FontStyle.Bold
        };
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private void RestartRun()
    {
        FindPlayerReferences();

        BossAI[] bosses = FindObjectsByType<BossAI>(FindObjectsSortMode.None);
        foreach (BossAI boss in bosses)
        {
            if (boss != null) Destroy(boss.gameObject);
        }

        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        ExperienceOrb[] orbs = FindObjectsByType<ExperienceOrb>(FindObjectsSortMode.None);
        foreach (ExperienceOrb orb in orbs)
        {
            if (orb != null) Destroy(orb.gameObject);
        }

        EnergyProjectile[] projectiles = FindObjectsByType<EnergyProjectile>(FindObjectsSortMode.None);
        foreach (EnergyProjectile projectile in projectiles)
        {
            if (projectile != null) Destroy(projectile.gameObject);
        }

        // 攻撃演出の途中で終了した場合に残る一時オブジェクトも片付ける。
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform == null) continue;

            string objectName = sceneTransform.gameObject.name;
            if (objectName == "MeteorWarning" || objectName == "Meteor" ||
                objectName == "MeteorExplosion" || objectName == "ShockwaveVisual" ||
                objectName == "LightningVisual" || objectName == "EnemyHitEffect")
            {
                Destroy(sceneTransform.gameObject);
            }
        }

        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (EnemySpawner spawner in spawners)
        {
            spawner.ResetSpawner();
        }

        if (playerController != null)
            playerController.ResetPlayerPosition();

        if (playerExperience != null)
            playerExperience.ResetForNewRun();

        if (playerHealth != null)
            playerHealth.ResetHealth();

        elapsedTime = 0f;
        defeatedEnemyCount = 0;
        hasFinished = false;
        cleared = false;
        isRunning = playerHealth != null;
        Time.timeScale = 1f;
    }
}
