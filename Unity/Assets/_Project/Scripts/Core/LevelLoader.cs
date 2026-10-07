using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // Posé dans la scène Hub : au lancement, il charge la scène Labyrinthe EN PLUS du hub (chargement additif).
    // Les deux niveaux sont séparés (deux scènes, loin l'un de l'autre), mais tournent en même temps :
    // le plateau du hub montre la carte en direct, et une vague continue quand on revient au hub.
    public class LevelLoader : MonoBehaviour
    {
        public string mapScene = "Labyrinthe";

        void Awake()
        {
            if (!SceneManager.GetSceneByName(mapScene).isLoaded)   // déjà ouverte dans l'éditeur : rien à faire
                SceneManager.LoadScene(mapScene, LoadSceneMode.Additive);
        }
    }
}
