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
        }
    }
}
