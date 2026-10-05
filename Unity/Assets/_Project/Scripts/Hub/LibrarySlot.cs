using UnityEngine;

namespace SAE
{
    // Bibliothèque simplifiée : un seul socle par type de singe.
    // Le cube prend la couleur de la rareté choisie avec le sélecteur (RarityButton).
    // Pas encore de coffres : toutes les raretés sont disponibles.
    public class LibrarySlot : MonoBehaviour, IClickable
    {
        public MonkeyType type;

        GameObject piece;
        Rarity shown = (Rarity)(-1);

        Monkey Monkey => new Monkey(type, GameState.SelectedRarity);

        public string GetHint(Vector3 point) => $"Prendre : {Monkey}";

        public void OnClick(PlayerController player, Vector3 point)
        {
            GameState.Held = Monkey;
            GameState.NotifyChanged();
        }

        void OnEnable()
        {
            GameState.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => GameState.Changed -= Refresh;

        void Refresh()
        {
            if (shown == GameState.SelectedRarity && piece) return;
            shown = GameState.SelectedRarity;
            if (piece) Destroy(piece);
            piece = Visuals.MonkeyPiece(Monkey, transform, Vector3.zero, 0.14f);
        }
    }
}
