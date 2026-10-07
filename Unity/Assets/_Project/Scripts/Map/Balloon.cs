using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un ballon suit la piste. Ses points de vie = ses couches (la couleur change à chaque couche perdue).
    // Sa sorte (BalloonKind) change sa taille, sa vitesse et sa résistance :
    //   Rapide = petit et vif ; Blindé = gris, moitié moins de dégâts, insensible au ralentissement ;
    //   Boss = gros ballon violet foncé et lent ; Dirigeable = le boss final rouge, énorme, insensible au ralentissement.
    // Avec les modèles 3D (BalloonVisuals) : Normal et Rapide = Ballon_Normal teinté par couche,
    //   Blindé = Ballon_Blindage, Boss = MOAB, Dirigeable = BFB.
    // S'il atteint la sortie, il retire autant de vies qu'il lui reste de couches.
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

        public float baseSpeed = 2.5f;

        WaveSpawner spawner;
        List<Vector3> path;
        int nextPoint = 1;
        float hp;
        float speed;
        float slowFactor = 1f;
        float slowUntil;
        ColorTint tint;
        ColorTint[] modelTints;   // les morceaux du modèle 3D qui prennent la couleur de la couche
        bool hasModel;

        public GameObject Model { get; private set; }               // le modèle 3D, recopié en miniature par le plateau
        public bool TintedModel => modelTints != null;              // le modèle prend la couleur de la couche

        public BalloonKind Kind { get; private set; }
        bool Armored => Kind == BalloonKind.Blinde || Kind == BalloonKind.Dirigeable;
        bool IsBlimp => Kind == BalloonKind.Boss || Kind == BalloonKind.Dirigeable;

        // Distance parcourue : les singes visent le ballon le plus avancé.
        public float Progress { get; private set; }

        public void Init(WaveSpawner owner, List<Vector3> points, int layers, BalloonKind kind)
        {
            spawner = owner;
            path = points;
            hp = layers;
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

            var box = gameObject.AddComponent<BoxCollider>();
            var fitted = turn * bounds.size * scale;
            box.size = new Vector3(Mathf.Abs(fitted.x), Mathf.Abs(fitted.y), Mathf.Abs(fitted.z));

            // Les ballons normaux et rapides changent de couleur à chaque couche ; les autres gardent leur texture.
            if (Kind == BalloonKind.Normal || Kind == BalloonKind.Rapide)
            {
                modelTints = new ColorTint[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) modelTints[i] = renderers[i].gameObject.AddComponent<ColorTint>();
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);


        public void Hit(float damage)
        {
            if (damage <= 0f || hp <= 0f) return;
            hp -= Armored ? damage * 0.5f : damage;
            if (hp <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            UpdateColor();
        }

        public void Slow(float factor, float duration)
        {
            if (Armored) return;   // le blindage ne se laisse ni geler ni coller
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        void Update()
        {
            if (Time.time > slowUntil) slowFactor = 1f;
            float step = speed * slowFactor * Time.deltaTime;
            Progress += step;

            var target = path[nextPoint];
            // Le dirigeable est couché dans le sens de la marche
            // (avec un modèle, le MOAB et le BFB regardent simplement vers où ils vont)
            if (hasModel ? IsBlimp : Kind == BalloonKind.Dirigeable)
            {
                if (target != transform.position)
                    transform.rotation = Quaternion.LookRotation(target - transform.position) * (hasModel ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f));
            }

            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                nextPoint++;
                if (nextPoint >= path.Count)
                {
                    spawner.BalloonEscaped(Mathf.CeilToInt(hp));
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
                default:
                    int layer = Mathf.Clamp(Mathf.CeilToInt(hp) - 1, 0, layerColors.Length - 1);
                    tint.Set(layerColors[layer]);
                    if (modelTints != null) foreach (var t in modelTints) t.Set(layerColors[layer]);
                    break;
            }
        }
    }
}
