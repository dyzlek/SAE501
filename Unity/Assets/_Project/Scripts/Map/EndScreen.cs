using UnityEngine;

namespace SAE
{
    // L'écran de fin, un panneau dans le décor de la carte, à 2 m devant le point d'arrivée (rien collé au visage) :
    //   - VICTOIRE (la dernière vague est gagnée) : deux boutons, REJOUER (nouvelle partie) et CONTINUER (mode infini) ;
    //   - VAGUE PERDUE : une bannière de 5 s qui dit quoi faire (on rejoue la vague, rien n'est perdu).
    // Il se construit tout seul au premier besoin, avec des cubes, comme le reste des panneaux du jeu.
    // Créé par WaveSpawner.Start, qui lui donne la vague à écouter.
    public class EndScreen : MonoBehaviour
    {
        public WaveSpawner spawner;

        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Green = new Color(0.25f, 0.65f, 0.3f);
        static readonly Color Red = new Color(1f, 0.4f, 0.35f);

        const float Distance = 2.2f;      // devant le point d'arrivée, en mètres
        const float Height = 1.5f;        // hauteur du centre du panneau
        const float BannerSeconds = 5f;

        GameObject panel;
        TextMesh title, body;
        GameObject buttons;
        float hideAt = float.MaxValue;

        void Start()
        {
            spawner.GameWon += ShowVictory;
            spawner.WaveFailed += ShowLost;
        }

        void OnDestroy()
        {
            if (!spawner) return;
            spawner.GameWon -= ShowVictory;
            spawner.WaveFailed -= ShowLost;
        }

        void Update()
        {
            if (panel && panel.activeSelf && Time.time > hideAt) Hide();
        }

        // Pour voir le panneau sans jouer 100 vagues : en mode Play, clic droit sur le composant EndScreen
        [ContextMenu("Tester la victoire")] void TestVictory() => ShowVictory();
        [ContextMenu("Tester la vague perdue")] void TestLost() => ShowLost(7);

        void ShowVictory()
        {
            Show("VICTOIRE !", $"Les {spawner.LastWrittenWave} vagues sont vaincues.\nBravo, singe !", Gold, withButtons: true);
        }

        void ShowLost(int wave)
        {
            if (Tutorial.Active) return;   // le tutoriel a son propre tableau pour ça
            Show("VAGUE PERDUE", $"Vague {wave} : rien n'est perdu.\nRelance-la avec LANCER.", Red, withButtons: false);
        }

        void Show(string titleText, string bodyText, Color titleColor, bool withButtons)
        {
            if (!panel) Build();
            Place();
            title.text = titleText;
            title.color = titleColor;
            body.text = bodyText;
            buttons.SetActive(withButtons);
            hideAt = withButtons ? float.MaxValue : Time.time + BannerSeconds;
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);

        // Devant le point d'arrivée de la carte, tourné vers le joueur
        void Place()
        {
            var spawn = LevelSpawn.Of(Level.Carte);
            if (!spawn) return;
            var forward = Vector3.ProjectOnPlane(spawn.forward, Vector3.up).normalized;
            panel.transform.position = spawn.position + forward * Distance + Vector3.up * Height;
            panel.transform.rotation = Quaternion.LookRotation(forward);   // son +Z s'éloigne du joueur, le texte est côté -Z
        }

        // Le panneau : cadre en bois, ardoise, titre, texte et deux boutons
        void Build()
        {
            panel = new GameObject("Panneau de fin");
            panel.transform.SetParent(transform, false);
            var root = panel.transform;
            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.72f, 1.02f, 0.06f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, 0f, -0.035f), new Vector3(1.6f, 0.9f, 0.02f), Slate);
            title = Visuals.Text(root, "", new Vector3(0f, 0.26f, -0.05f), 0.17f, Gold, title: true);
            body = Visuals.Text(root, "", new Vector3(0f, 0.02f, -0.05f), 0.075f, Color.white);

            buttons = new GameObject("Boutons");
            buttons.transform.SetParent(root, false);
            Button(buttons.transform, -0.4f, "REJOUER", Green, EndButton.Action.Restart);
            Button(buttons.transform, 0.4f, "CONTINUER", Gold, EndButton.Action.Continue);
        }

        // Un bouton de la couleur donnée, qu'on touche ou qu'on vise (comme les autres boutons du jeu)
        void Button(Transform parent, float x, string label, Color color, EndButton.Action action)
        {
            var go = new GameObject("Bouton " + label);
            go.tag = Tags.Bouton;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -0.28f, -0.08f);
            var col = go.AddComponent<BoxCollider>();   // avant RayPress : l'interactable récupère le collider
            col.size = new Vector3(0.64f, 0.2f, 0.12f);
            Visuals.Box("Face", go.transform, Vector3.zero, new Vector3(0.6f, 0.16f, 0.05f), color);
            Visuals.Text(go.transform, label, new Vector3(0f, 0f, -0.03f), 0.07f, Color.black, title: true);
            go.AddComponent<EndButton>().Init(this, action);
            go.AddComponent<RayPress>();
        }
    }

    // Un bouton du panneau de fin : REJOUER recommence la partie, CONTINUER cache le panneau (mode infini)
    public class EndButton : MonoBehaviour, IPressable
    {
        public enum Action { Restart, Continue }

        EndScreen screen;
        Action action;

        public void Init(EndScreen owner, Action what) { screen = owner; action = what; }

        public void Press()
        {
            if (action == Action.Restart) GameRestart.Now();
            else screen.Hide();
        }
    }
}
