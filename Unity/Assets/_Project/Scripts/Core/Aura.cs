using UnityEngine;

namespace SAE
{
    // Aura lumineuse de la couleur de la rareté autour d'un singe :
    // une sphère transparente qui « respire » + une petite lumière qui éclaire le décor autour.
    // Le matériau (transparent, additif) est créé par le générateur : Assets/_Project/Art/Materials/Aura.mat.
    public class Aura : MonoBehaviour
    {
        public float size = 0.6f;        // diamètre de l'aura, en mètres
        public float pulseSpeed = 4f;

        Transform glow;
        Light lightSource;
        float baseIntensity;

        public static Aura Add(GameObject target, Color color, Material material, float diameter)
        {
            var aura = target.AddComponent<Aura>();
            aura.size = diameter;
            aura.Build(color, material);
            return aura;
        }

        void Build(Color color, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Aura";
            Visuals.Kill(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(transform, false);
            if (material) sphere.GetComponent<Renderer>().sharedMaterial = material;
            sphere.AddComponent<ColorTint>().Set(new Color(color.r, color.g, color.b, 0.35f));
            glow = sphere.transform;

            lightSource = gameObject.AddComponent<Light>();
            lightSource.type = LightType.Point;
            lightSource.color = color;
            lightSource.range = 2f;
            baseIntensity = lightSource.intensity = 2f;
        }

        void Update()
        {
            if (!glow) return;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * pulseSpeed);
            glow.localScale = Vector3.one * size * pulse;
            lightSource.intensity = baseIntensity * pulse;
        }
    }
}
