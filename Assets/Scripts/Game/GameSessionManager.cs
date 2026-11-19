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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool debugDoubleSpeed;
#endif
    private bool cleared;
    private bool returnToTitleRequested;

    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private PlayerExperience playerExperience;

    public bool IsRunning => isRunning && !hasFinished;
    public bool HasFinished => hasFinished;
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
        // OnGUIの描画中に状態を大きく変えないよう、タイトルへの復帰はUpdateで行う。
        if (returnToTitleRequested)
        {
            returnToTitleRequested = false;
            RestartRun();
            TitleScreen.Show();
            return;
        }

        if (hasFinished) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 一時停止中は変更せず、再開後にデバッグ設定の速度へ戻す。
        if (Time.timeScale > 0f)
        {
            float targetScale = debugDoubleSpeed ? 2f : 1f;
            if (!Mathf.Approximately(Time.timeScale, targetScale))
                Time.timeScale = targetScale;
        }
#endif

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
        NeonUI.Apply();

        // タイトル画面の表示中は、ゲーム内HUDやデバッグ表示を出さない。
        if (TitleScreen.IsShowing) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DrawDebugTimerPanel();
#endif

        if (!hasFinished)
        {
            if (playerHealth != null)
            {
                DrawHud();
            }
            return;
        }

        float panelWidth = Mathf.Min(440f, Screen.width - 30f);
        float panelHeight = 392f;
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

        GUILayout.Space(8f);
        if (GUILayout.Button("タイトルへ戻る", GUILayout.Height(40f)))
        {
            returnToTitleRequested = true;
        }

        GUILayout.EndArea();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>経験値パネルと独立したタイマーデバッグ操作。</summary>
    private void DrawDebugTimerPanel()
    {
        if (hasFinished) return;

        const float panelWidth = 190f;
        const float panelHeight = 120f;
        Rect panel = new Rect(10f, Screen.height - panelHeight - 10f, panelWidth, panelHeight);
        GUI.Box(panel, "DEBUG TIME");

        string status = playerHealth == null
            ? "プレイヤー待機中"
            : $"経過 {Mathf.FloorToInt(ElapsedTime)}秒 / 残り {Mathf.CeilToInt(RemainingTime)}秒" + (Time.timeScale == 0f ? " [PAUSED]" : "");
        GUI.Label(new Rect(panel.x + 8f, panel.y + 22f, panelWidth - 16f, 20f), status);

        string speedLabel = debugDoubleSpeed ? "ゲーム速度：2倍" : "ゲーム速度：1倍";
        GUI.Label(new Rect(panel.x + 8f, panel.y + 43f, panelWidth - 16f, 20f), speedLabel);
        if (GUI.Button(new Rect(panel.x + 8f, panel.y + 64f, panelWidth - 16f, 24f),
            debugDoubleSpeed ? "倍速を解除（1x）" : "倍速にする（2x）"))
        {
            debugDoubleSpeed = !debugDoubleSpeed;
            if (Time.timeScale > 0f && !hasFinished)
                Time.timeScale = debugDoubleSpeed ? 2f : 1f;
        }

        float buttonY = panel.y + 92f;
        float buttonWidth = (panelWidth - 24f) / 3f;
        if (GUI.Button(new Rect(panel.x + 6f, buttonY, buttonWidth, 28f), "+10秒"))
            DebugAdvanceTime(10f);
        if (GUI.Button(new Rect(panel.x + 9f + buttonWidth, buttonY, buttonWidth, 28f), "+30秒"))
            DebugAdvanceTime(30f);
        if (GUI.Button(new Rect(panel.x + 12f + buttonWidth * 2f, buttonY, buttonWidth, 28f), "+60秒"))
            DebugAdvanceTime(60f);
    }
#endif

    /// <summary>画面右下のネオン調HUD（残り時間・撃破数・HP）。</summary>
    private void DrawHud()
    {
        const float width = 300f;
        const float height = 106f;
        Rect panel = new Rect(Screen.width - width - 10f, Screen.height - height - 10f, width, height);

        bool bossPhase = BossPhaseStarted;
        Color accent = bossPhase ? NeonUI.Magenta : NeonUI.Cyan;
        NeonUI.DrawPanel(panel, accent);

        NeonUI.Label(new Rect(panel.x + 12f, panel.y + 6f, 160f, 34f),
            "TIME " + FormatTime(RemainingTime), 24, TextAnchor.MiddleLeft, accent);
        NeonUI.Label(new Rect(panel.xMax - 132f, panel.y + 10f, 120f, 26f),
            "KILLS " + defeatedEnemyCount, 16, TextAnchor.MiddleRight, NeonUI.Violet);

        float hpRatio = playerHealth.MaxHealth > 0
            ? playerHealth.CurrentHealth / (float)playerHealth.MaxHealth
            : 0f;
        Color hpColor = hpRatio > 0.5f ? NeonUI.Green
            : (hpRatio > 0.25f ? new Color(1f, 0.8f, 0.2f) : NeonUI.Magenta);

        Rect bar = new Rect(panel.x + 12f, panel.y + 46f, width - 24f, 24f);
        NeonUI.DrawBar(bar, hpRatio, hpColor);
        NeonUI.Label(bar, "HP  " + playerHealth.CurrentHealth + " / " + playerHealth.MaxHealth,
            14, TextAnchor.MiddleCenter, Color.white);

        if (bossPhase)
        {
            NeonUI.Label(new Rect(panel.x + 12f, panel.y + 76f, width - 24f, 22f),
                "! BOSS PHASE !", 14, TextAnchor.MiddleCenter, NeonUI.Magenta);
        }
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        debugDoubleSpeed = false;
#endif
        Time.timeScale = 1f;
    }
}
