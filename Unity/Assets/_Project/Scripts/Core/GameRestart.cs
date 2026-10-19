using UnityEngine.SceneManagement;

namespace SAE
{
    // REJOUER (écran de victoire) : on remet à zéro tout ce qui est gardé d'une scène à l'autre (les statiques :
    // argent, inventaire, vagues gagnées, améliorations de l'arc, tutoriel, niveau), puis on recharge la scène Hub seule.
    // Les deux scènes se reconstruisent comme au premier lancement (la carte est rechargée par LevelLoader).
    // Le bananier, les récolteurs et les vagues vivent dans les scènes : ils repartent de zéro avec elles.
    public static class GameRestart
    {
        public static void Now()
        {
            GameState.Reset();
            BowUpgrades.Reset();
            Tutorial.Restart();
            Levels.Restart();
            SceneManager.LoadScene(Levels.HubScene, LoadSceneMode.Single);   // Single : la carte est déchargée avec
        }
    }
}
