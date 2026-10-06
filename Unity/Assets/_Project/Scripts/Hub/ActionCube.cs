using UnityEngine;

namespace SAE
{
    // Cube-bouton qu'on enfonce avec la main : se téléporter (Jouer / Retour au hub), vider le plateau ou lancer la vague.
    public class ActionCube : MonoBehaviour, IPressable
    {
        public enum Action { Teleport, ClearBoard, StartWave }

        public Action action;
        public Transform destination;
        public WaveSpawner spawner;   // pour StartWave

        public void Press()
        {
            switch (action)
            {
                case Action.Teleport: PlayerRig.Local.TeleportTo(destination); break;
                case Action.ClearBoard: GameState.ClearBoard(); break;
                case Action.StartWave: spawner.StartWave(); break;
            }
        }
    }
}
