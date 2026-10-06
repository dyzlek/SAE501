using UnityEngine;

namespace SAE
{
    // Aura « à la Dragon Ball » de la couleur de la rareté autour d'un singe :
    // des flammes qui partent du sol tout autour de lui et se resserrent en montant (forme de goutte),
    // plus une lueur douce au centre. Arc-en-ciel : chaque flamme prend une couleur au hasard.
    // Les particules suivent le singe (espace local) et grandissent ou rétrécissent avec lui.
    public class Aura : MonoBehaviour
    {
        public const float FlamesPerSecond = 70f;

        // bodySize = taille du singe en mètres (largeur, hauteur, profondeur) : l'aura s'adapte à sa forme.
        // Le singe est centré sur target, donc ses pieds sont à -hauteur/2.
        public static Aura Add(GameObject target, Rarity level, Vector3 bodySize)
        {
            var aura = new GameObject("Aura").AddComponent<Aura>();
            aura.transform.SetParent(target.transform, false);
            aura.transform.localPosition = Vector3.down * bodySize.y / 2f;
            // Les flammes partent du bord du singe : un singe fin garde une aura serrée,
            // un objet large et bas (Canon, Tireur) en a une plus large, sinon il cacherait les flammes.
            float radius = Mathf.Max(bodySize.y * 0.38f, Mathf.Max(bodySize.x, bodySize.z) * 0.5f);
            aura.Build(level, bodySize.y, radius);
            return aura;
        }

        void Build(Rarity level, float height, float radius)
        {
            var color = ColorOf(level);
            var material = MonkeyVisuals.Instance ? MonkeyVisuals.Instance.auraMaterial : null;

            // Les flammes : émises sur un cercle autour des pieds, elles montent et se resserrent vers le haut
            var flames = CreateSystem("Flammes", material);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(height * 1.1f, height * 1.6f);
            main.startSize3D = true;                       // flammes 2 fois plus hautes que larges
            main.startSizeX = new ParticleSystem.MinMaxCurve(height * 0.35f, height * 0.5f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(height * 0.7f, height * 1f);
            main.startSizeZ = 1f;
            main.startColor = color;
            main.maxParticles = 80;
            var emission = flames.emission;
            emission.rateOverTime = FlamesPerSecond;

            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;   // un cône presque droit tourné vers le haut
            shape.angle = 5f;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.radiusThickness = 0.2f;                     // surtout sur le bord : le singe reste visible au milieu

            var velocity = flames.velocityOverLifetime;   // vitesse vers le centre : les flammes se rejoignent en pointe
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = -height * 0.35f;

            var size = flames.sizeOverLifetime;           // la flamme s'affine en montant
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            FadeInOut(flames, 0.6f);
            // Toujours debout face au joueur : la pointe de la texture de flamme reste en haut
            flames.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.VerticalBillboard;

            // La lueur : quelques grosses particules pâles au centre, qui « respirent »
            var glow = CreateSystem("Lueur", material);
            glow.transform.localPosition = Vector3.up * height / 2f;
            var glowMain = glow.main;
            glowMain.startLifetime = 1f;
            glowMain.startSpeed = 0f;
            glowMain.startSize = new ParticleSystem.MinMaxCurve(height * 1.3f, height * 1.6f);
            glowMain.startColor = color;
            glowMain.maxParticles = 6;
            var glowEmission = glow.emission;
            glowEmission.rateOverTime = 4f;
            var glowShape = glow.shape;
            glowShape.enabled = false;
            FadeInOut(glow, 0.25f);

            flames.Play();
            glow.Play();
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

        ParticleSystem CreateSystem(string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // on règle tout avant de le lancer

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;      // l'aura suit le singe quand il vole
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;          // et rétrécit avec lui
            main.prewarm = true;                                             // déjà allumée dès qu'elle apparaît

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            return ps;
        }

        // Les particules apparaissent puis s'effacent en douceur (sinon elles « clignotent »).
        static void FadeInOut(ParticleSystem ps, float maxAlpha)
        {
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(maxAlpha, 0.25f), new GradientAlphaKey(0f, 1f) });
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = fade;
        }
    }
}
