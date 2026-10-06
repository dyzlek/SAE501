using UnityEngine;

namespace SAE
{
    // Choisit le joueur au lancement : VR (casque) ou PC (clavier/souris, pour tester vite).
    // Les deux joueurs sont dans la scène, désactivés ; on n'en active qu'un ici, avant qu'ils ne démarrent.
    // Le mode se change avec le menu SAE → Mode de jeu. Sur le casque (Android), c'est toujours la VR.
    public class PlayerMode : MonoBehaviour
    {
        public bool vr = true;
        public GameObject vrPlayer;
        public GameObject pcPlayer;

        void Awake()
        {
            bool useVR = vr || Application.platform == RuntimePlatform.Android;
            vrPlayer.SetActive(useVR);
            pcPlayer.SetActive(!useVR);
        }
    }
}
