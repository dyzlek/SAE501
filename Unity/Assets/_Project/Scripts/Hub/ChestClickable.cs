using UnityEngine;
using UnityEngine.InputSystem;
using Sae501.Coffres;

namespace SAE
{
    // Branche le coffre de Nicolas (ChestController) sur notre joueur :
    // clic gauche en visant le coffre, ou touche E quand on est assez près.
    public class ChestClickable : MonoBehaviour, IClickable
    {
        public ChestController chest;

        bool InRange => PlayerController.Local && chest.IsInRange(PlayerController.Local.transform.position);

        public string GetHint(Vector3 point)
        {
            if (chest.IsBusy) return "Ouverture en cours…";
            return InRange ? $"Ouvrir le coffre (prix : {chest.Price})" : "Coffre : approche-toi";
        }

        public void OnClick(PlayerController player, Vector3 point)
        {
            if (InRange) chest.Interact();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && InRange) chest.Interact();
        }
    }
}
