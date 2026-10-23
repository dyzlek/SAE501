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
    // Camouflé (Camo, comme dans Bloons TD) : sa couleur tire sur le kaki et il porte des taches sombres. Seuls le Tireur
    // et les singes de rareté Violet et plus le voient (MonkeyData.SeesCamo) ; les flèches de Quincy le touchent toujours.
    // S'il atteint la sortie, il retire autant de vies qu'il lui reste de couches.
    // Un boss éclaté se divise (façon Bloons TD) : le Boss lâche 4 ballons normaux, le Dirigeable lâche 2 Boss
    // (qui se divisent à leur tour). Les petits repartent de l'endroit où il a éclaté, un peu en arrière sur la piste.
    // Coop (Coop) : chez l'hôte, chaque ballon a un double réseau (Proxy, CoopBalloon) qui le montre aux invités.
    // Chez un invité, le ballon est une copie (Remote) posée sur ce double : elle bouge avec lui, et un tir qui la perce
    // est envoyé à l'hôte, qui perce le vrai ballon.
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

        public GameObject Model { get; private set; }               // le modèle 3D, recopié en miniature par le plateau
        public bool TintedModel => modelTints != null;              // le modèle prend la couleur de la couche

        public BalloonKind Kind { get; private set; }
        public int Layers => layers;
        public bool Camo { get; private set; }
        static readonly Color CamoColor = new Color(0.36f, 0.42f, 0.2f);
        static readonly Color CamoSpot = new Color(0.22f, 0.18f, 0.1f);

        // Coop : le double réseau (hôte : celui qui nous suit ; invité : celui qui nous porte)
        public CoopBalloon Proxy { get; set; }
        public bool Remote { get; private set; }
        int hitRequest;        // notre dernier tir sur ce ballon, envoyé à l'hôte (voir ApplyHostLayers)
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
            UpdateColor();
        }

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

        // Hôte du coop : les invités le voient aussi (son double réseau). Dans Start et pas à la création :
        // un petit ballon de boss est d'abord déplacé là où le boss a éclaté (ContinueFrom), son double part de là.
        void Start() => Coop.ShareBalloon(this);

        // Hôte : le ballon éclate ou sort de la piste, son double réseau disparaît aussi chez les invités
        void OnDestroy()
        {
            if (!Remote && Proxy && Proxy.IsSpawned) Proxy.NetworkObject.Despawn();
        }


        // Un tir qui peut percer 'pierce' couches touche ce ballon. Renvoie la perforation dépensée :
        // 1 par couche, 2 par couche blindée. Un tir trop faible pour une couche blindée s'y arrête (tout est dépensé).
        public int Pop(int pierce)
        {
            if (pierce <= 0 || layers <= 0) return 0;
            int cost = Armored ? 2 : 1;
            int popped = Mathf.Min(layers, pierce / cost);
            if (popped == 0) return pierce;
            if (Remote) return PopRemote(pierce, popped, cost);

            layers -= popped;
            if (layers <= 0) { Sounds.Pop(transform.position); Split(); Destroy(gameObject); }
            else UpdateColor();
            return popped * cost;
        }

        // Coop, chez l'invité : on perce tout de suite notre copie (on voit le tir faire effet), et on prévient l'hôte.
        // Éclatée, la copie est seulement cachée : c'est l'hôte qui la fait disparaître (avec son double réseau).
        // Les petits ballons d'un boss ne sont pas créés ici : l'hôte les crée, et ils arrivent avec les autres.
        int PopRemote(int pierce, int popped, int cost)
        {
            hitRequest = Coop.SendHit(Proxy, pierce);
            layers -= popped;
            if (layers <= 0)
            {
                Sounds.Pop(transform.position);
                gameObject.SetActive(false);
            }
            else UpdateColor();
            return popped * cost;
        }

        // Coop, chez l'invité : la copie est posée sur son double réseau, elle bouge avec lui
        public void MakeRemote(CoopBalloon proxy)
        {
            Remote = true;
            Proxy = proxy;
            transform.SetParent(proxy.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // Coop, chez l'invité : les couches vues par l'hôte. Si notre dernier tir sur ce ballon n'y est pas encore fait,
        // elles sont en retard : on garde les nôtres. S'il est fait et que le ballon tient encore (on l'avait éclaté
        // chez nous, l'hôte non, à un cheveu près), il réapparaît.
        public void ApplyHostLayers(int hostLayers)
        {
            if (Coop.DoneRequest < hitRequest || hostLayers == layers) return;
            layers = hostLayers;
            gameObject.SetActive(layers > 0);
            UpdateColor();
        }

        // Le rend camouflé (WaveSpawner, après Init) : la couleur kaki, et 6 taches sombres posées sur sa surface
        public void MakeCamo()
        {
            if (Camo) return;
            Camo = true;
            for (int i = 0; i < 6; i++)
            {
                var dir = Quaternion.Euler(Random.Range(-35f, 35f), i * 60f + Random.Range(-15f, 15f), 0f) * Vector3.forward;
                var spot = Visuals.Primitive(PrimitiveType.Sphere);
                spot.name = "Tache";
                Visuals.Kill(spot.GetComponent<Collider>());
                spot.transform.SetParent(transform, false);
                spot.transform.localPosition = dir * 0.4f + Vector3.up * 0.05f;
                spot.transform.localRotation = Quaternion.LookRotation(dir);
                spot.transform.localScale = new Vector3(0.3f, 0.22f, 0.12f);
                spot.AddComponent<ColorTint>().Set(CamoSpot);
            }
            UpdateColor();
        }

        // Ce que lâche un boss éclaté : (sorte, nombre, couches). Les couches suivent la solidité du boss.
        (BalloonKind kind, int count, int layers) SplitInto() => Kind switch
        {
            BalloonKind.Boss => (BalloonKind.Normal, 4, Mathf.Clamp(maxLayers / 5, 2, 6)),
            BalloonKind.Dirigeable => (BalloonKind.Boss, 2, Mathf.Max(10, maxLayers / 4)),
            _ => (Kind, 0, 0),
        };

        const float SplitSpacing = 0.6f;   // écart entre les petits ballons, en mètres le long de la piste

        void Split()
        {
            var (kind, count, childLayers) = SplitInto();
            if (count == 0 || !spawner) return;
            for (int i = 0; i < count; i++) spawner.SpawnSplit(kind, childLayers, this, i * SplitSpacing);
            Shockwave.Spawn(transform.position, transform.localScale.x * 1.5f);
            Sounds.Play(Sounds.BallonEclate, transform.position, 1f, 0.6f);   // le même « pop », plus grave et plus fort
            Haptics.PlayBoth(Haptics.BossPop);
            Sounds.Quincy(sure: true);   // Quincy salue le boss abattu
        }

        // Un petit ballon né d'un boss : il reprend la piste là où le boss a éclaté, 'back' mètres en arrière.
        public void ContinueFrom(Balloon parent, float back)
        {
            nextPoint = parent.nextPoint;
            Progress = Mathf.Max(0f, parent.Progress - back);
            transform.position = Vector3.MoveTowards(parent.transform.position, path[nextPoint - 1], back);
        }

        public void Slow(float factor, float duration)
        {
            if (Armored) return;   // le blindage ne se laisse ni geler ni coller
            if (Remote) { Coop.SendSlow(Proxy, factor, duration); return; }   // c'est l'hôte qui le fait avancer
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        void Update()
        {
            if (Remote) return;   // une copie bouge avec son double réseau (CoopBalloon), c'est l'hôte qui la fait avancer

            // Le ballon cœur se régénère, jusqu'à son nombre de couches de départ
            if (Kind == BalloonKind.Coeur && Time.time >= nextRegen)
            {
                nextRegen = Time.time + RegenDelay;
                if (layers < maxLayers) { layers++; UpdateColor(); }
            }

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

        void UpdateColor()
        {
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
                    var color = Camo ? Color.Lerp(layerColors[layer], CamoColor, 0.55f) : layerColors[layer];   // on devine encore la couche
                    tint.Set(color);
                    if (modelTints != null) foreach (var t in modelTints) t.Set(color);
                    break;
            }
        }
    }
}
