using UnityEngine;

namespace SAE
{
    // Une gerbe de petites particules qui partent d'un point, retombent et s'éteignent, en une seule fois :
    // les étincelles de la flèche explosive, les confettis d'un ballon qui éclate, le feu d'artifice de la victoire.
    // L'objet se détruit tout seul quand la dernière particule s'éteint.
    public static class Burst
    {
        public static void Play(Vector3 position, Color colorA, Color colorB, int count,
                                float minSpeed, float maxSpeed, float size, float gravity = 1f, float lifetime = 0.6f)
        {
            var go = new GameObject("Gerbe");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // on règle avant de jouer
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var shrink = ps.sizeOverLifetime;   // elles rapetissent en s'éteignant
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Visuals.LineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }
    }
}
