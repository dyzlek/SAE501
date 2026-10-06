using UnityEngine;

namespace SAE
{
    // Ce qui montre un singe quand on le vise (case de la bibliothèque, singe posé sur le plateau ou la carte).
    // La fiche du singe (MonkeyInfoCard) s'en sert pour afficher ses caractéristiques.
    public interface IMonkeyInfo
    {
        // false si rien à montrer à cet endroit.
        // anchor = où poser la fiche ; rangeCenter/rangeScale = cercle de portée (rangeScale 0 = pas de cercle).
        bool TryGetMonkeyInfo(Vector3 point, out Monkey monkey, out Vector3 anchor, out Vector3 rangeCenter, out float rangeScale);
    }
}
