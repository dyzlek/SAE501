using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // Posé dans la scène Hub : au lancement, il charge aussi les scènes Labyrinthe et Bananeraie (chargement additif), éteintes.
    // La carte est ainsi prête : LANCER depuis le hub démarre la vague tout de suite, et le plateau la montre en direct ;
    // et le bananier produit dès le début, même si on n'est pas encore passé par la porte.
    public class LevelLoader : MonoBehaviour
    {
        void Start()
        {
            foreach (var scene in new[] { Levels.MapScene, Levels.GroveScene })
                if (!SceneManager.GetSceneByName(scene).isLoaded)
                    SceneManager.LoadScene(scene, LoadSceneMode.Additive);
        }
    }
}
