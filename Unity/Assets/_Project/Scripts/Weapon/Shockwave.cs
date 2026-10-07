using UnityEngine;

namespace SAE
{
    // L'onde d'une flèche explosive : un anneau orange, à plat, qui grandit jusqu'au rayon de l'explosion
    // et s'efface en un quart de seconde, et une gerbe d'étincelles qui retombent. Le joueur voit tout de suite
    // jusqu'où l'explosion a porté.
    public class Shockwave : MonoBehaviour
    {
        const int Points = 32;
        const float Duration = 0.25f;   // secondes
        static readonly Color WaveColor = new Color(1f, 0.6f, 0.15f);

        LineRenderer ring;
        float radius;
        float age;

        public static void Spawn(Vector3 center, float radius)
        {
            var go = new GameObject("Onde");
            go.transform.position = center;
            var wave = go.AddComponent<Shockwave>();
            wave.radius = radius;
            Sparks(center);
        }

        // Une gerbe d'étincelles orange : 40 petits points qui partent dans tous les sens, retombent et s'éteignent
        // (un seul « burst », puis l'objet se détruit tout seul quand elles sont toutes éteintes).
        static void Sparks(Vector3 center)
        {
            var go = new GameObject("Étincelles");
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // on règle avant de jouer
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), WaveColor);
            main.gravityModifier = 1f;                          // elles retombent
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var fade = ps.sizeOverLifetime;                     // elles rapetissent en s'éteignant
            fade.enabled = true;
            fade.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Visuals.LineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        void Awake()
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = Visuals.LineMaterial;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = Points;
            ring.widthMultiplier = 0.08f;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < Points; i++)
            {
                float a = i * Mathf.PI * 2f / Points;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));   // cercle de 1 m, agrandi par l'échelle
            }
            Destroy(gameObject, Duration);
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, radius, t);
            var c = WaveColor;
            c.a = 1f - t;
            ring.startColor = ring.endColor = c;
        }
    }
}
