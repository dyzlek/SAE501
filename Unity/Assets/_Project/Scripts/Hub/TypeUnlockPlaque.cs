using UnityEngine;

namespace SAE
{
    // La plaque au bout d'une étagère de la bibliothèque, avec le nom du type de singe.
    // Tant que le type n'est pas débloqué, elle est grise et affiche son prix : on l'enfonce (main ou rayon)
    // pour l'acheter avec des bananes. Ensuite, le coffre peut donner ce type de singe.
    // (Avant, les types arrivaient tout seuls avec les vagues : on choisit maintenant lesquels on veut.)
    public class TypeUnlockPlaque : MonoBehaviour, IPressable
    {
        public MonkeyType type;
        public TextMesh label;
        public ColorTint plate;              // la plaque elle-même : claire si débloqué, grise sinon

        public static readonly Color Unlocked = new Color(0.93f, 0.86f, 0.68f);   // parchemin, comme avant
        static readonly Color Locked = new Color(0.28f, 0.27f, 0.26f);   // foncé : le prix doré se lit bien
        static readonly Color Refused = new Color(0.95f, 0.25f, 0.25f);
        static readonly Color Engraved = new Color(0.22f, 0.13f, 0.06f);
        static readonly Color Price = new Color(1f, 0.82f, 0.2f);

        float refused;                       // 1 = flash rouge « pas assez de bananes », revient à 0

        int Cost => MonkeyData.UnlockPrice(type);

        public void Press()
        {
            if (GameState.IsUnlocked(type)) return;
            if (Economy.TrySpend(Cost, transform.position)) GameState.Unlock(type);
            else refused = 1f;
        }

        void Update()
        {
            bool open = GameState.IsUnlocked(type);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            if (label)
            {
                label.text = open ? type.ToString() : $"{type}\n{Cost} bananes";
                label.color = open ? Engraved : Price;
            }
            if (plate)
            {
                var color = open ? Unlocked : Color.Lerp(Locked, Refused, refused);
                if (plate.color != color) plate.Set(color);
            }
        }
    }
}
