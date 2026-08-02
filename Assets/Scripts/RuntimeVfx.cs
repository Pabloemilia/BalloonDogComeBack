using UnityEngine;

public static class RuntimeVfx
{
    public static void SpawnBurst(
        Vector3 position,
        Color color,
        int particleCount = 16,
        float speed = 3f,
        float size = 0.14f,
        float lifetime = 0.55f)
    {
        GameObject effectObject = new GameObject("RuntimeBurstVFX");
        effectObject.transform.position = position;

        ParticleSystem system = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(24, particleCount);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            color,
            new Color(color.r, color.g, color.b, 0f));

        system.Emit(particleCount);
        Object.Destroy(effectObject, lifetime + 0.35f);
    }
}
