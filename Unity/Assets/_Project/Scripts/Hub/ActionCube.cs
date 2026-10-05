using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // Cube-bouton : charger une scène (Jouer / Retour au hub) ou vider le plateau.
    public class ActionCube : MonoBehaviour, IClickable
    {
        public enum Action { LoadScene, ClearBoard }

        public Action action;
        public string sceneName;
        public string hint = "Appuyer";

        public string Hint => hint;

        public void OnClick(PlayerController player)
        {
            switch (action)
            {
                case Action.LoadScene: SceneManager.LoadScene(sceneName); break;
                case Action.ClearBoard: GameState.ClearBoard(); break;
            }
        }
    }
}
