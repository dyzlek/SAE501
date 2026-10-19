using UnityEngine;

namespace SAE
{
    // Le râtelier à bombes de la carte (#134) : une petite table en bois à hauteur de main, à droite du point
    // d'arrivée, avec une bombe de chaque sorte (glace, colle, clous). Une bombe lancée revient après RefillSeconds :
    // on ne peut pas tout geler en boucle, il faut choisir quand s'en servir.
    // Créé par WaveSpawner.Start, comme le panneau d'annonce.
    public class BombRack : MonoBehaviour
    {
        const float RefillSeconds = 12f;
        const float TableHeight = 0.9f;    // en mètres : à hauteur de main, rien à ramasser par terre
        const float SlotGap = 0.25f;
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);

        static readonly BombKind[] Kinds = { BombKind.Glace, BombKind.Colle, BombKind.Clous };
        readonly Bomb[] slots = new Bomb[3];
        readonly float[] refillAt = new float[3];

        public static BombRack Build(Transform parent)
        {
            var spawn = LevelSpawn.Of(Level.Carte);
            if (!spawn) return null;
            var go = new GameObject("Râtelier à bombes");
            go.transform.SetParent(parent, true);
            var forward = Vector3.ProjectOnPlane(spawn.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            go.transform.position = spawn.position + right * 0.8f + forward * 0.3f;   // à droite, un pas devant
            go.transform.rotation = Quaternion.LookRotation(forward);
            return go.AddComponent<BombRack>();
        }

        void Start()
        {
            var root = transform;
            Visuals.Solid("Plateau", root, new Vector3(0f, TableHeight - 0.03f, 0f), new Vector3(0.8f, 0.05f, 0.3f), Wood);
            Visuals.Box("Pied", root, new Vector3(0f, (TableHeight - 0.05f) / 2f, 0f), new Vector3(0.08f, TableHeight - 0.05f, 0.08f), Wood);
            Visuals.Label(root, "BOMBES", new Vector3(0f, TableHeight + 0.3f, 0.1f), 0.08f, new Color(1f, 0.83f, 0.35f), title: true);
            for (int i = 0; i < Kinds.Length; i++)
            {
                Visuals.Label(root, Kinds[i].ToString().ToUpper(), SlotPosition(i) + new Vector3(0f, 0.13f, 0f), 0.035f, Bomb.ColorOf(Kinds[i]));
                Refill(i);
            }
        }

        Vector3 SlotPosition(int i) => new Vector3((i - 1) * SlotGap, TableHeight + 0.07f, 0f);

        void Refill(int i)
        {
            slots[i] = Bomb.Create(Kinds[i], this, transform.TransformPoint(SlotPosition(i)));
            refillAt[i] = 0f;
        }

        // Une bombe vient d'être lancée : sa place se remplira dans RefillSeconds
        public void Taken(Bomb bomb)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == bomb) { slots[i] = null; refillAt[i] = Time.time + RefillSeconds; }
        }

        void Update()
        {
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i] && refillAt[i] > 0f && Time.time >= refillAt[i])
                {
                    Refill(i);
                    Sfx.Play(Sound.Ding, transform.position, 0.6f);   // « une bombe est prête »
                }
        }
    }
}
