using UnityEngine;

namespace SAE
{
    // Le panneau d'annonce de la carte, dans le décor, à 2,2 m devant le point d'arrivée (rien collé au visage).
    // Il n'y a pas de fin dans le jeu (les vagues sont infinies), il annonce seulement quelques moments, 5 s chacun :
    //   - PALIER (toutes les 10 vagues) : « PALIER 20 ! », le bonus de bananes, le record ;
    //   - VAGUE PERDUE : rien n'est perdu, on rejoue la même vague en la relançant avec LANCER ;
    //   - VAGUE SPÉCIALE / BOSS au départ de la vague (#133), et COMBO de vagues parfaites (#132).
    // Il se construit tout seul au premier besoin, avec des cubes, comme les autres panneaux du jeu.
    // Créé par WaveSpawner.Start, qui lui donne la vague à écouter.
    public class EndScreen : MonoBehaviour
    {
        public WaveSpawner spawner;

        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Red = new Color(1f, 0.4f, 0.35f);

        const float Distance = 2.2f;      // devant le point d'arrivée, en mètres
        const float Height = 1.5f;        // hauteur du centre du panneau
        const float BannerSeconds = 5f;

        GameObject panel;
        TextMesh title, body;
        float hideAt = float.MaxValue;

        void Start()
        {
            spawner.MilestoneReached += ShowMilestone;
            spawner.WaveFailed += ShowLost;
            spawner.SpecialWaveStarted += ShowAnnounce;
            spawner.ComboReached += ShowCombo;
        }

        void OnDestroy()
        {
            if (!spawner) return;
            spawner.MilestoneReached -= ShowMilestone;
            spawner.WaveFailed -= ShowLost;
            spawner.SpecialWaveStarted -= ShowAnnounce;
            spawner.ComboReached -= ShowCombo;
        }

        void Update()
        {
            if (panel && panel.activeSelf && Time.time > hideAt) panel.SetActive(false);
        }

        // Pour voir le panneau sans jouer 10 vagues : en mode Play, clic droit sur le composant EndScreen
        [ContextMenu("Tester le palier")] void TestMilestone() => ShowMilestone(20, 100);
        [ContextMenu("Tester la vague perdue")] void TestLost() => ShowLost(7);
        [ContextMenu("Tester REJOUER (nouvelle partie)")] void TestRestart() => GameRestart.Now();
        [ContextMenu("Tester l'annonce de vague spéciale")] void TestAnnounce() => ShowAnnounce(20);
        [ContextMenu("Tester le combo")] void TestCombo() => ShowCombo(3, 120);

        // Le contenu n'est pas dévoilé (#70) : on dit seulement que la vague est différente, et quel temps il fait
        void ShowAnnounce(int wave)
        {
            string title = WaveBook.IsSpecial(wave) ? "VAGUE SPÉCIALE !" : "UN BOSS ARRIVE !";
            string sky = WaveBook.WeatherOf(wave) switch
            {
                Weather.Brouillard => "Le brouillard tombe…",
                Weather.Nuit => "La nuit tombe…",
                Weather.Pluie => "Il se met à pleuvoir…",
                _ => "",
            };
            Show(title, $"Vague {wave}. {sky}\nTiens bon !", Red);
        }

        void ShowCombo(int combo, int bonus)
        {
            Show($"COMBO x{combo} !", $"{combo} vagues parfaites d'affilée : +{bonus} bananes.\nUn seul ballon passé et le combo retombe.", Gold);
        }

        void ShowMilestone(int wave, int bonus)
        {
            Show($"PALIER {wave} !", $"Vague spéciale « {WaveBook.SpecialName(wave)} » vaincue.\nBonus : +{bonus} bananes · Record : {GameState.BestWave}", Gold);
        }

        void ShowLost(int wave)
        {
            if (Tutorial.Active) return;   // le tutoriel a son propre tableau pour ça
            Show("VAGUE PERDUE", $"Vague {wave} : rien n'est perdu.\nRelance-la avec LANCER · Record : {GameState.BestWave}", Red);
        }

        void Show(string titleText, string bodyText, Color titleColor)
        {
            if (!panel) Build();
            Place();
            title.text = titleText;
            title.color = titleColor;
            body.text = bodyText;
            hideAt = Time.time + BannerSeconds;
            panel.SetActive(true);
        }

        // Devant le point d'arrivée de la carte, tourné vers le joueur
        void Place()
        {
            var spawn = LevelSpawn.Of(Level.Carte);
            if (!spawn) return;
            var forward = Vector3.ProjectOnPlane(spawn.forward, Vector3.up).normalized;
            panel.transform.position = spawn.position + forward * Distance + Vector3.up * Height;
            panel.transform.rotation = Quaternion.LookRotation(forward);   // son +Z s'éloigne du joueur, le texte est côté -Z
        }

        // Le panneau : cadre en bois, ardoise, titre et texte
        void Build()
        {
            panel = new GameObject("Panneau d'annonce");
            panel.transform.SetParent(transform, false);
            var root = panel.transform;
            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.72f, 0.72f, 0.06f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, 0f, -0.035f), new Vector3(1.6f, 0.6f, 0.02f), Slate);
            title = Visuals.Text(root, "", new Vector3(0f, 0.15f, -0.05f), 0.17f, Gold, title: true);
            body = Visuals.Text(root, "", new Vector3(0f, -0.1f, -0.05f), 0.07f, Color.white);
        }
    }
}
