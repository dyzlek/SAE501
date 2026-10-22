using UnityEngine;

namespace SAE
{
    // Le panneau COOP (même ardoise que les OPTIONS), dans deux endroits :
    //   - la PAUSE, en pleine partie (inGame) : OUVRIR EN COOP. On choisit un mot de passe de 4 caractères au clavier,
    //     puis le panneau affiche le code de la partie (6 caractères, donné par le relais) et le mot de passe,
    //     à dire aux autres joueurs ;
    //   - le MENU PRINCIPAL : REJOINDRE. On tape le code de la partie, puis son mot de passe ; une fois connecté,
    //     on part tout seul dans la partie de l'hôte.
    // Le clavier : chiffres puis lettres en AZERTY, EFFACER et OK, visés au rayon comme tous les boutons.
    // Déjà en coop : le panneau dit où on en est, avec QUITTER LE COOP (ou FERMER LE COOP pour l'hôte).
    public class CoopPanel : MonoBehaviour
    {
        const int CodeLength = 6;       // les codes de Unity Relay font 6 caractères
        const int PasswordLength = 4;
        static readonly string[] rows = { "1234567890", "AZERTYUIOP", "QSDFGHJKLM", "WXCVBN" };
        const float KeyStep = 0.125f;   // en mètres, d'une touche à la suivante

        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color Red = new Color(0.85f, 0.3f, 0.25f);
        static readonly Color Key = new Color(0.45f, 0.32f, 0.18f);

        enum Step { Code, Password, Info }   // taper le code, taper le mot de passe, ou déjà en coop

        bool inGame;
        System.Action onBack;
        Step step;
        string typed = "";
        string code = "";         // menu : le code tapé, en attendant le mot de passe
        bool wasConnected;

        TextMesh title, big, status, leaveLabel;
        GameObject keyboard, leave;

        int Length => step == Step.Code ? CodeLength : PasswordLength;

        public static CoopPanel Build(Transform parent, Vector3 localPos, bool inGame, System.Action onBack)
        {
            var root = new GameObject("Coop").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            var panel = root.gameObject.AddComponent<CoopPanel>();
            panel.inGame = inGame;
            panel.onBack = onBack;

            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.5f, 1.2f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, 0f, -0.03f), new Vector3(1.4f, 1.1f, 0.02f), Slate);
            panel.title = Visuals.Text(root, "", new Vector3(0f, 0.47f, -0.05f), 0.08f, Gold, title: true);
            panel.big = Visuals.Text(root, "", new Vector3(0f, 0.33f, -0.05f), 0.1f, Color.white, title: true);
            panel.status = Visuals.Text(root, "", new Vector3(0f, 0.2f, -0.05f), 0.045f, Color.white);

            panel.keyboard = new GameObject("Clavier");
            panel.keyboard.transform.SetParent(root, false);
            var k = panel.keyboard.transform;
            var keySize = new Vector2(0.11f, 0.11f);
            for (int row = 0; row < rows.Length; row++)
            {
                float y = 0.06f - row * 0.13f;
                for (int i = 0; i < rows[row].Length; i++)
                {
                    char c = rows[row][i];
                    WorldButton.Create(k, c.ToString(), new Vector3((i - 4.5f) * KeyStep, y, -0.06f), keySize, Key, () => panel.Type(c));
                }
            }
            float lastY = 0.06f - 3 * 0.13f;   // la dernière rangée (WXCVBN) a de la place à droite
            WorldButton.Create(k, "EFFACER", new Vector3(2.2f * KeyStep, lastY, -0.06f), new Vector2(0.28f, 0.11f), Red, panel.Erase, 0.045f);
            WorldButton.Create(k, "OK", new Vector3(4.25f * KeyStep, lastY, -0.06f), new Vector2(0.18f, 0.11f), Green, panel.Validate);

            panel.leave = new GameObject("Quitter");
            panel.leave.transform.SetParent(root, false);
            panel.leaveLabel = WorldButton.Create(panel.leave.transform, "QUITTER LE COOP", new Vector3(0f, -0.1f, -0.06f), new Vector2(0.7f, 0.13f), Red, Coop.Leave);

            WorldButton.Create(root, "RETOUR", new Vector3(0f, -0.48f, -0.06f), new Vector2(0.45f, 0.09f), Key, panel.Back);
            root.gameObject.SetActive(false);
            return panel;
        }

        // À chaque ouverture : on repart du début (pause : le mot de passe ; menu : le code)
        void OnEnable()
        {
            if (!title) return;   // pendant Build
            wasConnected = Coop.Connected;
            GoTo(Coop.Role == CoopRole.Solo && !Coop.Busy ? FirstStep : Step.Info);
        }

        Step FirstStep => inGame ? Step.Password : Step.Code;

        void GoTo(Step s)
        {
            step = s;
            typed = "";
            keyboard.SetActive(s != Step.Info);
            leave.SetActive(s == Step.Info && Coop.Role != CoopRole.Solo);
            leaveLabel.text = Coop.IsHost ? "FERMER LE COOP" : "QUITTER LE COOP";
            title.text = s == Step.Info ? "COOP PAR INTERNET" : inGame ? "OUVRIR EN COOP" : "REJOINDRE UNE PARTIE";
            Refresh();
        }

        void Type(char c)
        {
            if (typed.Length >= Length) return;
            typed += c;
            Refresh();
        }

        void Erase()
        {
            if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);
            Refresh();
        }

        void Validate()
        {
            if (typed.Length < Length) { Sounds.Play(Sounds.AchatRefuse, transform.position, 0.6f); return; }
            if (step == Step.Code)
            {
                code = typed;
                GoTo(Step.Password);
                return;
            }
            if (inGame) Coop.Host(typed);
            else Coop.Join(code, typed);
            GoTo(Step.Info);
        }

        // RETOUR : du mot de passe au code (menu), sinon on ferme le panneau
        void Back()
        {
            if (!inGame && step == Step.Password) GoTo(Step.Code);
            else onBack?.Invoke();
        }

        void Update()
        {
            // Le coop n'a pas démarré, ou s'est arrêté tout seul (refusé, code inconnu, hôte parti) : on revient au clavier
            if (step == Step.Info && Coop.Role == CoopRole.Solo && !Coop.Busy) { GoTo(FirstStep); return; }
            // Le serveur a répondu : on montre le bouton pour quitter
            if (step == Step.Info && !leave.activeSelf && Coop.Role != CoopRole.Solo) GoTo(Step.Info);
            // Menu : connecté ! On part dans la partie de l'hôte, comme avec CONTINUER
            if (!inGame && Coop.Connected && !wasConnected)
            {
                wasConnected = true;
                onBack?.Invoke();
                GameMenu.Continue();
                return;
            }
            if (step == Step.Info) Refresh();   // le nombre de joueurs, la connexion : ça bouge tout seul
        }

        void Refresh()
        {
            if (step != Step.Info)
            {
                // « K 7 _ _ _ _ » : ce qu'on a tapé, et ce qu'il reste
                big.text = string.Join(" ", (typed + new string('_', Length - typed.Length)).ToCharArray());
                status.text = Coop.Status.Length > 0 ? Coop.Status
                    : step == Step.Code ? "Tape le code de la partie (il est dans la pause de l'hôte)"
                    : inGame ? "Choisis un mot de passe de 4 caractères" : "Tape le mot de passe de la partie";
                return;
            }
            big.text = Coop.IsHost ? $"CODE {Coop.Code} · MDP {Coop.Password}" : Coop.IsGuest ? $"PARTIE {Coop.Code}" : "";
            status.text = Coop.Status;
        }
    }
}
