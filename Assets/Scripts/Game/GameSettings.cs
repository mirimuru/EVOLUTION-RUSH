using System;
using UnityEngine;

/// <summary>
/// 操作難易度とゲーム難易度を管理する。
/// シーンに配置しなくても、ゲーム開始時に自動生成される。
/// </summary>
public class GameSettings : MonoBehaviour
{
    public enum OperationDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public static GameSettings Instance { get; private set; }

    public OperationDifficulty OperationMode { get; private set; } = OperationDifficulty.Normal;
    public GameDifficulty GameMode { get; private set; } = GameDifficulty.Normal;

    private bool showPanel = true;

    // 敵の能力・出現量はゲーム難易度によって変化する。
    public float EnemyMoveSpeedMultiplier
    {
        get
        {
            switch (GameMode)
            {
                case GameDifficulty.Easy: return 0.85f;
                case GameDifficulty.Hard: return 1.2f;
                default: return 1f;
            }
        }
    }

    public float EnemyHealthMultiplier
    {
        get
        {
            switch (GameMode)
            {
                case GameDifficulty.Easy: return 0.8f;
                case GameDifficulty.Hard: return 1.25f;
                default: return 1f;
            }
        }
    }

    public float SpawnIntervalMultiplier
    {
        get
        {
            switch (GameMode)
            {
                case GameDifficulty.Easy: return 1.25f;
                case GameDifficulty.Hard: return 0.75f;
                default: return 1f;
            }
        }
    }

    public float EnemyCountMultiplier
    {
        get
        {
            switch (GameMode)
            {
                case GameDifficulty.Easy: return 0.8f;
                case GameDifficulty.Hard: return 1.5f;
                default: return 1f;
            }
        }
    }

    // マウス照準の補助角度。NORMALは少し狙いやすくする。
    public float AimAssistAngle
    {
        get
        {
            switch (OperationMode)
            {
                case OperationDifficulty.Normal: return 14f;
                case OperationDifficulty.Hard: return 5f;
                default: return 180f;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject settingsObject = new GameObject("GameSettings");
        settingsObject.AddComponent<GameSettings>();
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

        int operationValue = PlayerPrefs.GetInt("EVOLUTION_RUSH_OperationDifficulty", (int)OperationDifficulty.Normal);
        int gameValue = PlayerPrefs.GetInt("EVOLUTION_RUSH_GameDifficulty", (int)GameDifficulty.Normal);

        OperationMode = Enum.IsDefined(typeof(OperationDifficulty), operationValue)
            ? (OperationDifficulty)operationValue
            : OperationDifficulty.Normal;

        GameMode = Enum.IsDefined(typeof(GameDifficulty), gameValue)
            ? (GameDifficulty)gameValue
            : GameDifficulty.Normal;
    }

    public void SetOperationDifficulty(OperationDifficulty difficulty)
    {
        OperationMode = difficulty;
        PlayerPrefs.SetInt("EVOLUTION_RUSH_OperationDifficulty", (int)difficulty);
        PlayerPrefs.Save();
    }

    public void SetGameDifficulty(GameDifficulty difficulty)
    {
        GameMode = difficulty;
        PlayerPrefs.SetInt("EVOLUTION_RUSH_GameDifficulty", (int)difficulty);
        PlayerPrefs.Save();
    }

    private void OnGUI()
    {
        if (!showPanel)
        {
            GUILayout.BeginArea(new Rect(10, 10, 130, 35), GUI.skin.box);
            if (GUILayout.Button("難易度設定を開く"))
            {
                showPanel = true;
            }
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 310, 160), GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("難易度設定", GUILayout.ExpandWidth(true));
        if (GUILayout.Button("閉じる", GUILayout.Width(55)))
        {
            showPanel = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("操作難易度： " + GetOperationDifficultyName());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("EASY")) SetOperationDifficulty(OperationDifficulty.Easy);
        if (GUILayout.Button("NORMAL")) SetOperationDifficulty(OperationDifficulty.Normal);
        if (GUILayout.Button("HARD")) SetOperationDifficulty(OperationDifficulty.Hard);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label("ゲーム難易度： " + GetGameDifficultyName());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("EASY")) SetGameDifficulty(GameDifficulty.Easy);
        if (GUILayout.Button("NORMAL")) SetGameDifficulty(GameDifficulty.Normal);
        if (GUILayout.Button("HARD")) SetGameDifficulty(GameDifficulty.Hard);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private string GetOperationDifficultyName()
    {
        switch (OperationMode)
        {
            case OperationDifficulty.Easy: return "EASY";
            case OperationDifficulty.Hard: return "HARD";
            default: return "NORMAL";
        }
    }

    private string GetGameDifficultyName()
    {
        switch (GameMode)
        {
            case GameDifficulty.Easy: return "EASY";
            case GameDifficulty.Hard: return "HARD";
            default: return "NORMAL";
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
