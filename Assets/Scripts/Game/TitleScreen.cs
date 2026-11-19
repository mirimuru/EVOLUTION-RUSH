using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タイトル画面。ゲーム開始前に難易度の選択と操作説明を表示し、STARTでゲームを始める。
/// シーンへの手動配置は不要で、ゲーム開始時に自動生成される。
/// 表示中は Time.timeScale = 0 でゲームを止める。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public static TitleScreen Instance { get; private set; }
    public static bool IsShowing => Instance != null && Instance.visible;

    private static readonly string[] DifficultyNames = { "EASY", "NORMAL", "HARD" };

    private bool visible;
    private Vector2 howToScroll;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle startButtonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        if (Instance != null) return;
        new GameObject("TitleScreen").AddComponent<TitleScreen>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OpenTitle();
    }

    /// <summary>リザルト画面などからタイトル画面へ戻るときに呼ぶ。</summary>
    public static void Show()
    {
        if (Instance == null) CreateInstance();
        if (Instance != null) Instance.OpenTitle();
    }

    private void OpenTitle()
    {
        visible = true;
        howToScroll = Vector2.zero;
        Time.timeScale = 0f;
        GameSettings.SuppressGui = true;
    }

    private void StartGame()
    {
        visible = false;
        Time.timeScale = 1f;
        GameSettings.SuppressGui = false;
        SoundManager.Play(SoundManager.Effect.LevelUp);
    }

    private void Update()
    {
        if (!visible || Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartGame();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            GameSettings.SuppressGui = false;
        }
    }

    private void BuildStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.35f, 0.9f, 1f);

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16
        };

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        headingStyle.normal.textColor = new Color(0.8f, 0.55f, 1f);

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true
        };

        startButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
    }

    private void OnGUI()
    {
        if (!visible) return;

        NeonUI.Apply();
        BuildStyles();

        // 背景を暗くして、ゲーム画面が透けて見えないようにする。
        Color previousColor = GUI.color;
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.92f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        float panelWidth = Mathf.Min(580f, Screen.width - 30f);
        float panelHeight = Mathf.Min(640f, Screen.height - 30f);
        Rect panel = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUILayout.BeginArea(panel, GUI.skin.window);
        GUILayout.Space(10f);
        GUILayout.Label("EVOLUTION RUSH", titleStyle);
        GUILayout.Label("180秒を生き延び、ボスを倒せ！", subtitleStyle);
        GUILayout.Space(10f);

        DrawDifficultySelectors();

        GUILayout.Space(8f);
        GUILayout.Label("遊び方", headingStyle);
        howToScroll = GUILayout.BeginScrollView(howToScroll, false, true, GUILayout.ExpandHeight(true));
        GUILayout.Label(BuildHowToText(), bodyStyle);
        GUILayout.EndScrollView();

        GUILayout.Space(8f);
        if (GUILayout.Button("START　（Enter / Space）", startButtonStyle, GUILayout.Height(54f)))
        {
            StartGame();
        }

        GUILayout.Space(6f);
        GUILayout.EndArea();
    }

    private void DrawDifficultySelectors()
    {
        GameSettings settings = GameSettings.Instance;
        if (settings == null) return;

        GUILayout.Label("操作難易度", headingStyle);
        int operation = GUILayout.Toolbar((int)settings.OperationMode, DifficultyNames, GUILayout.Height(30f));
        if (operation != (int)settings.OperationMode)
        {
            settings.SetOperationDifficulty((GameSettings.OperationDifficulty)operation);
        }
        GUILayout.Label(DescribeOperation(settings.OperationMode), bodyStyle);

        GUILayout.Space(4f);
        GUILayout.Label("ゲーム難易度（敵の強さ・数）", headingStyle);
        int game = GUILayout.Toolbar((int)settings.GameMode, DifficultyNames, GUILayout.Height(30f));
        if (game != (int)settings.GameMode)
        {
            settings.SetGameDifficulty((GameSettings.GameDifficulty)game);
        }
    }

    private static string DescribeOperation(GameSettings.OperationDifficulty mode)
    {
        switch (mode)
        {
            case GameSettings.OperationDifficulty.Easy:
                return "最も近い敵を自動で狙って撃ちます。移動に集中できます。";
            case GameSettings.OperationDifficulty.Hard:
                return "マウスで狙い、左クリックで発射します。照準補助は弱めです。";
            default:
                return "マウスの方向へ自動で撃ちます。照準補助があります。";
        }
    }

    private static string BuildHowToText()
    {
        return
            "【目的】\n" +
            "制限時間は180秒。残り30秒でボスが現れます。ボスを倒せばクリアです。HPが0になるか、時間切れになるとゲームオーバーです。\n\n" +
            "【移動】\n" +
            "WASDキー、または矢印キーで移動します。敵に近づかれるとダメージを受けるので、動き続けましょう。\n\n" +
            "【攻撃】\n" +
            "攻撃は自動で行われます。狙い方は操作難易度で変わります。\n\n" +
            "【レベルアップ】\n" +
            "敵を倒すと経験値が入り、レベルアップすると強化を3つの候補から1つ選べます（マウスでクリック）。候補が多いときは、マウスホイールやスクロールバーで画面をスクロールできます。\n\n" +
            "【攻撃を増やして育てる】\n" +
            "回転オーブ・衝撃波・雷撃・メテオを新しく取得できます。強化を重ねると分岐強化や「進化コア」が現れ、攻撃を進化させられます。エネルギー弾と雷撃を育てると、特殊進化も狙えます。\n\n" +
            "【敵の種類】\n" +
            "時間が進むと新しい敵が現れます。緑の「ランナー」は速くて脆く、橙の「シューター」は離れた所から弾を撃ち、赤の大型「ブルート」は遅いけれど硬く、倒すと経験値がたくさん手に入ります。\n\n" +
            "【ヒント】\n" +
            "ボス戦では、足元に出る赤い円（予告範囲）から素早く離れましょう。シューターの弾は動き続ければ避けられます。";
    }
}
