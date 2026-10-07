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
            Burst.Play(center, new Color(1f, 0.9f, 0.4f), WaveColor, 40, 3f, 7f, 0.06f, 1f, 0.6f);   // une gerbe d'étincelles qui retombent
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
