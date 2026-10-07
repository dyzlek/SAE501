using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un ballon suit la piste. Sa vie = ses couches (la couleur change à chaque couche perdue).
    // Il n'y a pas de « dégâts » : un tir a une perforation, le nombre de couches qu'il peut percer (voir Pop).
    // Sa sorte (BalloonKind) change sa taille, sa vitesse et sa résistance :
    //   Rapide = petit et vif ; Blindé = gris, chaque couche coûte 2 de perforation, insensible au ralentissement ;
    //   Boss = gros ballon violet foncé et lent ; Dirigeable = le boss final rouge, énorme, insensible au ralentissement.
    // Avec les modèles 3D (BalloonVisuals) : Normal et Rapide = Ballon_Normal teinté par couche,
    //   Blindé = Ballon_Blindage, Boss = MOAB (hélice qui tourne), Dirigeable = BFB (2 hélices), Coeur = Ballon_Coeur.
    // Le ballon cœur regagne une couche toutes les 2 s (règle du GDD : il se régénère).
    // S'il atteint la sortie, il retire autant de vies qu'il lui reste de couches.
    // Ce qu'on voit de sa vie (critique du 7 oct. : on ne savait pas où en était un gros ballon) :
    //   - chaque couche percée : un « pop » et le ballon se gonfle un instant ;
    //   - les gros (BarFrom couches ou plus : boss, dirigeable) ont une barre de vie au-dessus d'eux, verte puis rouge ;
    //   - éclaté : un pop plus fort et une gerbe de confettis de sa couleur.
    public class Balloon : MonoBehaviour
    {
        public static readonly List<Balloon> All = new List<Balloon>();

        static readonly Color[] layerColors =
        {
            new Color(0.9f, 0.1f, 0.1f), new Color(0.2f, 0.5f, 1f), new Color(0.2f, 0.8f, 0.2f),
            new Color(1f, 0.9f, 0.1f), new Color(1f, 0.4f, 0.8f), new Color(0.1f, 0.1f, 0.1f),
        };
        static readonly Color ArmorColor = new Color(0.55f, 0.57f, 0.6f);
        static readonly Color BossColor = new Color(0.35f, 0.1f, 0.45f);
        static readonly Color BlimpColor = new Color(0.8f, 0.1f, 0.1f);
        static readonly Color HeartColor = new Color(1f, 0.3f, 0.55f);

        const float RegenDelay = 2f;   // le ballon cœur regagne une couche toutes les 2 s
        const int BarFrom = 5;         // à partir de 5 couches, une barre de vie
        const float BarWidth = 1.2f, BarHeight = 0.12f;   // en mètres

        public float baseSpeed = 2.5f;

        WaveSpawner spawner;
        List<Vector3> path;
        int nextPoint = 1;
        int layers;
        int maxLayers;
        float nextRegen;
        bool facingSet;
        const float TurnSpeed = 60f;   // degrés par seconde : un dirigeable prend ~1,5 s pour un virage à angle droit
        float speed;
        float slowFactor = 1f;
        float slowUntil;
        ColorTint tint;
        ColorTint[] modelTints;   // les morceaux du modèle 3D qui prennent la couleur de la couche
        bool hasModel;
        Transform bar, barFill;   // la barre de vie des gros ballons (hors du ballon : elle ne tourne pas avec lui)
        ColorTint barTint;
        Vector3 baseScale;
        float punch;              // 1 = vient d'être touché (il gonfle), revient à 0

        public GameObject Model { get; private set; }               // le modèle 3D, recopié en miniature par le plateau
        public bool TintedModel => modelTints != null;              // le modèle prend la couleur de la couche

        public BalloonKind Kind { get; private set; }
        bool Armored => Kind == BalloonKind.Blinde || Kind == BalloonKind.Dirigeable;
        bool IsBlimp => Kind == BalloonKind.Boss || Kind == BalloonKind.Dirigeable;

        // Distance parcourue : les singes visent le ballon le plus avancé.
        public float Progress { get; private set; }

        public void Init(WaveSpawner owner, List<Vector3> points, int layerCount, BalloonKind kind)
        {
            spawner = owner;
            path = points;
            layers = layerCount;
            maxLayers = layerCount;
            nextRegen = Time.time + RegenDelay;
            Kind = kind;
            transform.position = path[0];
            tint = GetComponent<ColorTint>();

            // Taille et vitesse selon la sorte
            (float size, float speedFactor) = kind switch
            {
                BalloonKind.Rapide => (0.65f, 1.7f),
                BalloonKind.Blinde => (1f, 0.8f),
                BalloonKind.Boss => (1.8f, 0.6f),
                BalloonKind.Dirigeable => (2.2f, 0.35f),
                _ => (0.9f, 1f),
            };
            var modelAsset = BalloonVisuals.Model(kind);
            hasModel = modelAsset;
            if (hasModel)
            {
                // Les dirigeables (MOAB, BFB) sont longs : leur taille est leur longueur, on les grandit un peu.
                if (IsBlimp) size *= 1.5f;
                transform.localScale = Vector3.one * size;
                AddModel(modelAsset);
            }
            else
                transform.localScale = kind == BalloonKind.Dirigeable ? new Vector3(size, size * 1.6f, size) : Vector3.one * size;
            speed = baseSpeed * speedFactor;
            baseScale = transform.localScale;
            if (maxLayers >= BarFrom) CreateBar();
            UpdateColor();
        }

        // La barre de vie : un fond sombre et une jauge colorée, au-dessus du ballon, tournée vers le joueur
        void CreateBar()
        {
            bar = new GameObject("Barre de vie").transform;
            bar.gameObject.AddComponent<Billboard>();
            Visuals.Box("Fond", bar, new Vector3(0, 0, 0.01f), new Vector3(BarWidth + 0.06f, BarHeight + 0.06f, 0.01f), new Color(0.1f, 0.1f, 0.1f));
            barFill = Visuals.Box("Jauge", bar, Vector3.zero, new Vector3(BarWidth, BarHeight, 0.02f), Color.green).transform;
            barTint = barFill.GetComponent<ColorTint>();
        }

        void UpdateBar()
        {
            if (!bar) return;
            float k = Mathf.Clamp01((float)layers / maxLayers);
            barFill.localScale = new Vector3(BarWidth * k, BarHeight, 0.02f);
            barFill.localPosition = new Vector3(-BarWidth * (1f - k) / 2f, 0f, 0f);   // la jauge se vide vers la gauche
            barTint.Set(Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(0.2f, 0.85f, 0.25f), k));
        }

        void OnDestroy() { if (bar) Destroy(bar.gameObject); }

        // Pose le modèle 3D, ramené à une taille de 1 avant l'échelle, et un collider à sa forme
        // pour que les flèches et projectiles le touchent.
        void AddModel(GameObject asset)
        {
            var model = Instantiate(asset);
            model.name = "Modele";
            model.transform.SetPositionAndRotation(Vector3.zero, asset.transform.rotation);

            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            // Un dirigeable avance dans le sens de son grand axe : on le tourne pour qu'il soit le long de +Z.
            var turn = Quaternion.identity;
            if (IsBlimp && bounds.size.x > bounds.size.z) turn = Quaternion.Euler(0f, 90f, 0f);
            float scale = 1f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            model.transform.SetParent(transform, false);
            model.transform.localRotation = turn * asset.transform.rotation;
            model.transform.localScale = asset.transform.localScale * scale;
            model.transform.localPosition = turn * -bounds.center * scale;
            Model = model;
            BalloonVisuals.ApplyTexture(model, Kind);

            // Les hélices tournent autour du grand axe du dirigeable (le +Z du ballon)
            var localAxis = model.transform.InverseTransformDirection(transform.forward);
            foreach (var r in renderers)
                if (r.name.Contains("Helice"))
                {
                    var spinner = r.gameObject.AddComponent<Spinner>();
                    spinner.axisRef = model.transform;
                    spinner.localAxis = localAxis;
                }

            var box = gameObject.AddComponent<BoxCollider>();
            var fitted = turn * bounds.size * scale;
            box.size = new Vector3(Mathf.Abs(fitted.x), Mathf.Abs(fitted.y), Mathf.Abs(fitted.z));

            // Les ballons normaux et rapides changent de couleur à chaque couche, le cœur est rose ; les autres gardent leur texture.
            if (Kind == BalloonKind.Normal || Kind == BalloonKind.Rapide || Kind == BalloonKind.Coeur)
            {
                modelTints = new ColorTint[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) modelTints[i] = renderers[i].gameObject.AddComponent<ColorTint>();
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);


        // Un tir qui peut percer 'pierce' couches touche ce ballon. Renvoie la perforation dépensée :
        // 1 par couche, 2 par couche blindée. Un tir trop faible pour une couche blindée s'y arrête (tout est dépensé).
        public int Pop(int pierce)
        {
            if (pierce <= 0 || layers <= 0) return 0;
            int cost = Armored ? 2 : 1;
            int popped = Mathf.Min(layers, pierce / cost);
            if (popped == 0) return pierce;

            layers -= popped;
            if (layers <= 0)
            {
                Sfx.Play(Sfx.Sound.Pop, transform.position, 0.8f, IsBlimp ? 0.6f : 1f);   // un gros ballon éclate plus grave
                Confetti(transform.position, CurrentColor, IsBlimp ? 60 : 15);
                Destroy(gameObject);
            }
            else
            {
                Sfx.Play(Sfx.Sound.Pop, transform.position, 0.35f, 1.3f);
                punch = 1f;
                UpdateColor();
            }
            return popped * cost;
        }

        public void Slow(float factor, float duration)
        {
            if (Armored) return;   // le blindage ne se laisse ni geler ni coller
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        void Update()
        {
            // Le ballon cœur se régénère, jusqu'à son nombre de couches de départ
            if (Kind == BalloonKind.Coeur && Time.time >= nextRegen)
            {
                nextRegen = Time.time + RegenDelay;
                if (layers < maxLayers) { layers++; UpdateColor(); }
            }

            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 6f);
            transform.localScale = baseScale * (1f + 0.15f * punch);
            if (bar) bar.position = transform.position + Vector3.up * (baseScale.y * 0.7f + 0.4f);

            if (Time.time > slowUntil) slowFactor = 1f;
            float step = speed * slowFactor * Time.deltaTime;
            Progress += step;

            var target = path[nextPoint];
            // Le dirigeable est couché dans le sens de la marche
            // (avec un modèle, le MOAB et le BFB regardent simplement vers où ils vont)
            if (hasModel ? IsBlimp : Kind == BalloonKind.Dirigeable)
            {
                if (target != transform.position)
                {
                    var wanted = Quaternion.LookRotation(target - transform.position) * (hasModel ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f));
                    // Au départ il est déjà dans le bon sens ; ensuite il tourne en douceur dans les virages
                    transform.rotation = facingSet ? Quaternion.RotateTowards(transform.rotation, wanted, TurnSpeed * slowFactor * Time.deltaTime) : wanted;
                    facingSet = true;
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                nextPoint++;
                if (nextPoint >= path.Count)
                {
                    spawner.BalloonEscaped(layers);
                    Destroy(gameObject);
                }
            }
        }

        // La couleur du ballon en ce moment (pour ses confettis)
        Color CurrentColor => Kind switch
        {
            BalloonKind.Blinde => ArmorColor,
            BalloonKind.Boss => BossColor,
            BalloonKind.Dirigeable => BlimpColor,
            BalloonKind.Coeur => HeartColor,
            _ => layerColors[Mathf.Clamp(layers - 1, 0, layerColors.Length - 1)],
        };

        // Une gerbe de petits morceaux de caoutchouc qui volent et retombent (un seul « burst », puis l'objet disparaît)
        static void Confetti(Vector3 position, Color color, int count)
        {
            var go = new GameObject("Confettis");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = color;
            main.gravityModifier = 1.5f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Visuals.LineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        void UpdateColor()
        {
            UpdateBar();
            if (!tint) return;
            switch (Kind)
            {
                case BalloonKind.Blinde: tint.Set(ArmorColor); break;
                case BalloonKind.Boss: tint.Set(BossColor); break;
                case BalloonKind.Dirigeable: tint.Set(BlimpColor); break;
                case BalloonKind.Coeur:
                    tint.Set(HeartColor);
                    if (modelTints != null) foreach (var t in modelTints) t.Set(HeartColor);
                    break;
                default:
                    int layer = Mathf.Clamp(layers - 1, 0, layerColors.Length - 1);
                    tint.Set(layerColors[layer]);
                    if (modelTints != null) foreach (var t in modelTints) t.Set(layerColors[layer]);
                    break;
            }
        }
    }
}
