using UnityEngine;

namespace SAE
{
    // Tout objet sur lequel le joueur peut cliquer (plus tard : saisir avec la main en VR).
    // point = endroit exact visé, utile pour poser un singe librement sur le plateau.
    public interface IClickable
    {
        string GetHint(Vector3 point);
        void OnClick(PlayerController player, Vector3 point);
    }
}
