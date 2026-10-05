using UnityEngine;

namespace SAE
{
    // Une case de la bibliothèque : cliquer donne une copie du singe en main.
    // Pas encore de coffres : tous les singes de tous les niveaux sont disponibles.
    public class LibrarySlot : MonoBehaviour, IClickable
    {
        public MonkeyType type;
        public Rarity level;

        Monkey Monkey => new Monkey(type, level);

        public string GetHint(Vector3 point) => $"Prendre : {Monkey}";

        public void OnClick(PlayerController player, Vector3 point)
        {
            GameState.Held = Monkey;
            GameState.NotifyChanged();
        }
    }
}
