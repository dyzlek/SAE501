using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // L'onde d'une flèche explosive : un anneau orange, à plat, qui grandit jusqu'au rayon de l'explosion
    // et s'efface en un quart de seconde, avec une gerbe d'étincelles qui retombent. Le joueur voit tout de suite
    // jusqu'où l'explosion a porté.
    public class Shockwave : MonoBehaviour
    {
        const int Points = 32;
        const float Duration = 0.25f;   // secondes
        static readonly Color WaveColor = new Color(1f, 0.6f, 0.15f);

        // Ondes et étincelles réutilisées (#178) : avant, chaque explosion créait un objet, et un système de particules neuf.
        static readonly Stack<Shockwave> pool = new Stack<Shockwave>();
        static ParticleSystem sparks;
        const int SparkCount = 25;   // étincelles par explosion (avant : 40)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool() { pool.Clear(); sparks = null; }

        LineRenderer ring;
        Color color = WaveColor;
        float radius;
        float age;

        public static void Spawn(Vector3 center, float radius) => Spawn(center, radius, WaveColor, true);

        // Même onde, d'une autre couleur (ex. l'onde de froid bleue du singe de glace), avec ou sans étincelles.
        public static void Spawn(Vector3 center, float radius, Color color, bool withSparks)
        {
            Shockwave wave = null;
            while (pool.Count > 0 && !wave) wave = pool.Pop();   // une onde détruite avec sa scène est sautée
            if (!wave) wave = new GameObject("Onde").AddComponent<Shockwave>();
            wave.transform.position = center;
            wave.radius = radius;
            wave.color = color;
            wave.age = 0f;
            wave.gameObject.SetActive(true);
            wave.Grow(0f);
            if (withSparks) Sparks(center);
        }

        // Une gerbe d'étincelles orange : de petits points qui partent dans tous les sens, retombent et s'éteignent.
        // Un seul système de particules pour toute la scène : on le déplace puis on émet d'un coup (Emit).
        static void Sparks(Vector3 center)
        {
            if (!sparks) sparks = MakeSparks();
            sparks.transform.position = center;
            sparks.Emit(SparkCount);
        }

        static ParticleSystem MakeSparks()
        {
            var go = new GameObject("Étincelles");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // on règle avant de jouer
            var main = ps.main;
            main.loop = true;                                   // toujours « en marche », sans rien émettre seul : Emit ajoute les gerbes
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // une gerbe ne suit pas le système quand il part ailleurs
            main.maxParticles = 300;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), WaveColor);
            main.gravityModifier = 1f;                          // elles retombent
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var shrink = ps.sizeOverLifetime;                   // elles rapetissent en s'éteignant
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Visuals.LineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
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
        }

        // Appelée aussi tout de suite par Spawn (avec age = 0) : taille et couleur de départ sans attendre une image
        void Update() => Grow(Time.deltaTime);

        void Grow(float dt)
        {
            age += dt;
            float t = Mathf.Clamp01(age / Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, radius, t);
            var c = color;
            c.a = 1f - t;
            ring.startColor = ring.endColor = c;
            if (t < 1f) return;
            gameObject.SetActive(false);   // finie : rangée pour la prochaine explosion
            pool.Push(this);
        }
    }
}
