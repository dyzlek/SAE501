using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Une case de la bibliothèque = un type de singe dans une rareté.
    // Elle affiche combien on en possède (inventaire).
    // Possédé : le singe de la case (modèle 3D + aura) est un MonkeyToken, qu'on prend à la main (ou au clic en mode PC).
    // Vide : un singe sombre, plus petit et sans aura (on voit ce qu'on pourrait avoir).
    public class LibrarySlot : MonoBehaviour, IMonkeyInfo
    {
        public MonkeyType type;
        public Rarity level;

        // Toutes les cases, pour que le coffre trouve où ranger un singe.
        public static readonly List<LibrarySlot> All = new List<LibrarySlot>();

        static readonly Color EmptyColor = new Color(0.2f, 0.2f, 0.22f);

        MonkeyView view;       // le singe sombre, montré quand la case est vide
        TextMesh countLabel;
        MonkeyToken token;     // le singe posé sur la case, prêt à être pris
        float size;            // taille du singe de la case, en mètres

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
            foreach (Transform child in transform)
            {
                var corps = child.Find("Corps");
                if (corps) size = corps.localScale.x;
                Destroy(child.gameObject);
            }
            view = Visuals.MonkeyPiece(Monkey, transform, Vector3.zero, size, withLabel: false).GetComponent<MonkeyView>();

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
            view.SetEmpty(true, EmptyColor, Monkey);
            view.transform.localScale = Vector3.one * 0.6f;
            view.gameObject.SetActive(!owned);       // possédé : c'est le singe à saisir qu'on voit à sa place
            countLabel.text = owned ? count.ToString() : "";

            // Le singe à saisir, à la place du singe de la case, tant qu'il en reste
            if (owned && !token)
                token = MonkeyToken.Create(this, Monkey, Vector3.zero, size);
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
