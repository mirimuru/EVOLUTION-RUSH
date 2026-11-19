using UnityEngine;

/// <summary>
/// 敵撃破・ボス撃破・進化・レベルアップのネオン調演出（パーティクルと発光リング）。
/// Prefabやマテリアルの用意は不要で、すべてコードから生成する。
/// </summary>
public static class NeonEffects
{
    private static Material particleMaterial;
    private static Material lineMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        particleMaterial = null;
        lineMaterial = null;
    }

    /// <summary>敵を倒した位置に赤紫の破片を飛ばす。ボスの場合は大きな爆発にする。</summary>
    public static void EnemyBurst(Vector3 position, bool boss)
    {
        Color a = new Color(1f, 0.2f, 0.4f);
        Color b = new Color(0.7f, 0.25f, 1f);

        if (boss)
        {
            Burst(position, a, b, 70, 6f, 14f, 0.45f, 0.9f);
            Ring(position, new Color(1f, 0.3f, 0.8f), 9f, 0.7f, 0.3f);
            Ring(position, new Color(0.6f, 0.9f, 1f), 5f, 0.5f, 0.2f);
            ScreenFlash.Trigger(new Color(0.9f, 0.5f, 1f), 0.6f, 0.45f);
        }
        else
        {
            Burst(position, a, b, 14, 3f, 7f, 0.15f, 0.55f);
            Ring(position, new Color(1f, 0.3f, 0.55f), 1.8f, 0.25f, 0.09f);
        }
    }

    /// <summary>攻撃の進化時に、金とシアンの大きな衝撃を出す。</summary>
    public static void EvolutionBurst(Vector3 position)
    {
        Color gold = new Color(1f, 0.85f, 0.3f);
        Color cyan = new Color(0.4f, 0.95f, 1f);
        Burst(position + Vector3.up * 0.8f, gold, cyan, 50, 4f, 9f, 0.2f, 0.8f);
        Ring(position, gold, 7f, 0.7f, 0.25f);
        Ring(position, cyan, 4.5f, 0.5f, 0.18f);
        ScreenFlash.Trigger(gold, 0.5f, 0.4f);
    }

    /// <summary>レベルアップ時にプレイヤーの足元からシアンのリングを広げる。</summary>
    public static void LevelUpRing(Vector3 position)
    {
        Ring(position, new Color(0.3f, 0.9f, 1f), 5.5f, 0.55f, 0.16f);
    }

    public static void Ring(Vector3 position, Color color, float radius, float seconds, float width)
    {
        GameObject ringObject = new GameObject("NeonRing");
        ringObject.transform.position = new Vector3(position.x, 0.2f, position.z);
        ringObject.AddComponent<LineRenderer>();
        NeonRing ring = ringObject.AddComponent<NeonRing>();
        ring.Initialize(GetLineMaterial(), color, radius, seconds, width);
    }

    private static void Burst(Vector3 position, Color colorA, Color colorB, int count,
        float minSpeed, float maxSpeed, float minSize, float maxSize)
    {
        GameObject effect = new GameObject("NeonBurst");
        effect.transform.position = position;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize * 0.5f, maxSize * 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.maxParticles = count + 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.4f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        // だんだん小さく、透明になって消える。
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        Material material = GetParticleMaterial();
        if (particleRenderer != null && material != null) particleRenderer.sharedMaterial = material;

        particles.Play();
        Object.Destroy(effect, 1.5f);
    }

    // マテリアルは1つを使い回す（色は頂点カラーで指定する）。
    private static Material GetParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return null;

        particleMaterial = new Material(shader);
        return particleMaterial;
    }

    private static Material GetLineMaterial()
    {
        if (lineMaterial != null) return lineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return null;

        lineMaterial = new Material(shader);
        return lineMaterial;
    }
}
