using UnityEngine;

namespace SAE
{
    // Bouton rond d'un pupitre de commande (voir PrototypeGenerator.BuildConsole) qu'on enfonce avec la main :
    // changer de niveau (SE TP : vers la carte, HUB : vers le hub ; c'est un changement de scène), vider le plateau
    // ou lancer la vague. Il s'enfonce un instant quand on appuie.
    // Pendant une vague, LANCER et HUB deviennent gris : la vague est partie, et on ne quitte pas la carte en pleine vague
    // (la scène serait déchargée et la vague perdue).
    public class ActionCube : MonoBehaviour, IPressable
    {
        public enum Action { Teleport, ClearBoard, StartWave }

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

        // Une vague tourne sur la carte : on ne relance pas, et on ne part pas au hub
        bool Blocked => WaveSpawner.Instance && WaveSpawner.Instance.Running
                        && (action == Action.StartWave || (action == Action.Teleport && destination == Level.Hub));

        public void Press()
        {
            pressed = 1f;
            if (Blocked) return;
            switch (action)
            {
                case Action.Teleport: Levels.Load(destination); break;
                case Action.ClearBoard:
                    // Plateau déjà vide : rien à payer. Pas assez de bananes : rien ne se passe.
                    if (GameState.Placed.Count > 0 && Economy.TrySpend(ClearBoardPrice, transform.position))
                        GameState.ClearBoard();
                    break;
                case Action.StartWave: WaveSpawner.Launch(); break;
            }
        }
    }
}
