using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // Posé dans la scène Hub : au lancement, il charge aussi la scène Labyrinthe (chargement additif), éteinte.
    // La carte est ainsi prête : LANCER depuis le hub démarre la vague tout de suite, et le plateau la montre en direct.
    public class LevelLoader : MonoBehaviour
    {
        void Start()
        {
            if (!SceneManager.GetSceneByName(Levels.MapScene).isLoaded)
                SceneManager.LoadScene(Levels.MapScene, LoadSceneMode.Additive);
            PauseMenu.Ensure();                  // Échap / bouton Menu de la manette, partout
            GameMenu.OpenAtLaunch();             // le jeu commence dans le menu principal (sauf après NOUVELLE PARTIE)
        }
    }
}
