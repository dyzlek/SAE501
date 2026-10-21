using System.Text;
using UnityEngine;

namespace SAE
{
    // Le tableau du classement, accroché au mur de la cabane (posé par PrototypeGenerator.BuildHub).
    //   En haut, l'ardoise : deux colonnes, ICI (les 10 meilleures parties jouées sur ce casque) et MONDE (le serveur).
    //   En dessous, le nom du joueur façon borne d'arcade : 3 lettres, chacune avec un bouton + au-dessus et un bouton - en dessous
    //   qu'on vise au rayon (ou qu'on touche) pour faire défiler l'alphabet. Le nom est gardé sur le casque.
    // La partie en cours est en doré. Le texte n'est réécrit que quand le classement change (Leaderboard.Changed).
    // +Z local = vers le mur, le texte se lit côté -Z.
    public class LeaderboardBoard : MonoBehaviour
    {
        const float BoardY = 2.05f, BoardW = 1.7f, BoardH = 1.0f;
        const float NameY = 1.25f;        // la rangée des lettres, sous l'ardoise
        const float LetterGap = 0.22f;

        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color DarkWood = new Color(0.22f, 0.14f, 0.07f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Arrow = new Color(0.3f, 0.75f, 0.35f);

        TextMesh here, world;
        readonly TextMesh[] letters = new TextMesh[3];

        void OnEnable() => Leaderboard.Changed += Refresh;
        void OnDisable() => Leaderboard.Changed -= Refresh;

        void Start()
        {
            if (Leaderboard.GameId == null) Leaderboard.NewGame();
            Build();
            Refresh();
            Leaderboard.Refresh();   // demande le classement MONDE au serveur
        }

        void Build()
        {
            var root = transform;
            Visuals.Box("Cadre", root, new Vector3(0f, BoardY, 0f), new Vector3(BoardW + 0.12f, BoardH + 0.12f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, BoardY, -0.03f), new Vector3(BoardW, BoardH, 0.02f), Slate);
            Visuals.Box("Trait", root, new Vector3(0f, BoardY - 0.06f, -0.045f), new Vector3(0.008f, BoardH - 0.25f, 0.004f), new Color(0.6f, 0.62f, 0.58f));
            Visuals.Text(root, "CLASSEMENT", new Vector3(0f, BoardY + 0.4f, -0.05f), 0.1f, Gold, title: true);
            here = Visuals.Text(root, "", new Vector3(-BoardW / 4f, BoardY - 0.06f, -0.05f), 0.045f, Color.white);
            world = Visuals.Text(root, "", new Vector3(BoardW / 4f, BoardY - 0.06f, -0.05f), 0.045f, Color.white);

            // Le nom : une planche, « TON NOM », 3 lettres et leurs flèches
            Visuals.Box("Planche du nom", root, new Vector3(0f, NameY, -0.02f), new Vector3(0.9f, 0.42f, 0.04f), DarkWood);
            Visuals.Text(root, "TON NOM", new Vector3(-0.3f, NameY, -0.05f), 0.05f, Gold, title: true);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 0.5f) * LetterGap + 0.08f;
                letters[i] = Visuals.Text(root, "", new Vector3(x, NameY, -0.05f), 0.11f, Color.white, title: true);
                LetterButton.Create(this, i, +1, root, new Vector3(x, NameY + 0.15f, -0.06f));
                LetterButton.Create(this, i, -1, root, new Vector3(x, NameY - 0.15f, -0.06f));
            }
        }

        // Une flèche a été appuyée : la lettre 'index' avance (+1) ou recule (-1) dans l'alphabet (Z -> A en boucle)
        public void Step(int index, int direction)
        {
            var name = Leaderboard.Initials.ToCharArray();
            name[index] = (char)('A' + ((name[index] - 'A' + direction + 26) % 26));
            Leaderboard.Initials = new string(name);   // déclenche Refresh
        }

        void Refresh()
        {
            if (!here) return;
            string name = Leaderboard.Initials;
            for (int i = 0; i < 3; i++) letters[i].text = name[i].ToString();

            var sb = new StringBuilder("<color=#FFD45A>ICI</color>\n");
            var local = Leaderboard.Local;
            if (local.Count == 0) sb.Append("Gagne une vague\npour y entrer !");
            for (int i = 0; i < local.Count; i++)
            {
                var e = local[i];
                string line = $"{i + 1}. {e.name}   vague {e.wave}";
                sb.AppendLine(e.game == Leaderboard.GameId ? $"<color=#FFD45A>{line}</color>" : line);
            }
            here.text = sb.ToString();

            sb.Clear();
            sb.AppendLine("<color=#FFD45A>MONDE</color>");
            if (!Leaderboard.OnlineReady) sb.Append(Leaderboard.OnlineFailed ? "Hors ligne" : "Connexion…");
            var online = Leaderboard.Online;
            for (int i = 0; i < online.Count; i++) sb.AppendLine($"{i + 1}. {online[i].name}   vague {online[i].wave}");
            world.text = sb.ToString();
        }

        [ContextMenu("Vider le classement local")] void TestClear() => Leaderboard.ClearLocal();
        [ContextMenu("Recharger le classement MONDE")] void TestFetch() => Leaderboard.Refresh();

        // Un bouton + ou - autour d'une lettre : à viser au rayon (RayPress) ou à toucher (HandPress)
        class LetterButton : MonoBehaviour, IPressable
        {
            LeaderboardBoard board;
            int index, direction;

            public static void Create(LeaderboardBoard board, int index, int direction, Transform parent, Vector3 pos)
            {
                var go = Visuals.Solid(direction > 0 ? "Lettre suivante" : "Lettre précédente", parent, pos, new Vector3(0.12f, 0.07f, 0.04f), Arrow);
                // Le signe est posé à côté (pas dans le cube étiré, il serait déformé) ; + et - : les polices du jeu n'ont pas de flèches
                Visuals.Text(parent, direction > 0 ? "+" : "-", pos + new Vector3(0f, 0f, -0.025f), 0.06f, Color.white, title: true);
                var button = go.AddComponent<LetterButton>();
                button.board = board;
                button.index = index;
                button.direction = direction;
                go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();   // après le collider (Solid)
                go.AddComponent<RayPress>();
            }

            public void Press() => board.Step(index, direction);
        }
    }
}
