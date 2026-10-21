using System;
using System.Text;
using UnityEngine;

namespace SAE
{
    // Le grand panneau du classement, DEHORS, dans la bananeraie : dans le pré après la barrière de droite, face à la terrasse
    // (posé par PrototypeGenerator.BuildGrove). Un panneau d'affichage en bois sur deux poteaux, avec un petit toit :
    //   l'ardoise : CLASSEMENT en grand, deux colonnes ICI (les 10 meilleures parties de ce casque) et MONDE (le serveur),
    //   médailles or / argent / bronze sur les 3 premiers, la partie en cours en vert ;
    //   en dessous, la planche du nom : « TON NOM : SINGE-4821 », son état (RÉSERVÉ, PRIS, hors ligne) et un bouton MODIFIER
    //   qui ouvre un clavier (lettres, chiffres, « - », EFFACER, OK) à viser au rayon.
    // Un nom n'est gardé que si le serveur le réserve pour ce casque (Leaderboard.TryRename) : personne ne peut prendre le tien.
    // Le texte n'est réécrit que quand quelque chose change (Leaderboard.Changed, une touche). Que des cubes : rien de coûteux.
    // +Z local = dos au joueur, le texte se lit côté -Z.
    public class LeaderboardBoard : MonoBehaviour
    {
        const float BoardY = 2.6f, BoardW = 3.2f, BoardH = 1.9f;   // l'ardoise, en mètres : lisible depuis la terrasse
        const float PlankY = 1.0f, PlankH = 1.05f;                 // la planche du nom et du clavier, sous l'ardoise
        const float PostX = BoardW / 2f + 0.15f;
        const float KeyStep = 0.17f, KeySize = 0.15f;              // les touches du clavier, en mètres

        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color DarkWood = new Color(0.22f, 0.14f, 0.07f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Thatch = new Color(0.72f, 0.58f, 0.3f);
        static readonly Color KeyColor = new Color(0.45f, 0.32f, 0.18f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color Red = new Color(0.85f, 0.3f, 0.25f);
        static readonly string[] Medals = { "#FFD45A", "#D9DEE8", "#D08A4A" };   // or, argent, bronze
        static readonly string[] Rows = { "ABCDEFGHIJ", "KLMNOPQRST", "UVWXYZ0123", "456789-" };

        TextMesh here, world, nameText, stateText, editLabel;
        GameObject keyboard;
        string draft;                 // le nom en train d'être tapé (null = clavier fermé)
        string message;               // un message du clavier (« 3 à 12 caractères »), à la place de l'état

        void OnEnable() => Leaderboard.Changed += Refresh;
        void OnDisable() => Leaderboard.Changed -= Refresh;

        void Start()
        {
            if (Leaderboard.GameId == null) Leaderboard.NewGame();
            Build();
            Refresh();
            Leaderboard.Refresh();        // demande le classement MONDE au serveur
            Leaderboard.ClaimCurrent();   // et réserve le nom actuel (déjà à nous en général)
        }

        void Build()
        {
            var root = transform;
            float top = BoardY + BoardH / 2f;

            // La charpente : deux poteaux plantés dans l'herbe, une traverse, un petit toit de chaume (comme la cabane)
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Poteau", root, new Vector3(side * PostX, (top + 0.4f) / 2f, 0.05f), new Vector3(0.18f, top + 0.4f, 0.18f), DarkWood);
            Visuals.Box("Toit", root, new Vector3(0f, top + 0.45f, -0.1f), new Vector3(BoardW + 0.9f, 0.12f, 0.75f), Thatch).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Visuals.Box("Traverse", root, new Vector3(0f, PlankY - PlankH / 2f - 0.05f, 0.05f), new Vector3(2f * PostX, 0.1f, 0.1f), DarkWood);

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

            // La planche du nom : le nom en gros, son état dessous, le bouton MODIFIER à gauche
            float nameY = PlankY + PlankH / 2f - 0.16f;
            Visuals.Box("Planche du nom", root, new Vector3(0f, PlankY, -0.02f), new Vector3(2f * PostX - 0.2f, PlankH, 0.06f), DarkWood);
            nameText = Visuals.Text(root, "", new Vector3(0.15f, nameY, -0.06f), 0.13f, Color.white, title: true);
            stateText = Visuals.Text(root, "", new Vector3(0.15f, nameY - 0.15f, -0.06f), 0.06f, Color.white);
            editLabel = Key(root, "MODIFIER", new Vector3(-1.25f, nameY - 0.05f, -0.07f), 0.5f, Green, ToggleKeyboard);

            // Le clavier, caché tant qu'on n'appuie pas sur MODIFIER : 4 rangées sous le nom, puis EFFACER et OK
            keyboard = new GameObject("Clavier");
            keyboard.transform.SetParent(root, false);
            var k = keyboard.transform;
            for (int r = 0; r < Rows.Length; r++)
            {
                float y = nameY - 0.33f - r * KeyStep;
                string row = Rows[r];
                float x0 = -(Rows[0].Length - 1) * KeyStep / 2f;   // toutes les rangées partent du même bord gauche
                for (int i = 0; i < row.Length; i++)
                {
                    char c = row[i];
                    Key(k, c.ToString(), new Vector3(x0 + i * KeyStep, y, -0.07f), KeySize, KeyColor, () => Type(c));
                }
                if (r == Rows.Length - 1)
                {
                    float x = x0 + row.Length * KeyStep + 0.12f;
                    Key(k, "EFFACER", new Vector3(x + 0.1f, y, -0.07f), 0.38f, Red, Erase);
                    Key(k, "OK", new Vector3(x + 0.52f, y, -0.07f), 0.3f, Green, Validate);
                }
            }
            keyboard.SetActive(false);
        }

        // ---------------- Le clavier ----------------
        void ToggleKeyboard()
        {
            draft = draft == null ? "" : null;   // ouvrir : on part d'un nom vide ; fermer : on annule
            message = null;
            keyboard.SetActive(draft != null);
            Refresh();
        }

        void Type(char c)
        {
            if (draft == null || draft.Length >= Leaderboard.MaxName) return;
            draft += c;
            message = null;
            Refresh();
        }

        void Erase()
        {
            if (string.IsNullOrEmpty(draft)) return;
            draft = draft.Substring(0, draft.Length - 1);
            Refresh();
        }

        void Validate()
        {
            if (draft == null) return;
            if (!Leaderboard.IsValidName(draft))
            {
                message = $"<color=#FF7766>{Leaderboard.MinName} à {Leaderboard.MaxName} caractères</color>";
                Refresh();
                return;
            }
            // Le serveur décide : libre -> le nom est gardé et le clavier se ferme ; pris -> on reste sur le clavier
            Leaderboard.TryRename(draft, kept =>
            {
                if (!kept) return;
                draft = null;
                keyboard.SetActive(false);
            });
        }

        // ---------------- L'affichage ----------------
        void Refresh()
        {
            if (!here) return;

            bool editing = draft != null;
            nameText.text = editing ? $"TON NOM : {draft}_" : $"TON NOM : {Leaderboard.Name}";
            stateText.text = message ?? StateText(editing);
            editLabel.text = editing ? "ANNULER" : "MODIFIER";

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

        static string StateText(bool editing) => Leaderboard.State switch
        {
            Leaderboard.NameState.Checking => "Vérification…",
            Leaderboard.NameState.Mine => editing ? "Tape ton nom puis OK" : "<color=#7CFF7C>RÉSERVÉ pour ce casque</color>",
            Leaderboard.NameState.Taken => "<color=#FF7766>PRIS par un autre joueur : choisis-en un autre</color>",
            Leaderboard.NameState.Offline => "<color=#AAAAAA>Hors ligne : nom pas encore vérifié</color>",
            _ => editing ? "Tape ton nom puis OK" : "",
        };

        // « 1. DYLAN   vague 42 », avec le rang en couleur de médaille pour les 3 premiers
        static string Line(int i, Leaderboard.Entry e)
        {
            string rank = i < Medals.Length ? $"<color={Medals[i]}>{i + 1}.</color>" : $"{i + 1}.";
            return $"{rank} {e.name}   vague {e.wave}";
        }

        [ContextMenu("Vider le classement local")] void TestClear() => Leaderboard.ClearLocal();
        [ContextMenu("Recharger le classement MONDE")] void TestFetch() => Leaderboard.Refresh();

        // Une touche : un cube qu'on vise au rayon (RayPress) ou qu'on touche (HandPress), son texte posé devant
        // (pas dans le cube étiré : il serait déformé). Renvoie le texte, pour pouvoir le changer (MODIFIER / ANNULER).
        static TextMesh Key(Transform parent, string label, Vector3 pos, float width, Color color, Action action)
        {
            var go = Visuals.Solid("Touche " + label, parent, pos, new Vector3(width, KeySize, 0.04f), color);
            go.AddComponent<KeyButton>().action = action;
            go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();   // après le collider (Solid)
            go.AddComponent<RayPress>();
            return Visuals.Text(parent, label, pos + new Vector3(0f, 0f, -0.03f), label.Length > 1 ? 0.055f : 0.08f, Color.white, title: true);
        }

        class KeyButton : MonoBehaviour, IPressable
        {
            public Action action;
            public void Press() => action?.Invoke();
        }
    }
}
