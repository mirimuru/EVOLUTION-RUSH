using UnityEngine;

/// <summary>
/// エネルギー弾が敵に命中したときの簡易パーティクル演出。
/// </summary>
public static class EnemyHitEffect
{
    public static void Create(Vector3 position)
    {
        GameObject effectObject = new GameObject("EnemyHitEffect");
        effectObject.transform.position = position;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.22f;
        main.startSpeed = 2.2f;
        main.startSize = 0.18f;
        main.startColor = new Color(0.35f, 0.85f, 1f, 1f);
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 8)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        particles.Play();
        Object.Destroy(effectObject, 1f);
    }
}
