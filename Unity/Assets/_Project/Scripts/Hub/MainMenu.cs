using UnityEngine;

namespace SAE
{
    // Le menu principal (#158), dans sa propre scène (Menu), loin du hub et de la carte. Le joueur y arrive au lancement
    // (GameMenu.OpenAtLaunch) et depuis la pause (MENU PRINCIPAL). Le jeu est arrêté pendant le menu : tout ce qui bouge
    // ici utilise le temps réel (Time.unscaledTime).
    //   Le titre BLOONS VR en grand, sur une enseigne en bois ;
    //   une estrade où des singes dansent (sautillent, se tournent), avec leur aura de rareté ;
    //   des ballons qui montent dans le ciel : on peut les éclater au rayon, pour s'amuser en attendant ;
    //   les boutons, à 2 m, visés au rayon : CONTINUER (ou JOUER s'il n'y a pas de sauvegarde), NOUVELLE PARTIE
    //   (seulement s'il y a une sauvegarde, à confirmer), OPTIONS.
    // Posé par PrototypeGenerator dans la scène Menu ; tout se construit ici au lancement (des cubes et les modèles des singes).
    // +Z local = devant le joueur (le point d'arrivée regarde vers +Z) ; les textes se lisent côté -Z.
    public class MainMenu : MonoBehaviour
    {
        const float ButtonsZ = 2.2f;      // les boutons, à 2,2 m devant le joueur
        const float StageZ = 5.5f;        // l'estrade des singes
        const int BalloonCount = 14;

        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color DarkWood = new Color(0.22f, 0.14f, 0.07f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color Red = new Color(0.85f, 0.3f, 0.25f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 0.85f);
        static readonly Color[] BalloonColors =
        {
            new Color(0.9f, 0.15f, 0.15f), new Color(0.2f, 0.45f, 0.95f), new Color(0.25f, 0.8f, 0.3f),
            new Color(1f, 0.85f, 0.2f), new Color(1f, 0.45f, 0.75f),
        };

        GameObject buttons;
        OptionsPanel options;
        TextMesh newGameLabel, saveInfo;
        float confirmUntil;

        void Start()
        {
            BuildTitle();
            BuildStage();
            BuildQuincy();
            BuildBalloonBunches();
            for (int i = 0; i < BalloonCount; i++) MenuBalloon.Create(transform, BalloonColors[i % BalloonColors.Length], i);
            BuildButtons();
            options = OptionsPanel.Build(transform, new Vector3(0f, 1.35f, ButtonsZ), CloseOptions);
        }

        void BuildTitle()
        {
            var t = transform;
            Visuals.Box("Enseigne", t, new Vector3(0f, 3.6f, StageZ + 1.5f), new Vector3(4.6f, 1.3f, 0.12f), DarkWood);
            Visuals.Box("Liseré", t, new Vector3(0f, 3.6f, StageZ + 1.43f), new Vector3(4.4f, 1.1f, 0.02f), Gold);
            Visuals.Box("Planche", t, new Vector3(0f, 3.6f, StageZ + 1.42f), new Vector3(4.3f, 1.0f, 0.02f), Wood);
            Visuals.Text(t, "BLOONS VR", new Vector3(0f, 3.72f, StageZ + 1.38f), 0.7f, Gold, title: true);
            Visuals.Text(t, "Défends la jungle avec Quincy et ses singes", new Vector3(0f, 3.28f, StageZ + 1.38f), 0.14f, Color.white);
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Poteau", t, new Vector3(side * 2.1f, 1.6f, StageZ + 1.55f), new Vector3(0.2f, 3.2f, 0.2f), DarkWood);
        }

        // L'estrade et les singes qui dansent dessus (un de chaque type, de raretés différentes)
        void BuildStage()
        {
            var t = transform;
            Visuals.Solid("Estrade", t, new Vector3(0f, 0.15f, StageZ), new Vector3(7f, 0.3f, 2.2f), Wood);
            int types = MonkeyData.TypeCount;
            for (int i = 0; i < types; i++)
            {
                float x = (i - (types - 1) / 2f) * 0.95f;
                var monkey = new Monkey((MonkeyType)i, (Rarity)(i % MonkeyData.LevelCount));
                var piece = Visuals.MonkeyPiece(monkey, t, new Vector3(x, 0.75f, StageZ - 0.2f + (i % 2) * 0.5f), 0.8f, withLabel: false);
                var dancer = piece.AddComponent<MenuMonkey>();
                dancer.phase = i * 0.7f;
                dancer.wave = i % 2 == 0;   // un sur deux fait coucou, les autres dansent les bras le long du corps
            }
        }

        // Quincy (posé par le générateur, à gauche des boutons) : bras baissés, puis il sautille un peu et fait coucou
        void BuildQuincy()
        {
            var quincy = transform.Find("Quincy");
            if (!quincy || quincy.childCount == 0) return;
            Visuals.RestPose(quincy.GetChild(0), quincy);
            var anim = quincy.gameObject.AddComponent<MenuMonkey>();
            anim.hopHeight = 0.05f;   // il est grand : il sautille à peine
            anim.phase = 1.3f;
        }

        // Deux grappes de ballons de toutes les couleurs, accrochées aux poteaux de l'enseigne (fixes : rien à calculer)
        void BuildBalloonBunches()
        {
            var random = new System.Random(158);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 9; i++)
                {
                    var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(b.GetComponent<Collider>());
                    b.name = "Grappe";
                    b.transform.SetParent(transform, false);
                    var offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() * 0.9f, (float)random.NextDouble() * 0.4f);
                    b.transform.localPosition = new Vector3(side * 2.45f, 3.9f, StageZ + 1.4f) + offset * 0.8f;
                    b.transform.localScale = new Vector3(0.38f, 0.46f, 0.38f);
                    b.AddComponent<ColorTint>().Set(BalloonColors[i % BalloonColors.Length]);
                }
        }

