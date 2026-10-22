using UnityEngine;

namespace SAE
{
    // Le panneau COOP (même ardoise que les OPTIONS), dans deux endroits :
    //   - la PAUSE, en pleine partie (inGame) : OUVRIR EN COOP. On choisit un mot de passe à 4 chiffres sur le pavé,
    //     puis le panneau affiche le code de la partie et le mot de passe, à dire aux autres joueurs ;
    //   - le MENU PRINCIPAL : REJOINDRE. On tape le code de la partie, puis son mot de passe ; une fois connecté,
    //     on part tout seul dans la cabane de l'hôte.
    // Le pavé : 1 2 3 4 5 6 / 7 8 9 0 EFFACER OK, visé au rayon comme tous les boutons.
    // Déjà en coop : le panneau dit où on en est, avec QUITTER LE COOP (ou FERMER LE COOP pour l'hôte).
    public class CoopPanel : MonoBehaviour
    {
        const int Digits = 4;   // code et mot de passe : 4 chiffres (facile à taper au rayon, assez pour une salle de TP)

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
        int code;                 // menu : le code tapé, en attendant le mot de passe
        bool wasConnected;

        TextMesh title, big, status, leaveLabel;
        GameObject keypad, leave;

        public static CoopPanel Build(Transform parent, Vector3 localPos, bool inGame, System.Action onBack)
        {
            var root = new GameObject("Coop").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            var panel = root.gameObject.AddComponent<CoopPanel>();
            panel.inGame = inGame;
            panel.onBack = onBack;

            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.5f, 1.05f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, 0f, -0.03f), new Vector3(1.4f, 0.95f, 0.02f), Slate);
            panel.title = Visuals.Text(root, "", new Vector3(0f, 0.38f, -0.05f), 0.08f, Gold, title: true);
            panel.big = Visuals.Text(root, "", new Vector3(0f, 0.24f, -0.05f), 0.09f, Color.white, title: true);
            panel.status = Visuals.Text(root, "", new Vector3(0f, 0.12f, -0.05f), 0.045f, Color.white);

            panel.keypad = new GameObject("Pavé");
            panel.keypad.transform.SetParent(root, false);
            var k = panel.keypad.transform;
            var keySize = new Vector2(0.17f, 0.15f);
            for (int i = 0; i < 10; i++)
            {
                char digit = (char)('0' + (i + 1) % 10);   // 1 à 9, puis 0
                int row = i / 6, col = i % 6;
                WorldButton.Create(k, digit.ToString(), new Vector3(-0.5f + col * 0.2f, -0.02f - row * 0.18f, -0.06f), keySize, Key, () => panel.Type(digit));
            }
            WorldButton.Create(k, "EFFACER", new Vector3(0.3f, -0.2f, -0.06f), new Vector2(0.17f, 0.15f), Red, panel.Erase, 0.03f);
            WorldButton.Create(k, "OK", new Vector3(0.5f, -0.2f, -0.06f), keySize, Green, panel.Validate);

            panel.leave = new GameObject("Quitter");
            panel.leave.transform.SetParent(root, false);
            panel.leaveLabel = WorldButton.Create(panel.leave.transform, "QUITTER LE COOP", new Vector3(0f, -0.1f, -0.06f), new Vector2(0.7f, 0.13f), Red, Coop.Leave);

            WorldButton.Create(root, "RETOUR", new Vector3(0f, -0.4f, -0.06f), new Vector2(0.45f, 0.1f), Key, panel.Back);
            root.gameObject.SetActive(false);
            return panel;
        }

        // À chaque ouverture : on repart du début (pause : le mot de passe ; menu : le code)
        void OnEnable()
        {
            if (!title) return;   // pendant Build
            wasConnected = Coop.Connected;
            GoTo(Coop.Role == CoopRole.Solo ? (inGame ? Step.Password : Step.Code) : Step.Info);
        }

        void GoTo(Step s)
        {
            step = s;
            typed = "";
            keypad.SetActive(s != Step.Info);
            leave.SetActive(s == Step.Info);
            leaveLabel.text = Coop.IsHost ? "FERMER LE COOP" : "QUITTER LE COOP";
            title.text = s switch
            {
                Step.Info => "COOP · MÊME WI-FI",
                _ => inGame ? "OUVRIR EN COOP" : "REJOINDRE UNE PARTIE",
            };
            Refresh();
        }

        void Type(char digit)
        {
            if (typed.Length >= Digits) return;
            typed += digit;
            Refresh();
        }

        void Erase()
        {
            if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);
            Refresh();
        }

        void Validate()
        {
            if (typed.Length < Digits) { Sounds.Play(Sounds.AchatRefuse, transform.position, 0.6f); return; }
            if (step == Step.Code)
            {
                code = int.Parse(typed);
                GoTo(Step.Password);
            }
            else if (inGame)
            {
                Coop.Host(typed);
                GoTo(Step.Info);
            }
            else
            {
                Coop.Join(code, typed);
                GoTo(Step.Info);
            }
        }

        // RETOUR : du mot de passe au code (menu), sinon on ferme le panneau
        void Back()
        {
            if (!inGame && step == Step.Password) GoTo(Step.Code);
            else onBack?.Invoke();
        }

        void Update()
        {
            // Le coop a changé tout seul (refusé, partie introuvable, hôte parti) : on revient au pavé
            if (step == Step.Info && Coop.Role == CoopRole.Solo) GoTo(inGame ? Step.Password : Step.Code);
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
                // « 4 8 _ _ » : ce qu'on a tapé, et ce qu'il reste
                big.text = string.Join(" ", (typed + new string('_', Digits - typed.Length)).ToCharArray());
                status.text = Coop.Status.Length > 0 ? Coop.Status
                    : step == Step.Code ? "Tape le code de la partie (il est dans la pause de l'hôte)"
                    : inGame ? "Choisis un mot de passe à 4 chiffres" : "Tape le mot de passe de la partie";
                return;
            }
            big.text = Coop.IsHost ? $"CODE {Coop.Code} · MDP {Coop.Password}" : $"PARTIE {Coop.Code}";
            status.text = Coop.Status;
        }
    }
}
