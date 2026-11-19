using UnityEngine;

/// <summary>
/// 画面全体を一瞬だけ色で光らせる演出（被弾・レベルアップ・ボス登場など）。
/// どこからでも ScreenFlash.Trigger(色, 長さ) で呼べる。時間停止中も動作する。
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    private static ScreenFlash instance;

    private Color flashColor;
    private float startTime;
    private float duration;
    private float peakAlpha;

    public static void Trigger(Color color, float seconds = 0.25f, float alpha = 0.35f)
    {
        if (instance == null)
        {
            instance = new GameObject("ScreenFlash").AddComponent<ScreenFlash>();
        }

        instance.flashColor = color;
        instance.startTime = Time.unscaledTime;
        instance.duration = Mathf.Max(0.05f, seconds);
        instance.peakAlpha = Mathf.Clamp01(alpha);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnGUI()
    {
        if (duration <= 0f) return;

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed >= duration) return;

        // ほかのUIより奥に描く（数字が大きいほど奥）。
        GUI.depth = 100;

        float fade = 1f - Mathf.Clamp01(elapsed / duration);
        Color previous = GUI.color;
        GUI.color = new Color(flashColor.r, flashColor.g, flashColor.b, peakAlpha * fade * fade);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
