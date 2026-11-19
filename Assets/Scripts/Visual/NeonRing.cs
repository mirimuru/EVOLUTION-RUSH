using UnityEngine;

/// <summary>
/// 地面に広がって消える発光リング。撃破・進化・レベルアップの演出に使う。
/// 時間停止中（強化選択画面）でも動くよう、unscaledDeltaTimeで進める。
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class NeonRing : MonoBehaviour
{
    private const int Segments = 48;

    private LineRenderer line;
    private Color color;
    private float maxRadius;
    private float duration;
    private float elapsed;

    public void Initialize(Material material, Color ringColor, float radius, float seconds, float width)
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = Segments + 1;
        line.widthMultiplier = width;
        if (material != null) line.sharedMaterial = material;

        color = ringColor;
        maxRadius = radius;
        duration = Mathf.Max(0.05f, seconds);
        elapsed = 0f;
        UpdateRing(0f);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(elapsed / duration);
        UpdateRing(k);

        if (k >= 1f) Destroy(gameObject);
    }

    private void UpdateRing(float k)
    {
        // 最初に勢いよく広がり、だんだん遅くなる。
        float eased = 1f - (1f - k) * (1f - k);
        float radius = Mathf.Lerp(0.3f, maxRadius, eased);

        for (int i = 0; i <= Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        Color faded = new Color(color.r, color.g, color.b, 1f - k);
        line.startColor = faded;
        line.endColor = faded;
    }
}
