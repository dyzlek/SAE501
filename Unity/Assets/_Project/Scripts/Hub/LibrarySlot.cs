using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Une case de la bibliothèque = un type de singe dans une rareté.
    // Elle affiche combien on en possède (inventaire) ; vide, elle est grisée et plus petite.
    // Si on en possède au moins un, un petit singe à saisir (MonkeyToken) est posé sur elle.
    public class LibrarySlot : MonoBehaviour, IMonkeyInfo
    {
        public MonkeyType type;
        public Rarity level;

        // Toutes les cases, pour que le coffre trouve où ranger un singe.
        public static readonly List<LibrarySlot> All = new List<LibrarySlot>();

        static readonly Color EmptyColor = new Color(0.2f, 0.2f, 0.22f);

        ColorTint body;
        Vector3 bodyScale;
        TextMesh countLabel;
        MonkeyToken token;     // le singe posé sur la case, prêt à être pris

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

            // Un singe à saisir sur le dessus de la case tant qu'il en reste
            if (owned && !token)
                token = MonkeyToken.Create(this, Monkey, Vector3.up * (bodyScale.y + MonkeyToken.Size) / 2f);
            else if (!owned && token)
            {
                Destroy(token.gameObject);
                token = null;
            }
        }

        // Appelé quand on prend le singe posé : il n'appartient plus à la case (elle en posera un autre).
        public void Detach(MonkeyToken taken)
        {
            if (token == taken) token = null;
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
    }
}
