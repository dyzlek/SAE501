using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Une case de la bibliothèque = un type de singe dans une rareté.
    // Elle affiche combien on en possède (inventaire) ; vide, le singe est sombre, plus petit et sans aura.
    // Clic : prendre un singe en main. Clic en tenant déjà un singe : le ranger.
    public class LibrarySlot : MonoBehaviour, IClickable, IMonkeyInfo
    {
        public MonkeyType type;
        public Rarity level;

        // Toutes les cases, pour que le coffre trouve où ranger un singe.
        public static readonly List<LibrarySlot> All = new List<LibrarySlot>();

        static readonly Color EmptyColor = new Color(0.2f, 0.2f, 0.22f);

        MonkeyView view;
        TextMesh countLabel;

        Monkey Monkey => new Monkey(type, level);

        public static LibrarySlot Find(Monkey m) => All.Find(s => s.type == m.type && s.level == m.level);

        void OnEnable()
        {
            All.Add(this);
            GameState.Changed += Refresh;
        }

        void OnDisable()
        {
            All.Remove(this);
            GameState.Changed -= Refresh;
        }

        void Start()
        {
            // La scène a pu être générée avant l'arrivée des modèles 3D (cases en cubes) :
            // on reconstruit le singe de la case pour avoir la version à jour (modèle + aura).
            float size = 0f;
            foreach (Transform child in transform)
            {
                var corps = child.Find("Corps");
                if (corps) size = corps.localScale.x;
                Destroy(child.gameObject);
            }
            view = Visuals.MonkeyPiece(Monkey, transform, Vector3.zero, size).GetComponent<MonkeyView>();

            // Le nombre possédé, devant le singe, côté joueur (-Z local)
            bool hasModel = view.model;
            var labelPos = hasModel ? new Vector3(0, -size * 0.35f, -size / 2f - 0.01f) : new Vector3(0, 0, -size / 2f - 0.005f);
            countLabel = Visuals.Label(transform, "", labelPos, size * (hasModel ? 0.4f : 0.6f), hasModel ? Color.white : Color.black);
            Destroy(countLabel.GetComponent<Billboard>());
            Refresh();
        }

        void Refresh()
        {
            if (!view) return;
            int count = GameState.Count(Monkey);
            bool owned = count > 0;
            view.SetEmpty(!owned, EmptyColor, Monkey);
            view.transform.localScale = owned ? Vector3.one : Vector3.one * 0.6f;
            countLabel.text = owned ? count.ToString() : "";
        }

        public string GetHint(Vector3 point)
        {
            if (GameState.Held != null) return $"Ranger : {GameState.Held}";
            int count = GameState.Count(Monkey);
            return count > 0 ? $"Prendre : {Monkey} (×{count})" : $"{Monkey} : aucun (ouvre le coffre)";
        }

        // La fiche s'affiche au-dessus de la case, même vide (on voit ce que vaut un singe avant de l'avoir).
        public bool TryGetMonkeyInfo(Vector3 point, out Monkey monkey, out Vector3 anchor, out Vector3 rangeCenter, out float rangeScale)
        {
            monkey = Monkey;
            anchor = transform.position + Vector3.up * 0.3f - transform.forward * 0.15f;
            rangeCenter = Vector3.zero;
            rangeScale = 0f;
            return true;
        }

        public void OnClick(PlayerController player, Vector3 point)
        {
            if (GameState.Held != null) GameState.ReturnHeld();
            else GameState.TakeFromInventory(Monkey);
        }
    }
}
