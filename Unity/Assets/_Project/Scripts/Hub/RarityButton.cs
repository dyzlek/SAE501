using UnityEngine;

namespace SAE
{
    // Pastille de couleur : choisit la rareté des singes pris dans la bibliothèque.
    // La pastille choisie est plus grosse.
    public class RarityButton : MonoBehaviour, IClickable
    {
        public Rarity rarity;

        Vector3 baseScale;

        public string GetHint(Vector3 point) => $"Rareté : {MonkeyData.RarityName(rarity)} (niv {(int)rarity + 1})";

        public void OnClick(PlayerController player, Vector3 point)
        {
            GameState.SelectedRarity = rarity;
            GameState.NotifyChanged();
        }

        void Awake() => baseScale = transform.localScale;

        void Update()
        {
            bool selected = GameState.SelectedRarity == rarity;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * (selected ? 1.6f : 1f), Time.deltaTime * 12f);
        }
    }
}
