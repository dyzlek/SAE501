using UnityEngine;

namespace SAE
{
    // Bouton rond d'un pupitre de commande (voir PrototypeGenerator.BuildConsole) qu'on enfonce avec la main :
    // changer de niveau (SE TP : vers la carte, HUB : vers le hub, voir Levels), vider le plateau ou lancer la vague.
    // Il s'enfonce un instant quand on appuie. Pendant une vague, LANCER devient gris (elle est partie) ;
    // on peut aller et venir entre le hub et la carte, la vague continue.
    // REJOUER reste gris jusqu'à la victoire (vague 10) : on ne recommence pas tout par erreur.
    // ASSIS : pour jouer assis (le joueur est remonté à hauteur d'yeux debout), on rappuie pour revenir debout.
    public class ActionCube : MonoBehaviour, IPressable
    {
        public enum Action { Teleport, ClearBoard, StartWave, Restart, Seated }

        public Action action;
        public Level destination;     // pour Teleport : le niveau (la scène) où aller

        static readonly Color Busy = new Color(0.4f, 0.4f, 0.42f);   // LANCER pendant une vague

        Vector3 rest;
        float pressed;                // 1 = enfoncé, revient à 0
        ColorTint tint;
        Color idleColor;

        public const int ClearBoardPrice = 10;   // en bananes : vider le plateau n'est pas gratuit

        void Start()
        {
            rest = transform.localPosition;
            tint = GetComponent<ColorTint>();
            if (tint) idleColor = tint.color;
        }

        void Update()
        {
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            transform.localPosition = rest + Vector3.down * (0.012f * pressed);
            if (tint)
            {
                var color = Blocked ? Busy : idleColor;
                if (tint.color != color) tint.Set(color);
            }
        }

        // Une vague tourne déjà : on ne la relance pas. Pas encore gagné : on ne recommence pas.
        bool Blocked => action switch
        {
            Action.StartWave => WaveSpawner.Instance && WaveSpawner.Instance.Running,
            Action.Restart => !WaveSpawner.Instance || !WaveSpawner.Instance.Won,
            _ => false,
        };

        public void Press()
        {
            pressed = 1f;
            if (Blocked) return;
            switch (action)
            {
                case Action.Teleport: Levels.Go(destination); break;
                case Action.ClearBoard:
                    // Plateau déjà vide : rien à payer. Pas assez de bananes : rien ne se passe.
                    if (GameState.Placed.Count > 0 && Economy.TrySpend(ClearBoardPrice, transform.position))
                        GameState.ClearBoard();
                    break;
                case Action.StartWave: WaveSpawner.Launch(); break;
                case Action.Restart: Levels.Restart(); break;
                case Action.Seated: if (PlayerRig.Local) PlayerRig.Local.ToggleSeated(); break;   // assis ↔ debout
            }
        }
    }
}
