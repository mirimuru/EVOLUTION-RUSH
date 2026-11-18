using UnityEngine;

/// <summary>
/// 1プレイの制限時間、撃破数、終了状態、リザルトを管理する。
/// </summary>
[DefaultExecutionOrder(10000)]
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>デバッグ用。ゲーム内タイマーを指定秒数だけ進める。</summary>
    public void DebugAdvanceTime(float seconds)
    {
        if (hasFinished || seconds <= 0f) return;

        // レベルアップ選択中はTime.timeScaleが0になるため、
        // Updateに依存せずデバッグ操作から直接経過時間を進める。
        FindPlayerReferences();
        if (playerHealth == null) return;

        isRunning = true;
        elapsedTime = Mathf.Min(runDuration, elapsedTime + seconds);
        if (elapsedTime >= runDuration)
        {
            elapsedTime = runDuration;
            EndRun(false);
        }
    }
#endif

    public void EndRun(bool wasCleared)
    {
        if (hasFinished) return;

        hasFinished = true;
        isRunning = false;
        cleared = wasCleared;
        SoundManager.Play(cleared ? SoundManager.Effect.Victory : SoundManager.Effect.GameOver);
        Time.timeScale = 0f;
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DrawDebugTimerPanel();
#endif

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
        GUILayout.Label($"到達レベル：{(playerExperience != null ? playerExperience.Level : 1)}", MakeCenteredStyle(18));
        GUILayout.Space(24f);

        if (GUILayout.Button("もう一度プレイ", GUILayout.Height(48f)))
        {
            RestartRun();
        }

        GUILayout.EndArea();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>経験値パネルと独立したタイマーデバッグ操作。</summary>
    private void DrawDebugTimerPanel()
    {
        if (hasFinished) return;

        const float panelWidth = 190f;
        const float panelHeight = 88f;
        Rect panel = new Rect(10f, Screen.height - panelHeight - 10f, panelWidth, panelHeight);
        GUI.Box(panel, "DEBUG TIME");

        string status = playerHealth == null
            ? "プレイヤー待機中"
            : $"経過 {Mathf.FloorToInt(ElapsedTime)}秒 / 残り {Mathf.CeilToInt(RemainingTime)}秒" + (Time.timeScale == 0f ? " [PAUSED]" : "");
        GUI.Label(new Rect(panel.x + 8f, panel.y + 22f, panelWidth - 16f, 20f), status);

        float buttonY = panel.y + 49f;
        float buttonWidth = (panelWidth - 24f) / 3f;
        if (GUI.Button(new Rect(panel.x + 6f, buttonY, buttonWidth, 28f), "+10秒"))
            DebugAdvanceTime(10f);
        if (GUI.Button(new Rect(panel.x + 9f + buttonWidth, buttonY, buttonWidth, 28f), "+30秒"))
            DebugAdvanceTime(30f);
        if (GUI.Button(new Rect(panel.x + 12f + buttonWidth * 2f, buttonY, buttonWidth, 28f), "+60秒"))
            DebugAdvanceTime(60f);
    }
#endif

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
                objectName == "LightningVisual" || objectName == "EnemyHitEffect" ||
                objectName == "BossAttackWarning")
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
