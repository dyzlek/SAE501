using UnityEngine;

namespace SAE
{
    // Bouton rond d'une borne (voir PrototypeGenerator.MakeActionCube) qu'on enfonce avec la main : se téléporter (Jouer / Retour au hub), vider le plateau ou lancer la vague.
    public class ActionCube : MonoBehaviour, IPressable
    {
        public enum Action { Teleport, ClearBoard, StartWave }

        public Action action;
        public Transform destination;
        public WaveSpawner spawner;   // pour StartWave

        public const int ClearBoardPrice = 10;   // en bananes : vider le plateau n'est pas gratuit

        public void Press()
        {
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
