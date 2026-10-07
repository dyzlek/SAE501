using UnityEngine;

namespace SAE
{
    // Bouton rond d'un pupitre de commande (voir PrototypeGenerator.BuildConsole) qu'on enfonce avec la main :
    // se téléporter (Jouer / Retour au hub), vider le plateau ou lancer la vague. Il s'enfonce un instant quand on appuie.
    public class ActionCube : MonoBehaviour, IPressable
    {
        public enum Action { Teleport, ClearBoard, StartWave }

        public Action action;
        public Transform destination;
        public WaveSpawner spawner;   // pour StartWave

        Vector3 rest;
        float pressed;                // 1 = enfoncé, revient à 0

        public const int ClearBoardPrice = 10;   // en bananes : vider le plateau n'est pas gratuit

        void Start() => rest = transform.localPosition;

        void Update()
        {
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            transform.localPosition = rest + Vector3.down * (0.02f * pressed);
        }

        public void Press()
        {
            pressed = 1f;
            switch (action)
            {
                case Action.Teleport: PlayerRig.Local.TeleportTo(destination); break;
                case Action.ClearBoard:
                    // Plateau déjà vide : rien à payer. Pas assez de bananes : rien ne se passe.
                    if (GameState.Placed.Count > 0 && Economy.TrySpend(ClearBoardPrice, transform.position))
                        GameState.ClearBoard();
                    break;
                case Action.StartWave: spawner.StartWave(); break;
            }
        }
    }
}
