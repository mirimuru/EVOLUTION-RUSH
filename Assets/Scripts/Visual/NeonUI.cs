using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UIのネオン調デザインをまとめた共通ヘルパー。
/// 各スクリプトの OnGUI の先頭で NeonUI.Apply() を呼ぶと、ボタン・ウィンドウ・ボックス・文字が
/// 暗い背景＋シアンの発光枠のスタイルになる。HUDのバーやパネルの描画関数も提供する。
/// </summary>
public static class NeonUI
{
    public static readonly Color Cyan = new Color(0.20f, 0.85f, 1f);
    public static readonly Color Violet = new Color(0.65f, 0.35f, 1f);
    public static readonly Color Magenta = new Color(1f, 0.25f, 0.65f);
    public static readonly Color Green = new Color(0.25f, 1f, 0.6f);
    public static readonly Color TextColor = new Color(0.86f, 0.96f, 1f);
    public static readonly Color PanelFill = new Color(0.03f, 0.06f, 0.12f, 0.88f);

    private static GUISkin skin;
    private static readonly List<Texture2D> textures = new List<Texture2D>();
    private static readonly Dictionary<string, GUIStyle> textStyles = new Dictionary<string, GUIStyle>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        skin = null;
        textures.Clear();
        textStyles.Clear();
    }

    /// <summary>OnGUIの先頭で呼ぶ。以降のGUI部品がネオン調になる。</summary>
    public static void Apply()
    {
        if (skin == null) BuildSkin();
        GUI.skin = skin;
    }

    // ---- 描画ヘルパー ----

    /// <summary>暗い背景と発光枠のパネルを描く。</summary>
    public static void DrawPanel(Rect rect, Color border)
    {
        FillRect(rect, PanelFill);
        DrawOutline(rect, border, 2f);
    }

    /// <summary>ゲージ（HP・経験値など）を描く。</summary>
    public static void DrawBar(Rect rect, float ratio, Color fill)
    {
        FillRect(rect, new Color(0.02f, 0.03f, 0.07f, 0.95f));

        float clamped = Mathf.Clamp01(ratio);
        if (clamped > 0f)
        {
            Rect inner = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * clamped, rect.height - 4f);
            FillRect(inner, fill);
            // 上半分を明るくして発光しているように見せる。
            FillRect(new Rect(inner.x, inner.y, inner.width, inner.height * 0.4f),
                new Color(1f, 1f, 1f, 0.22f));
        }

        DrawOutline(rect, new Color(fill.r, fill.g, fill.b, 0.9f), 1.5f);
    }

    public static void FillRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void DrawOutline(Rect rect, Color color, float thickness)
    {
        FillRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        FillRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        FillRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        FillRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    /// <summary>指定サイズ・配置・色の太字ラベルを描く。</summary>
    public static void Label(Rect rect, string text, int size, TextAnchor anchor, Color color)
    {
        string key = size + "_" + (int)anchor + "_" + ColorUtility.ToHtmlStringRGB(color);
        if (!textStyles.TryGetValue(key, out GUIStyle style) || style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                fontStyle = FontStyle.Bold,
                wordWrap = false
            };
            style.normal.textColor = color;
            textStyles[key] = style;
        }

        // 影を付けて読みやすくする。
        Rect shadow = new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.Label(shadow, text, style);
        GUI.color = previous;
        GUI.Label(rect, text, style);
    }

    // ---- スキン生成 ----

    private static Texture2D MakeBox(Color fill, Color border)
    {
        const int size = 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                texture.SetPixel(x, y, edge ? border : fill);
            }
        }

        texture.Apply();
        textures.Add(texture);
        return texture;
    }

    private static void SetState(GUIStyleState state, Texture2D background, Color text)
    {
        state.background = background;
        state.textColor = text;
    }

    private static void BuildSkin()
    {
        skin = Object.Instantiate(GUI.skin);
        skin.hideFlags = HideFlags.HideAndDontSave;

        Texture2D windowTex = MakeBox(new Color(0.03f, 0.06f, 0.12f, 0.96f), new Color(0.15f, 0.75f, 1f, 1f));
        Texture2D boxTex = MakeBox(new Color(0.04f, 0.08f, 0.16f, 0.92f), new Color(0.35f, 0.28f, 0.8f, 1f));
        Texture2D buttonTex = MakeBox(new Color(0.05f, 0.16f, 0.30f, 1f), new Color(0.15f, 0.75f, 1f, 1f));
        Texture2D buttonHover = MakeBox(new Color(0.08f, 0.30f, 0.52f, 1f), new Color(0.6f, 0.95f, 1f, 1f));
        Texture2D buttonActive = MakeBox(new Color(0.15f, 0.50f, 0.80f, 1f), Color.white);
        Texture2D fieldTex = MakeBox(new Color(0.02f, 0.04f, 0.09f, 1f), new Color(0.3f, 0.5f, 0.8f, 1f));

        RectOffset border = new RectOffset(2, 2, 2, 2);

        skin.window.border = border;
        skin.window.padding = new RectOffset(14, 14, 12, 12);
        SetState(skin.window.normal, windowTex, TextColor);
        SetState(skin.window.onNormal, windowTex, TextColor);

        skin.box.border = border;
        SetState(skin.box.normal, boxTex, TextColor);

        skin.button.border = border;
        skin.button.fontStyle = FontStyle.Bold;
        SetState(skin.button.normal, buttonTex, TextColor);
        SetState(skin.button.hover, buttonHover, Color.white);
        SetState(skin.button.active, buttonActive, Color.white);
        // トグル／ツールバーで「選択中」の状態に使われる。
        SetState(skin.button.onNormal, buttonActive, Color.white);
        SetState(skin.button.onHover, buttonActive, Color.white);
        SetState(skin.button.onActive, buttonActive, Color.white);

        skin.label.normal.textColor = TextColor;
        skin.toggle.normal.textColor = TextColor;

        skin.textField.border = border;
        SetState(skin.textField.normal, fieldTex, TextColor);
        SetState(skin.textField.hover, fieldTex, TextColor);
        SetState(skin.textField.focused, fieldTex, Color.white);
    }
}
