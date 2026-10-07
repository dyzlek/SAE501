using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    public enum Level { Hub, Carte }

    // Les deux niveaux du jeu, chacun dans sa scène : Hub (la cabane) et Labyrinthe (la carte), loin l'un de l'autre.
    // Les deux scènes restent chargées (la seconde est chargée EN PLUS de la première, chargement additif) : le monde
    // que l'on quitte continue de tourner. Une vague lancée continue pendant qu'on va chercher des bananes au hub,
    // et les bananes tombent pendant qu'on défend la carte.
    // Changer de niveau, c'est changer de « présence » : le joueur, le soleil et les réglages d'image de la scène
    // d'arrivée s'allument, ceux de la scène quittée s'éteignent (voir LevelPresence).
    public static class Levels
    {
        public const string HubScene = "Hub";
        public const string MapScene = "Labyrinthe";

        public static Level Current { get; private set; }
        public static bool Started { get; private set; }   // la première scène lancée a choisi le niveau de départ

        // LANCER appuyé avant que la carte soit chargée : la vague démarre dès qu'elle l'est (voir WaveSpawner.Start)
        public static bool LaunchOnArrival;

        // Sans rechargement du domaine (Enter Play Mode rapide), les statiques survivent d'une partie à l'autre : on repart de zéro
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { Started = false; LaunchOnArrival = false; }

        // Appelé par la première scène qui démarre : c'est le niveau où l'on commence
        public static void StartIn(Level level)
        {
            if (Started) return;
            Started = true;
            Current = level;
        }

        public static string SceneOf(Level level) => level == Level.Hub ? HubScene : MapScene;

        // Aller dans un niveau : sa scène est chargée si elle ne l'est pas encore (elle s'allumera toute seule),
        // sinon on allume sa présence et on éteint l'autre.
        public static void Go(Level level)
        {
            if (level == Current) return;
            Current = level;
            foreach (var presence in LevelPresence.All) presence.Refresh();   // éteint le niveau quitté, allume l'autre s'il est là
            if (!SceneManager.GetSceneByName(SceneOf(level)).isLoaded)
                SceneManager.LoadScene(SceneOf(level), LoadSceneMode.Additive);   // il s'allumera en arrivant (LevelPresence.Awake)
        }
    }
}
