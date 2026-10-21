using System.Collections;
using UnityEngine;

namespace SAE
{
    // Le passage entre le jeu et le menu principal (#158). Pendant le menu (et la pause), le jeu est ARRÊTÉ
    // (Time.timeScale = 0) : une vague en cours attend, les bananes ne pourrissent pas. Le menu et le voile noir des
    // téléportations utilisent le temps réel, ils marchent quand même.
    //   OpenAtLaunch() : au lancement (LevelLoader), on part dans le menu ;
    //   ToMenu()       : depuis la pause (MENU PRINCIPAL) : la partie est sauvegardée, puis on y va sous le voile noir ;
    //   Continue()     : CONTINUER, on revient à la cabane et le jeu repart ;
    //   NewGame()      : NOUVELLE PARTIE, la sauvegarde est effacée, la cabane se recharge, on y reste (pas de menu).
    public static class GameMenu
    {
        static bool skipMenuOnce;   // juste après NOUVELLE PARTIE : on commence directement dans la cabane

        public static bool InMenu => Levels.Current == Level.Menu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { skipMenuOnce = false; Time.timeScale = 1f; }

        public static void OpenAtLaunch()
        {
            if (skipMenuOnce) { skipMenuOnce = false; return; }
            Time.timeScale = 0f;
            Levels.Go(Level.Menu);   // la scène du menu se charge et y pose le joueur (LevelPresence)
        }

        public static void ToMenu()
        {
            if (InMenu || ScreenCurtain.Busy) return;
            SaveGame.Save();
            Time.timeScale = 0f;
            Runner.Run(ScreenCurtain.Teleport(() => Levels.Go(Level.Menu)));
        }

        public static void Continue()
        {
            if (!InMenu || ScreenCurtain.Busy) return;
            Time.timeScale = 1f;
            Runner.Run(ScreenCurtain.Teleport(() => Levels.Go(Level.Hub)));
        }

        public static void NewGame()
        {
            Time.timeScale = 1f;
            skipMenuOnce = true;
            SaveGame.NewGame();   // efface et recharge la cabane
        }

        // Un objet caché qui fait tourner les coroutines (une classe statique ne peut pas en lancer)
        class Runner : MonoBehaviour
        {
            static Runner instance;
            public static void Run(IEnumerator routine)
            {
                if (!instance)
                {
                    var go = new GameObject("Menu") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Runner>();
                }
                instance.StartCoroutine(routine);
            }
        }
    }
}
