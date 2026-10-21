using System.Text;
using UnityEngine;

namespace SAE
{
    // Le grand panneau du classement, DEHORS, dans la bananeraie : derrière la roulette, face à la terrasse
    // (posé par PrototypeGenerator.BuildGrove). Un panneau d'affichage en bois sur deux poteaux, avec un petit toit :
    //   l'ardoise : CLASSEMENT en grand, deux colonnes ICI (les 10 meilleures parties de ce casque) et MONDE (le serveur),
    //   médailles or / argent / bronze sur les 3 premiers, la partie en cours en vert ;
    //   en dessous, le nom du joueur façon borne d'arcade : 3 lettres, un bouton + au-dessus et un bouton - en dessous
    //   de chacune, qu'on vise au rayon pour faire défiler l'alphabet. Le nom est gardé sur le casque.
    // Le texte n'est réécrit que quand le classement change (Leaderboard.Changed). Que des cubes : rien de coûteux.
    // +Z local = dos au joueur, le texte se lit côté -Z.
    public class LeaderboardBoard : MonoBehaviour
    {
        const float BoardY = 2.6f, BoardW = 3.2f, BoardH = 1.9f;   // l'ardoise, en mètres : lisible depuis la terrasse
        const float NameY = 1.15f;        // la planche du nom, sous l'ardoise, à hauteur de poitrine
        const float LetterGap = 0.34f;
        const float PostX = BoardW / 2f + 0.15f;

        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color DarkWood = new Color(0.22f, 0.14f, 0.07f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Thatch = new Color(0.72f, 0.58f, 0.3f);
        static readonly Color Arrow = new Color(0.3f, 0.75f, 0.35f);
        static readonly string[] Medals = { "#FFD45A", "#D9DEE8", "#D08A4A" };   // or, argent, bronze

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
            float top = BoardY + BoardH / 2f;

            // La charpente : deux poteaux plantés dans l'herbe, une traverse, un petit toit de chaume (comme la cabane)
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Poteau", root, new Vector3(side * PostX, (top + 0.4f) / 2f, 0.05f), new Vector3(0.18f, top + 0.4f, 0.18f), DarkWood);
            Visuals.Box("Toit", root, new Vector3(0f, top + 0.45f, -0.1f), new Vector3(BoardW + 0.9f, 0.12f, 0.75f), Thatch).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Visuals.Box("Traverse", root, new Vector3(0f, NameY - 0.32f, 0.05f), new Vector3(2f * PostX, 0.1f, 0.1f), DarkWood);

            // L'ardoise et son cadre : bois, puis un liseré doré
            Visuals.Box("Cadre", root, new Vector3(0f, BoardY, 0f), new Vector3(BoardW + 0.24f, BoardH + 0.24f, 0.08f), Wood);
            Visuals.Box("Liseré", root, new Vector3(0f, BoardY, -0.035f), new Vector3(BoardW + 0.06f, BoardH + 0.06f, 0.02f), Gold);
            Visuals.Box("Ardoise", root, new Vector3(0f, BoardY, -0.045f), new Vector3(BoardW, BoardH, 0.02f), Slate);
            Visuals.Box("Trait", root, new Vector3(0f, BoardY - 0.15f, -0.06f), new Vector3(0.012f, BoardH - 0.6f, 0.004f), new Color(0.6f, 0.62f, 0.58f));

            // L'enseigne dorée au-dessus : CLASSEMENT
            Visuals.Box("Enseigne", root, new Vector3(0f, BoardY + 0.68f, -0.07f), new Vector3(1.9f, 0.4f, 0.05f), DarkWood);
            Visuals.Text(root, "CLASSEMENT", new Vector3(0f, BoardY + 0.68f, -0.1f), 0.26f, Gold, title: true);
            here = Visuals.Text(root, "", new Vector3(-BoardW / 4f, BoardY - 0.18f, -0.07f), 0.09f, Color.white);
            world = Visuals.Text(root, "", new Vector3(BoardW / 4f, BoardY - 0.18f, -0.07f), 0.09f, Color.white);

            // Le nom : une planche entre les poteaux, « TON NOM », 3 grosses lettres et leurs boutons + / -
            Visuals.Box("Planche du nom", root, new Vector3(0f, NameY, -0.02f), new Vector3(1.6f, 0.62f, 0.06f), DarkWood);
            Visuals.Text(root, "TON NOM", new Vector3(-0.5f, NameY, -0.06f), 0.1f, Gold, title: true);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1f) * LetterGap + 0.3f;
                letters[i] = Visuals.Text(root, "", new Vector3(x, NameY, -0.06f), 0.2f, Color.white, title: true);
                LetterButton.Create(this, i, +1, root, new Vector3(x, NameY + 0.22f, -0.07f));
                LetterButton.Create(this, i, -1, root, new Vector3(x, NameY - 0.22f, -0.07f));
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
                string line = Line(i, e);
                sb.AppendLine(e.game == Leaderboard.GameId ? $"<color=#7CFF7C>{line}</color>" : line);   // ta partie en vert
            }
            here.text = sb.ToString();

            sb.Clear();
            sb.AppendLine("<color=#FFD45A>MONDE</color>");
            if (!Leaderboard.OnlineReady) sb.Append(Leaderboard.OnlineFailed ? "Hors ligne" : "Connexion…");
            var online = Leaderboard.Online;
            for (int i = 0; i < online.Count; i++) sb.AppendLine(Line(i, online[i]));
            world.text = sb.ToString();
        }

        // « 1. DYL   vague 42 », avec le rang en couleur de médaille pour les 3 premiers
        static string Line(int i, Leaderboard.Entry e)
        {
            string rank = i < Medals.Length ? $"<color={Medals[i]}>{i + 1}.</color>" : $"{i + 1}.";
            return $"{rank} {e.name}   vague {e.wave}";
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
                var go = Visuals.Solid(direction > 0 ? "Lettre suivante" : "Lettre précédente", parent, pos, new Vector3(0.22f, 0.11f, 0.05f), Arrow);
                // Le signe est posé à côté (pas dans le cube étiré, il serait déformé) ; + et - : les polices du jeu n'ont pas de flèches
                Visuals.Text(parent, direction > 0 ? "+" : "-", pos + new Vector3(0f, 0f, -0.03f), 0.1f, Color.white, title: true);
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
