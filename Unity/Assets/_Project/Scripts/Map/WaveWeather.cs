using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // La météo des vagues annoncées (#131) : brouillard, nuit ou pluie, sur la carte seulement.
    // Ajouté par WaveSpawner, qui appelle Set() au début et à la fin de chaque vague (WaveBook.WeatherOf choisit le temps).
    // Le changement se fait en douceur (TransitionSeconds) : pas de flash, rien de brusque pour les yeux en VR.
    // Coût léger pour le Quest : brouillard linéaire d'URP, une lumière baissée, et ~400 gouttes au plus autour du joueur.
    // Les réglages d'ambiance (RenderSettings) appartiennent à la scène active : on ne les touche que quand le joueur est
    // sur la carte, pour ne pas assombrir la cabane. Unity les garde avec la scène, ils reviennent quand on y retourne.
    public class WaveWeather : MonoBehaviour
    {
        const float TransitionSeconds = 3f;
        const float NightLight = 0.25f;        // la lumière du soleil la nuit (× l'intensité de départ)
        const float NightAmbient = 0.35f;
        static readonly Color FogColor = new Color(0.72f, 0.76f, 0.78f);
        static readonly Color NightColor = new Color(0.05f, 0.07f, 0.15f);
        static readonly Color RainColor = new Color(0.45f, 0.5f, 0.55f);

        Weather target = Weather.Clair;
        float amount;                          // 0 = temps clair, 1 = temps 'shown' à fond
        Weather shown = Weather.Clair;         // le temps qui s'affiche (il s'efface avant qu'un autre arrive)

        Light sun;
        float sunIntensity, ambientIntensity;
        bool fogWas;
        Color fogColorWas;
        float fogStartWas, fogEndWas;
        FogMode fogModeWas;
        ParticleSystem rain;

        public void Set(Weather weather) => target = weather;

        void Start()
        {
            // Le soleil de la carte (la lumière directionnelle de cette scène), et l'ambiance de départ
            foreach (var l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional && l.gameObject.scene == gameObject.scene) { sun = l; break; }
            if (sun) sunIntensity = sun.intensity;
            ambientIntensity = RenderSettings.ambientIntensity;
            fogWas = RenderSettings.fog;
            fogColorWas = RenderSettings.fogColor;
            fogModeWas = RenderSettings.fogMode;
            fogStartWas = RenderSettings.fogStartDistance;
            fogEndWas = RenderSettings.fogEndDistance;
        }

        void Update()
        {
            // D'abord on revient au clair, puis le nouveau temps monte
            float goal = target == shown ? 1f : 0f;
            amount = Mathf.MoveTowards(amount, goal, Time.deltaTime / TransitionSeconds);
            if (amount == 0f) shown = target;
            if (shown == Weather.Clair) amount = 0f;

            if (SceneManager.GetActiveScene() != gameObject.scene) { SetRain(0f); return; }
            Apply();
        }

        void Apply()
        {
            float fog = shown != Weather.Clair ? amount : 0f;   // les trois temps voilent l'horizon
            if (fog > 0f)
            {
                // Brouillard épais (on voit à ~12 m), plus léger sous la pluie, sombre la nuit
                Color c = shown == Weather.Nuit ? NightColor : shown == Weather.Pluie ? RainColor : FogColor;
                float end = shown == Weather.Brouillard ? 12f : shown == Weather.Pluie ? 35f : 40f;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = Color.Lerp(fogWas ? fogColorWas : c, c, fog);
                RenderSettings.fogStartDistance = Mathf.Lerp(fogWas ? fogStartWas : 200f, 2f, fog);
                RenderSettings.fogEndDistance = Mathf.Lerp(fogWas ? fogEndWas : 300f, end, fog);
            }
            else
            {
                RenderSettings.fog = fogWas;
                RenderSettings.fogColor = fogColorWas;
                RenderSettings.fogMode = fogModeWas;
                RenderSettings.fogStartDistance = fogStartWas;
                RenderSettings.fogEndDistance = fogEndWas;
            }

            // La nuit : le soleil et l'ambiance baissent (la pluie assombrit un peu aussi)
            float dark = shown == Weather.Nuit ? amount : shown == Weather.Pluie ? amount * 0.4f : 0f;
            if (sun) sun.intensity = sunIntensity * Mathf.Lerp(1f, NightLight, dark);
            RenderSettings.ambientIntensity = ambientIntensity * Mathf.Lerp(1f, NightAmbient, dark);

            SetRain(shown == Weather.Pluie ? amount : 0f);
        }

        // La pluie suit la tête du joueur : un carré de 16 m de côté au-dessus de lui, inutile d'arroser toute la carte
        void SetRain(float strength)
        {
            if (strength <= 0f && !rain) return;
            if (!rain) rain = BuildRain();
            var player = PlayerRig.Local;
            if (player) rain.transform.position = player.HeadPosition + Vector3.up * 6f;
            var emission = rain.emission;
            emission.rateOverTime = 350f * strength;
            if (strength > 0f && !rain.isPlaying) rain.Play();
        }

        ParticleSystem BuildRain()
        {
            var ps = new GameObject("Pluie").AddComponent<ParticleSystem>();
            ps.transform.SetParent(transform, true);
            ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // la boîte émet vers le bas

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // les gouttes ne suivent pas la tête une fois lâchées
            main.startLifetime = 0.8f;
            main.startSpeed = 11f;
            main.startSize = 0.02f;
            main.startColor = new Color(0.75f, 0.82f, 0.95f, 0.5f);
            main.maxParticles = 400;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 16f, 0.1f);

            // Des traits étirés dans le sens de la chute, avec le matériau des lignes du jeu (déjà dans le build)
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f;
            renderer.sharedMaterial = Visuals.LineMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        // Si la carte se décharge pendant une vague, on rend à la scène son ciel de départ
        void OnDestroy()
        {
            if (sun) sun.intensity = sunIntensity;
        }

        // Pour voir chaque temps sans jouer 15 vagues : en mode Play, clic droit sur le composant
        [ContextMenu("Tester le brouillard")] void TestFog() => Set(Weather.Brouillard);
        [ContextMenu("Tester la nuit")] void TestNight() => Set(Weather.Nuit);
        [ContextMenu("Tester la pluie")] void TestRain() => Set(Weather.Pluie);
        [ContextMenu("Revenir au clair")] void TestClear() => Set(Weather.Clair);
    }
}
