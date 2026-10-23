using UnityEngine;

namespace SAE
{
    // Une carte posée à plat sur le tapis : un carton blanc, son rang dans le coin (A, 2… 10, V, D, R) et sa couleur
    // dessinée au milieu avec quelques formes simples (nos polices n'ont pas les signes ♠ ♥ ♦ ♣) :
    //   carreau = un losange, cœur = deux ronds et une pointe, pique = un cœur à l'envers avec une tige, trèfle = trois ronds et une tige.
    // Face cachée : un dos rouge bordé d'or. Le haut de la carte est vers +Z (vers le croupier) : elle se lit depuis la place.
    // En arrivant, elle glisse depuis le sabot jusqu'à sa place (SlideFrom).
    public class BlackjackCard : MonoBehaviour
    {
        const float Width = 0.07f, Length = 0.1f;   // m : un peu plus grande qu'une vraie carte, pour la lire à 1 m
        static readonly Color Red = new Color(0.8f, 0.1f, 0.1f);
        static readonly Color Black = new Color(0.08f, 0.08f, 0.1f);
        static readonly Color Back = new Color(0.55f, 0.08f, 0.1f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);

        Vector3 from, to;    // en local : d'où elle part (le sabot) et sa place
        float t = 1f, duration;

        public static BlackjackCard Create(Transform parent, byte card, bool faceDown, Vector3 localPos)
        {
            var go = new GameObject(faceDown ? "Carte cachée" : $"Carte {BlackjackRules.RankName(card)} {BlackjackRules.SuitOf(card)}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var c = go.AddComponent<BlackjackCard>();
            c.to = localPos;

            var tr = go.transform;
            if (faceDown)
            {
                Visuals.Box("Bord", tr, Vector3.zero, new Vector3(Width, 0.002f, Length), Gold);
                Visuals.Box("Dos", tr, new Vector3(0f, 0.001f, 0f), new Vector3(Width - 0.01f, 0.002f, Length - 0.01f), Back);
                return c;
            }
            Visuals.Box("Carton", tr, Vector3.zero, new Vector3(Width, 0.002f, Length), Color.white);
            var color = BlackjackRules.IsRed(card) ? Red : Black;
            var rank = Visuals.Text(tr, BlackjackRules.RankName(card), new Vector3(-Width / 2f + 0.015f, 0.0015f, Length / 2f - 0.017f), 0.022f, color, title: true);
            rank.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Suit(tr, BlackjackRules.SuitOf(card), new Vector3(0.005f, 0.0015f, -0.012f), 0.03f, color);
            return c;
        }

        // La couleur de la carte, dans un carré de côté 'size', centré sur 'pos'
        static void Suit(Transform parent, CardSuit suit, Vector3 pos, float size, Color color)
        {
            var s = new GameObject("Couleur").transform;
            s.SetParent(parent, false);
            s.localPosition = pos;
            s.localScale = new Vector3(size, 0.001f, size);
            switch (suit)
            {
                case CardSuit.Carreau:
                    s.localScale = new Vector3(size * 0.75f, 0.001f, size * 1.15f);   // le carré tourné, étiré en hauteur : un losange
                    Diamond(s, Vector3.zero, new Vector3(0.7f, 1f, 0.7f), color);
                    break;
                case CardSuit.Coeur:
                    Heart(s, 1f, color);
                    break;
                case CardSuit.Pique:
                    Heart(s, -1f, color);
                    Visuals.Box("Tige", s, new Vector3(0f, 0f, -0.42f), new Vector3(0.12f, 1f, 0.3f), color);
                    break;
                case CardSuit.Trefle:
                    Round(s, new Vector3(0f, 0f, 0.25f), 0.48f, color);
                    Round(s, new Vector3(-0.25f, 0f, -0.12f), 0.48f, color);
                    Round(s, new Vector3(0.25f, 0f, -0.12f), 0.48f, color);
                    Visuals.Box("Tige", s, new Vector3(0f, 0f, -0.38f), new Vector3(0.12f, 1f, 0.35f), color);
                    break;
            }
        }

        // Deux ronds en haut et une pointe en bas (dir = 1) ; dir = -1 : retourné, la pointe en haut (pique)
        static void Heart(Transform s, float dir, Color color)
        {
            Round(s, new Vector3(-0.22f, 0f, 0.15f * dir), 0.5f, color);
            Round(s, new Vector3(0.22f, 0f, 0.15f * dir), 0.5f, color);
            Diamond(s, new Vector3(0f, 0f, -0.12f * dir), new Vector3(0.58f, 1f, 0.58f), color);
        }

        static void Diamond(Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            Visuals.Box("Losange", parent, pos, size, color).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }

        static void Round(Transform parent, Vector3 pos, float size, Color color)
        {
            var ball = Visuals.Primitive(PrimitiveType.Sphere);
            ball.name = "Rond";
            Visuals.Kill(ball.GetComponent<Collider>());
            ball.transform.SetParent(parent, false);
            ball.transform.localPosition = pos;
            ball.transform.localScale = new Vector3(size, 1f, size);
            ball.AddComponent<ColorTint>().Set(color);
        }

        // La carte part du sabot (position dans le repère du parent) et glisse jusqu'à sa place
        public void SlideFrom(Vector3 localFrom, float time)
        {
            from = localFrom;
            duration = time;
            t = 0f;
            transform.localPosition = from;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            transform.localPosition = Vector3.Lerp(from, to, 1f - (1f - t) * (1f - t));   // ralentit en arrivant
        }
    }
}
