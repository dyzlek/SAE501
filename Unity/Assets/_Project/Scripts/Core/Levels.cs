using UnityEngine.SceneManagement;

namespace SAE
{
    public enum Level { Hub, Carte }

    // Les deux niveaux du jeu, chacun dans sa scène : Hub (la cabane) et Labyrinthe (la carte).
    // On passe de l'un à l'autre par un vrai changement de scène (SceneManager.LoadScene, comme dans le cours) :
    // la scène quittée est déchargée. Ce qui doit survivre au voyage (argent, inventaire, singes posés, vagues gagnées,
    // améliorations) est gardé dans des champs static (GameState, HarvesterCrew, UpgradeButton).
    public static class Levels
    {
        public const string HubScene = "Hub";
        public const string MapScene = "Labyrinthe";

        // LANCER appuyé au hub : on part sur la carte, et la vague démarre dès l'arrivée (voir WaveSpawner.Start)
        public static bool LaunchOnArrival;

        public static void Load(Level level) => SceneManager.LoadScene(level == Level.Hub ? HubScene : MapScene);
    }
}
