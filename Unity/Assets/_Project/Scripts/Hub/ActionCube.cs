using UnityEngine;

namespace SAE
{
    // Cube-bouton : se téléporter (Jouer / Retour au hub), vider le plateau ou lancer la vague.
    public class ActionCube : MonoBehaviour, IClickable
    {
        public enum Action { Teleport, ClearBoard, StartWave }

        public Action action;
        public Transform destination;
        public WaveSpawner spawner;   // pour StartWave
        public string hint = "Appuyer";

        public string GetHint(Vector3 point) => hint;

        public void OnClick(PlayerController player, Vector3 point)
        {
            switch (action)
            {
                case Action.Teleport: player.TeleportTo(destination); break;
                case Action.ClearBoard: GameState.ClearBoard(); break;
                case Action.StartWave: spawner.StartWave(); break;
            }
        }
    }
}