        void BuildButtons()
        {
            buttons = new GameObject("Boutons");
            buttons.transform.SetParent(transform, false);
            var b = buttons.transform;
            bool hasSave = SaveGame.HasSave;
            var size = new Vector2(1.0f, 0.24f);

            WorldButton.Create(b, hasSave ? "CONTINUER" : "JOUER", new Vector3(0f, 1.6f, ButtonsZ), size, Green, GameMenu.Continue);
            if (hasSave)
            {
                // Où on en est, sous CONTINUER
                saveInfo = Visuals.Text(b, $"Vague {GameState.WavesWon + 1} · {GameState.Money} bananes",
                    new Vector3(0f, 1.42f, ButtonsZ - 0.03f), 0.06f, Gold, title: true);
                newGameLabel = WorldButton.Create(b, "NOUVELLE PARTIE", new Vector3(0f, 1.18f, ButtonsZ), size, Red, NewGame);
            }
            WorldButton.Create(b, "OPTIONS", new Vector3(0f, hasSave ? 0.88f : 1.28f, ButtonsZ), size, Blue, OpenOptions);
        }

        // NOUVELLE PARTIE : il faut appuyer une 2e fois en moins de 4 s (la sauvegarde sera effacée)
        void NewGame()
        {
            if (Time.unscaledTime < confirmUntil) { GameMenu.NewGame(); return; }
            confirmUntil = Time.unscaledTime + 4f;
            newGameLabel.text = "SÛR ? APPUIE ENCORE";
        }

        void OpenOptions() { buttons.SetActive(false); options.gameObject.SetActive(true); }
        void CloseOptions() { options.gameObject.SetActive(false); buttons.SetActive(true); }

        void Update()
        {
            if (confirmUntil > 0f && Time.unscaledTime >= confirmUntil)
            {
                confirmUntil = 0f;
                newGameLabel.text = "NOUVELLE PARTIE";
            }
        }
    }
}
