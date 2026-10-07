using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Raccourci au casque : le bouton B de la manette droite lance la vague, où qu'on soit (hub ou carte).
    // Le pupitre LANCER reste le geste « normal » ; le raccourci évite d'aller jusqu'au pupitre pour tester.
    public class WaveShortcut : MonoBehaviour
    {
        InputAction launch;

        void OnEnable()
        {
            launch = new InputAction("Lancer la vague", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");
            launch.performed += OnLaunch;
            launch.Enable();
        }

        void OnDisable()
        {
            launch.performed -= OnLaunch;
            launch.Disable();
        }

        void OnLaunch(InputAction.CallbackContext ctx)
        {
            var spawner = WaveSpawner.Instance;
            if (!spawner || spawner.Running) return;
            spawner.StartWave();
            if (PlayerRig.Local) PlayerRig.Buzz(PlayerRig.Local.rightHand, 0.6f);   // « c'est parti »
        }
    }
}
