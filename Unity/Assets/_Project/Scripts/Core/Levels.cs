using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    public enum Level { Hub, Carte }

    // Les deux niveaux du jeu, chacun dans sa scène : Hub (la cabane) et Labyrinthe (la carte), loin l'un de l'autre.
    // Les deux scènes restent chargées (la seconde est chargée EN PLUS de la première, chargement additif) : le monde
    // que l'on quitte continue de tourner. Une vague lancée continue pendant qu'on va chercher des bananes au hub,
    // et les bananes tombent pendant qu'on défend la carte.
    // Il n'y a qu'UN joueur (dans la scène Hub) : XRI ne gère bien qu'un seul joueur VR. Changer de niveau, c'est
    // le déplacer jusqu'au point d'arrivée de l'autre scène, et allumer le soleil et les réglages d'image de cette scène
    // (voir LevelPresence). L'arc, lui, ne sort que sur la carte (BowHolster écoute Changed).
    public static class Levels
    {
        public const string HubScene = "Hub";
        public const string MapScene = "Labyrinthe";

        public static Level Current { get; private set; }
        public static event System.Action<Level> Changed;   // on vient d'arriver dans ce niveau
        public static bool Started { get; private set; }   // la première scène lancée a choisi le niveau de départ

        // LANCER appuyé avant que la carte soit chargée : la vague démarre dès qu'elle l'est (voir WaveSpawner.Start)
        public static bool LaunchOnArrival;

        // Sans rechargement du domaine (Enter Play Mode rapide), les statiques survivent d'une partie à l'autre : on repart de zéro
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { Started = false; LaunchOnArrival = false; Current = Level.Hub; }

        // Appelé par la première scène qui démarre : c'est le niveau où l'on commence
        public static void StartIn(Level level)
        {
            if (Started) return;
            Started = true;
            Current = level;
        }

        public static string SceneOf(Level level) => level == Level.Hub ? HubScene : MapScene;

        // REJOUER (après la victoire) : une partie neuve. On remet à zéro ce qui survit aux scènes (l'argent, les singes,
        // les améliorations de l'arc), puis on recharge le hub seul ; il recharge la carte, comme au lancement.
        public static void Restart()
        {
            GameState.ResetAll();
            BowUpgrades.ResetLevels();
            ResetState();
            SceneManager.LoadScene(HubScene, LoadSceneMode.Single);
        }

        // Aller dans un niveau : sa scène est chargée si elle ne l'est pas encore (elle s'allumera toute seule),
        // sinon on éteint la présence de l'autre et on allume la sienne (le joueur y est déplacé).
        public static void Go(Level level)
        {
            if (level == Current) return;
            Current = level;
            // D'abord éteindre le niveau quitté, PUIS allumer l'autre (un seul soleil allumé à la fois)
            foreach (var presence in LevelPresence.All) if (presence.level != level) presence.Refresh();
            foreach (var presence in LevelPresence.All) if (presence.level == level) presence.Refresh();
            if (!SceneManager.GetSceneByName(SceneOf(level)).isLoaded)
                SceneManager.LoadScene(SceneOf(level), LoadSceneMode.Additive);   // il s'allumera en arrivant (LevelPresence.Awake)
            Changed?.Invoke(level);
        }
    }
}
