using UnityEngine;

namespace SAE
{
    // Fait vaciller une flamme de bougie (lustre et lanternes de la cabane, modèle Blender/cabane.py) :
    //   - posé sur une flamme : elle s'étire, se tasse et penche un peu, autour de sa base (l'origine de l'objet) ;
    //   - posé sur une lumière : son intensité monte et descend un peu, comme une vraie flamme.
    // Le mouvement vient d'un bruit de Perlin (doux, jamais saccadé), décalé pour chaque flamme : elles ne bougent pas ensemble.
    public class FlameFlicker : MonoBehaviour
    {
        public float speed = 6f;         // vitesse du vacillement
        public float stretch = 0.25f;    // flamme : jusqu'à ±25 % de hauteur
        public float sway = 8f;          // flamme : jusqu'à ±8° de penchant
        public float dim = 0.15f;        // lumière : jusqu'à ±15 % d'intensité

        Light lamp;
        Vector3 baseScale;
        Quaternion baseRotation;
        float baseIntensity;
        float seed;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            if (lamp) baseIntensity = lamp.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * speed;
            float n = Mathf.PerlinNoise(seed, t) * 2f - 1f;   // entre -1 et 1

            if (lamp)
            {
                lamp.intensity = baseIntensity * (1f + dim * n);
                return;
            }
            float side = Mathf.PerlinNoise(seed + 50f, t * 0.7f) * 2f - 1f;
            float height = 1f + stretch * n;
            transform.localScale = new Vector3(baseScale.x / Mathf.Sqrt(height), baseScale.y * height, baseScale.z / Mathf.Sqrt(height));
            transform.localRotation = baseRotation * Quaternion.Euler(sway * side, 0f, sway * n * 0.5f);
        }
    }
}
