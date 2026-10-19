using System.Text;
using UnityEngine;

namespace SAE
{
    // Le mur des records de la cabane (#139, #140, #141) : au mur, en face de la caisse.
    //   En haut, une ardoise : les records de la partie en cours, puis le classement local (les 10 meilleures parties,
    //   gardées sur le casque, la partie en cours en doré).
    //   En dessous, une étagère de trophées : une coupe par palier (vague 10, 50, 100, 1 000 bananes…),
    //   sombre tant qu'elle n'est pas gagnée, dorée ensuite, avec son nom gravé dessous.
    // Il se construit tout seul (des cubes, comme les autres panneaux du jeu). Posé par PrototypeGenerator (BuildHub).
    // +Z local = vers le mur, le texte se lit côté -Z.
    public class RecordWall : MonoBehaviour
    {
        const float BoardWidth = 1.5f, BoardHeight = 1.1f;
        const float BoardY = 2.0f;         // centre de l'ardoise, en mètres
        const float ShelfY = 1.15f;        // l'étagère des trophées, à hauteur d'yeux bas : on les voit sans se baisser
        const float TrophyGap = 0.19f;

        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Unearned = new Color(0.22f, 0.22f, 0.24f);

        TextMesh records, board;
        ColorTint[][] cups;        // les morceaux de chaque coupe, à recolorer quand elle est gagnée
        float[] shine;             // l'éclat d'une coupe qu'on vient de gagner (1 → 0)
        Transform[] cupRoots;

        void OnEnable()
        {
            Records.Changed += Refresh;
            Records.TrophyEarned += OnTrophy;
            Economy.MoneyChanged += OnMoney;   // les bananes gagnées montent en direct
        }

        void OnDisable()
        {
            Records.Changed -= Refresh;
            Records.TrophyEarned -= OnTrophy;
            Economy.MoneyChanged -= OnMoney;
        }

        void Start()
        {
            if (Records.Current == null) Records.NewGame();   // premier passage au hub : la partie commence
            Build();
            Refresh();
        }

        void Build()
        {
            var root = transform;
            Visuals.Box("Cadre", root, new Vector3(0f, BoardY, 0f), new Vector3(BoardWidth + 0.12f, BoardHeight + 0.12f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, BoardY, -0.03f), new Vector3(BoardWidth, BoardHeight, 0.02f), Slate);
            Visuals.Text(root, "RECORDS", new Vector3(0f, BoardY + 0.44f, -0.05f), 0.11f, Gold, title: true);
            records = Visuals.Text(root, "", new Vector3(-0.38f, BoardY + 0.05f, -0.05f), 0.05f, Color.white);
            board = Visuals.Text(root, "", new Vector3(0.36f, BoardY - 0.04f, -0.05f), 0.042f, Color.white);

            // L'étagère et ses trophées
            int n = Records.Trophies.Length;
            float width = n * TrophyGap + 0.1f;
            Visuals.Box("Étagère", root, new Vector3(0f, ShelfY, -0.1f), new Vector3(width, 0.04f, 0.22f), Wood);
            cups = new ColorTint[n][];
            cupRoots = new Transform[n];
            shine = new float[n];
            for (int i = 0; i < n; i++)
            {
                var cup = new GameObject("Trophée " + Records.Trophies[i].title).transform;
                cup.SetParent(root, false);
                cup.localPosition = new Vector3((i - (n - 1) / 2f) * TrophyGap, ShelfY + 0.02f, -0.1f);
                cupRoots[i] = cup;
                // Une coupe en 4 cubes : socle, pied, coupe, et une « anse » de chaque côté
                cups[i] = new[]
                {
                    Part(cup, new Vector3(0f, 0.015f, 0f), new Vector3(0.09f, 0.03f, 0.09f)),
                    Part(cup, new Vector3(0f, 0.06f, 0f), new Vector3(0.025f, 0.06f, 0.025f)),
                    Part(cup, new Vector3(0f, 0.13f, 0f), new Vector3(0.1f, 0.09f, 0.1f)),
                    Part(cup, new Vector3(0f, 0.14f, 0f), new Vector3(0.15f, 0.02f, 0.03f)),
                };
                Visuals.Text(cup, Records.Trophies[i].title, new Vector3(0f, -0.05f, -0.12f), 0.022f, new Color(0.85f, 0.8f, 0.7f));
            }
        }

        static ColorTint Part(Transform parent, Vector3 pos, Vector3 size) =>
            Visuals.Box("Coupe", parent, pos, size, Unearned).GetComponent<ColorTint>();

        void Refresh()
        {
            if (!records) return;

            var sb = new StringBuilder();
            sb.AppendLine("<color=#FFD45A>CETTE PARTIE</color>");
            sb.AppendLine($"Vague : {GameState.BestWave}");
            sb.AppendLine($"Combo : x{GameState.BestCombo}");
            sb.AppendLine($"Bananes : {Records.Bananas}");
            sb.AppendLine($"Ballons : {Records.BalloonsPopped}");
            int rank = Records.CurrentRank();
            sb.Append(rank > 0 ? $"<color=#FFD45A>{rank}e du classement</color>" : "Gagne une vague\npour entrer au classement");
            records.text = sb.ToString();

            sb.Clear();
            sb.AppendLine("<color=#FFD45A>CLASSEMENT</color>");
            var list = Records.Board;
            if (list.Count == 0) sb.Append("Personne pour l'instant !");
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                bool mine = Records.Current != null && e.game == Records.Current.game;
                string line = $"{i + 1}. {e.name}  {e.date}   vague {e.wave}   x{e.combo}";
                sb.AppendLine(mine ? $"<color=#FFD45A>{line}</color>" : line);
            }
            board.text = sb.ToString();

            for (int i = 0; i < cups.Length; i++)
                foreach (var part in cups[i]) part.Set(Records.Has(i) ? Gold : Unearned);
        }

        void OnMoney(int delta, Vector3? where) { if (delta > 0) Refresh(); }

        void OnTrophy(Records.Trophy trophy)
        {
            int i = System.Array.IndexOf(Records.Trophies, trophy);
            if (i >= 0 && shine != null) shine[i] = 1f;
            Sfx.Play(Sound.Victory, transform.position, 0.8f);
            Refresh();
        }

        // Une coupe qu'on vient de gagner tourne sur elle-même et grossit un instant
        void Update()
        {
            if (shine == null) return;
            for (int i = 0; i < shine.Length; i++)
            {
                if (shine[i] <= 0f) continue;
                shine[i] = Mathf.MoveTowards(shine[i], 0f, Time.deltaTime / 3f);
                cupRoots[i].localRotation = Quaternion.Euler(0f, shine[i] * 720f, 0f);
                cupRoots[i].localScale = Vector3.one * (1f + 0.4f * Mathf.Sin(shine[i] * Mathf.PI));
            }
        }

        // Pour tester sans jouer : en mode Play, clic droit sur le composant
        [ContextMenu("Effacer les records et les trophées")] void TestClear() => Records.Clear();
        [ContextMenu("Rafraîchir")] void TestRefresh() => Refresh();
    }
}
