using UnityEngine;

namespace SAE
{
    // Aura « à la Dragon Ball » autour d'un singe, de la couleur de sa rareté.
    // Le visuel est le prefab Assets/_Project/Prefabs/Aura.prefab (réglé dans l'Inspector, pour un singe de 1 m) :
    // - « Flammes » : des flammes qui partent du sol tout autour du singe et se resserrent en montant ;
    // - « Lueur » : quelques grosses particules pâles au centre.
    // Ce script ne fait que colorer l'aura et l'adapter à la taille et à la forme du singe.
    public class Aura : MonoBehaviour
    {
        public ParticleSystem flames;
        public ParticleSystem glow;

        // Optimisation : chaque aura fait ~70 flammes par seconde ; avec 200 singes, ça fait 16 000 particules.
        //  - au-delà de FullCount auras, chacune émet moins (le total reste à peu près celui de FullCount auras) ;
        //  - loin du joueur (au-delà de FarDistance), l'aura s'éteint : de loin, on voit la couleur du singe, pas les flammes.
        // Vérifié 2 fois par seconde seulement (pas besoin de plus pour un réglage qui change rarement).
        const int FullCount = 25;
        const float MinShare = 0.15f;      // jamais moins de 15 % des flammes (l'aura reste visible de près)
        const float FarDistance = 12f;     // en mètres
        const float CheckDelay = 0.5f;
        static readonly System.Collections.Generic.List<Aura> all = new System.Collections.Generic.List<Aura>();
        static Transform eye;              // la caméra du joueur
        float flamesRate, glowRate;        // émission d'origine du prefab
        float nextCheck;

        void Awake()
        {
            flamesRate = flames.emission.rateOverTimeMultiplier;
            glowRate = glow.emission.rateOverTimeMultiplier;
            nextCheck = Random.value * CheckDelay;   // étalé : les auras ne font pas toutes le calcul à la même image
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + CheckDelay;

            float share = all.Count <= FullCount ? 1f : Mathf.Max(MinShare, FullCount / (float)all.Count);
            if (!eye && Camera.main) eye = Camera.main.transform;
            if (eye && (eye.position - transform.position).sqrMagnitude > FarDistance * FarDistance) share = 0f;

            var flamesEmission = flames.emission;
            flamesEmission.rateOverTimeMultiplier = flamesRate * share;
            var glowEmission = glow.emission;
            glowEmission.rateOverTimeMultiplier = glowRate * share;
        }

        // Rayon minimum du cercle de flammes, en part de la hauteur du singe (un singe fin garde une aura serrée).
        public const float MinRadius = 0.38f;

        // Pose une aura sur target. bodySize = taille du singe en mètres (largeur, hauteur, profondeur).
        // Le singe est centré sur target, donc ses pieds sont à -hauteur/2.
        public static Aura Add(GameObject target, Rarity level, Vector3 bodySize) => Add(target, ColorOf(level), bodySize);

        // Même aura, d'une couleur choisie (ex. l'aura dorée du coffre qui s'ouvre).
        public static Aura Add(GameObject target, ParticleSystem.MinMaxGradient color, Vector3 bodySize)
        {
            var prefab = MonkeyVisuals.Instance ? MonkeyVisuals.Instance.auraPrefab : null;
            if (!prefab) return null;

            var aura = Instantiate(prefab, target.transform);
            aura.name = "Aura";
            aura.Fit(bodySize);
            aura.SetColor(color);
            aura.Restart();
            return aura;
        }

        // Le prefab est fait pour un singe de 1 m : on le met à l'échelle de la hauteur du singe
        // (les particules suivent, grâce au mode « Hierarchy »), puis on élargit le cercle de flammes
        // pour les objets larges et bas (Canon, Tireur), sinon ils cacheraient les flammes.
        void Fit(Vector3 bodySize)
        {
            float height = bodySize.y;
            transform.localPosition = Vector3.down * height / 2f;
            transform.localScale = Vector3.one * height;

            float halfWidth = Mathf.Max(bodySize.x, bodySize.z) / 2f;
            var shape = flames.shape;
            shape.radius = Mathf.Max(MinRadius, halfWidth / height);
        }

        void SetColor(ParticleSystem.MinMaxGradient color)
        {
            var flamesMain = flames.main;
            flamesMain.startColor = color;
            var glowMain = glow.main;
            glowMain.startColor = color;
        }

        // Le prefab démarre tout seul (déjà « allumé » grâce au prewarm), mais avec sa couleur et sa taille
        // d'origine : on le relance pour que les premières flammes aient déjà la bonne couleur.
        void Restart()
        {
            foreach (var ps in new[] { flames, glow })
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        // Rareté → couleur des particules. Arc-en-ciel = une couleur au hasard par particule.
        static ParticleSystem.MinMaxGradient ColorOf(Rarity level)
        {
            if (!MonkeyData.IsRainbow(level)) return MonkeyData.RarityColor(level);

            var rainbow = new Gradient();
            rainbow.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.2f),
                    new GradientColorKey(Color.green, 0.4f), new GradientColorKey(Color.cyan, 0.6f),
                    new GradientColorKey(Color.blue, 0.8f), new GradientColorKey(Color.magenta, 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(rainbow) { mode = ParticleSystemGradientMode.RandomColor };
        }
    }
}
