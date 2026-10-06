using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Branche le coffre de Nicolas (ChestController) sur la main du joueur : on le touche, il s'ouvre (s'il est payable).
    public class ChestClickable : MonoBehaviour, IPressable
    {
        public ChestController chest;

        public void Press() => chest.Interact();
    }
}
