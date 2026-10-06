using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Une case de la bibliothèque = un type de singe dans une rareté.
    // Elle affiche combien on en possède (inventaire) ; vide, elle est grisée et plus petite.
    // Clic : prendre un singe en main. Clic en tenant déjà un singe : le ranger.
    public class LibrarySlot : MonoBehaviour, IClickable, IMonkeyInfo
    {
        public MonkeyType type;
        public Rarity level;

        // Toutes les cases, pour que le coffre trouve où ranger un singe.
        public static readonly List<LibrarySlot> All = new List<LibrarySlot>();

        static readonly Color EmptyColor = new Color(0.2f, 0.2f, 0.22f);

        ColorTint body;
        Vector3 bodyScale;
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
            body = GetComponentInChildren<ColorTint>();
            bodyScale = body.transform.localScale;
            // Le nombre, écrit sur la face du cube tournée vers le joueur (-Z local)
            countLabel = Visuals.Label(transform, "", new Vector3(0, 0, -bodyScale.z / 2f - 0.005f), bodyScale.y * 0.6f, Color.black);
            Destroy(countLabel.GetComponent<Billboard>());
            Refresh();
        }

        void Refresh()
        {
            if (!body) return;
            int count = GameState.Count(Monkey);
            bool owned = count > 0;
            body.Set(owned ? MonkeyData.RarityColor(level) : EmptyColor, owned && MonkeyData.IsRainbow(level));
            body.transform.localScale = owned ? bodyScale : bodyScale * 0.6f;
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
