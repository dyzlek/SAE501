using UnityEngine;

namespace SAE
{
    // Bouton rond d'un pupitre de commande (voir PrototypeGenerator.BuildConsole) qu'on enfonce avec la main :
    // se téléporter (SE TP / HUB), vider le plateau ou lancer la vague. Il s'enfonce un instant quand on appuie.
    // LANCER devient gris tant que la vague tourne : on voit qu'elle est partie, et on n'a pas envie de recliquer.
    public class ActionCube : MonoBehaviour, IPressable
    {
        public enum Action { Teleport, ClearBoard, StartWave }

        public Action action;
        public Level destination;     // pour Teleport : le niveau où aller (son point d'arrivée est retrouvé au moment voulu)

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
            var spawner = WaveSpawner.Instance;
            if (action == Action.StartWave && tint && spawner)
            {
                var color = spawner.Running ? Busy : idleColor;
                if (tint.color != color) tint.Set(color);
            }
        }

        public void Press()
        {
            pressed = 1f;
            switch (action)
            {
                case Action.Teleport:
                    var spot = LevelSpawn.Of(destination);
                    if (spot) PlayerRig.Local.TeleportTo(spot);
                    break;
                case Action.ClearBoard:
                    // Plateau déjà vide : rien à payer. Pas assez de bananes : rien ne se passe.
                    if (GameState.Placed.Count > 0 && Economy.TrySpend(ClearBoardPrice, transform.position))
                        GameState.ClearBoard();
                    break;
                case Action.StartWave: if (WaveSpawner.Instance) WaveSpawner.Instance.StartWave(); break;
            }
        }
    }
}
