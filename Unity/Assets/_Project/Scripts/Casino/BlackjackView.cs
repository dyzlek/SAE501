using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Ce qu'on voit de la borne de blackjack (BlackjackTable fait les règles), façon « Gamble With Your Friends ».
    // Construite par le code au lancement, comme le panneau du classement. Repère local : +Z = vers le croupier,
    // le joueur est côté -Z. Tout est à portée de main, à hauteur de plan de travail :
    //   - un comptoir au tapis vert ; derrière, le croupier (un singe à chapeau) sous une arche noire et ses deux lampes ;
    //   - devant le joueur, la console penchée vers lui : DOUBLER (violet), RESTER (jaune), CARTE (rouge), JOUER (vert) ;
    //   - à sa droite, le terminal de mise : un pavé (chiffres, C, 000), l'écran (min, max, mise, message)
    //     et les mises rapides MIN, 1/4, 1/2, MAX (une part de la bourse).
    // Chaque image, on compare la partie à ce qui est dessiné : une carte en plus glisse depuis le sabot avec un « tic »,
    // la carte cachée du croupier se retourne, un résultat joue son son. Les textes ne sont refaits que s'ils changent.
    public class BlackjackView : MonoBehaviour
    {
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color Felt = new Color(0.08f, 0.4f, 0.3f);
        static readonly Color Frame = new Color(0.07f, 0.07f, 0.09f);
        static readonly Color Lamp = new Color(1f, 0.92f, 0.6f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Screen = new Color(0.05f, 0.04f, 0.05f);
        static readonly Color Digital = new Color(1f, 0.25f, 0.2f);   // l'écran rouge du terminal
        static readonly Color Key = new Color(0.85f, 0.83f, 0.78f);
        static readonly Color Orange = new Color(1f, 0.6f, 0.1f);
        static readonly Color Purple = new Color(0.45f, 0.35f, 0.85f);
        static readonly Color Yellow = new Color(0.95f, 0.75f, 0.15f);
        static readonly Color Red = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color Green = new Color(0.35f, 0.75f, 0.25f);

        const float TopY = 0.9f;                     // m : le dessus du comptoir
        const float CardStep = 0.08f;                // m entre deux cartes d'une main
        const float SlideTime = 0.3f;                // s : une carte glisse du sabot à sa place

        BlackjackTable table;
        Transform root;
        Vector3 shoe;                                // le sabot, d'où partent les cartes
        readonly List<GameObject> dealerCards = new List<GameObject>();
        readonly List<GameObject> handCards = new List<GameObject>();
        TextMesh dealerTotal, handTotal, screenLimits, screenBet, screenMessage;
        bool shownHidden;
        BlackjackResult shownResult;
        (int, int, bool, int, int, BlackjackPhase, string) shownTexts;   // ce que montrent les textes (voir RefreshTexts)

        public static BlackjackView Build(BlackjackTable table)
        {
            var view = table.gameObject.AddComponent<BlackjackView>();
            view.table = table;
            view.root = table.transform;
            view.BuildBooth();
            return view;
        }

        // Devant le joueur, au-dessus du comptoir (pour les sons et les « +40 » de la bourse)
        public Vector3 PlayerPosition => root.TransformPoint(0f, TopY, -0.1f);

        void BuildBooth()
        {
            // Le comptoir et son tapis
            Visuals.Solid("Comptoir", root, new Vector3(0f, TopY / 2f, 0.05f), new Vector3(1.3f, TopY, 0.7f), Wood);
            Visuals.Box("Tapis", root, new Vector3(0f, TopY + 0.005f, 0.05f), new Vector3(1.24f, 0.01f, 0.64f), Felt);

            // Le croupier, son chapeau, et son sabot à sa droite (vue du joueur : à gauche)
            var dealer = Visuals.MonkeyPiece(new Monkey(MonkeyType.Classique, Rarity.Gris), root, new Vector3(0f, TopY + 0.33f, 0.62f), 0.6f, withLabel: false);
            dealer.name = "Croupier";
            Cylinder("Bord du chapeau", new Vector3(0f, TopY + 0.65f, 0.62f), new Vector3(0.3f, 0.006f, 0.3f), Frame);
            Cylinder("Chapeau", new Vector3(0f, TopY + 0.72f, 0.62f), new Vector3(0.17f, 0.07f, 0.17f), Frame);
            shoe = new Vector3(-0.5f, TopY + 0.06f, 0.3f);
            Visuals.Box("Sabot", root, shoe, new Vector3(0.12f, 0.1f, 0.18f), Frame);

            // L'arche noire autour du croupier, ses deux lampes, et l'enseigne
            const float ArchZ = 0.38f, ArchX = 0.64f, ArchTop = 2.15f;
            for (int side = -1; side <= 1; side += 2)
            {
                Visuals.Box("Montant", root, new Vector3(side * ArchX, (TopY + ArchTop) / 2f, ArchZ), new Vector3(0.06f, ArchTop - TopY, 0.06f), Frame);
                var corner = Visuals.Box("Coin", root, new Vector3(side * (ArchX - 0.12f), ArchTop + 0.08f, ArchZ), new Vector3(0.06f, 0.32f, 0.06f), Frame);
                corner.transform.localRotation = Quaternion.Euler(0f, 0f, side * 45f);
                var lamp = Visuals.Primitive(PrimitiveType.Sphere);
                lamp.name = "Lampe";
                Visuals.Kill(lamp.GetComponent<Collider>());
                lamp.transform.SetParent(root, false);
                lamp.transform.localPosition = new Vector3(side * (ArchX - 0.2f), ArchTop + 0.12f, ArchZ - 0.05f);
                lamp.transform.localScale = Vector3.one * 0.06f;
                lamp.AddComponent<ColorTint>().Set(Lamp);
            }
            Visuals.Box("Haut", root, new Vector3(0f, ArchTop + 0.2f, ArchZ), new Vector3(2f * ArchX - 0.36f, 0.06f, 0.06f), Frame);
            Visuals.Box("Enseigne", root, new Vector3(0f, ArchTop + 0.42f, ArchZ), new Vector3(1.2f, 0.26f, 0.04f), Frame);
            Visuals.Text(root, "BLACKJACK", new Vector3(0f, ArchTop + 0.42f, ArchZ - 0.03f), 0.16f, Gold, title: true);

            dealerTotal = FlatText(new Vector3(0.42f, TopY + 0.012f, 0.3f), 0.05f, Color.white);
            handTotal = FlatText(new Vector3(0.42f, TopY + 0.012f, -0.04f), 0.06f, Gold);

            BuildConsole();
            BuildTerminal();
        }

        // La console des gros boutons, posée au bord du comptoir et penchée vers le joueur
        void BuildConsole()
        {
            var console = new GameObject("Console").transform;
            console.SetParent(root, false);
            console.localPosition = new Vector3(0f, TopY + 0.07f, -0.2f);
            console.localRotation = Quaternion.Euler(45f, 0f, 0f);   // la face des boutons regarde vers le haut et vers le joueur
            Visuals.Box("Pupitre", console, new Vector3(0f, 0f, 0.03f), new Vector3(1.0f, 0.17f, 0.04f), Frame);
            var size = new Vector2(0.2f, 0.11f);
            BlackjackButton.Create(console, "DOUBLER", new Vector3(-0.36f, 0f, 0f), size, Purple, table.Double, () => table.CanDouble, 0.04f);
            BlackjackButton.Create(console, "RESTER", new Vector3(-0.12f, 0f, 0f), size, Yellow, table.Stand, () => table.CanAct, 0.04f);
            BlackjackButton.Create(console, "CARTE", new Vector3(0.12f, 0f, 0f), size, Red, table.Hit, () => table.CanAct, 0.045f);
            BlackjackButton.Create(console, "JOUER", new Vector3(0.36f, 0f, 0f), size, Green, table.Play, () => table.CanPlay, 0.045f);
        }

        // Le terminal de mise, sur son pied, à droite du joueur, un peu tourné vers lui
        void BuildTerminal()
        {
            Visuals.Solid("Pied du terminal", root, new Vector3(0.95f, 0.5f, -0.2f), new Vector3(0.06f, 1f, 0.06f), Frame);
            var t = new GameObject("Terminal").transform;
            t.SetParent(root, false);
            t.localPosition = new Vector3(0.95f, 1.15f, -0.22f);
            t.localRotation = Quaternion.Euler(20f, 25f, 0f);   // penché en arrière, et tourné vers le joueur (à sa gauche)
            Visuals.Box("Boîtier", t, new Vector3(0f, 0f, 0.03f), new Vector3(0.52f, 0.34f, 0.04f), Frame);

            // Le pavé : 1 à 9, puis C, 0, 000
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "000" };
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                var pos = new Vector3(-0.2f + (i % 3) * 0.06f, 0.1f - (i / 3) * 0.065f, 0f);
                if (k == "C") BlackjackButton.Create(t, k, pos, new Vector2(0.05f, 0.05f), Red, table.ClearBet, () => table.CanType, 0.03f);
                else BlackjackButton.Create(t, k, pos, new Vector2(0.05f, 0.05f), Key, () => table.TypeDigits(k), () => table.CanType, k.Length > 1 ? 0.02f : 0.03f);
            }

            // L'écran
            Visuals.Box("Écran", t, new Vector3(0.04f, 0f, 0f), new Vector3(0.19f, 0.28f, 0.01f), Screen);
            screenLimits = Visuals.Text(t, "", new Vector3(0.04f, 0.09f, -0.01f), 0.022f, Color.white);
            screenBet = Visuals.Text(t, "", new Vector3(0.04f, 0f, -0.01f), 0.06f, Digital, title: true);
            screenMessage = Visuals.Text(t, "", new Vector3(0.04f, -0.09f, -0.01f), 0.016f, Digital);

            // Les mises rapides
            string[] names = { "MIN", "1/4", "1/2", "MAX" };
            float[] shares = { 0f, 0.25f, 0.5f, 1f };
            for (int i = 0; i < names.Length; i++)
            {
                float share = shares[i];
                BlackjackButton.Create(t, names[i], new Vector3(0.2f, 0.1f - i * 0.065f, 0f), new Vector2(0.08f, 0.05f), Orange,
                                       () => table.QuickBet(share), () => table.CanType, 0.025f);
            }
        }

        TextMesh FlatText(Vector3 pos, float height, Color color)
        {
            var text = Visuals.Text(root, "", pos, height, color, title: true);
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // à plat, lisible depuis la place du joueur
            return text;
        }

        void Cylinder(string name, Vector3 pos, Vector3 size, Color color)
        {
            var go = Visuals.Primitive(PrimitiveType.Cylinder);
            go.name = name;
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.AddComponent<ColorTint>().Set(color);
        }

        // --- Chaque image : dessiner ce qui a changé ---

        void Update()
        {
            // La carte cachée du croupier se retourne quand il joue
            if (table.DealerHidden != shownHidden)
            {
                if (!table.DealerHidden && dealerCards.Count > 1)
                {
                    Destroy(dealerCards[1]);
                    dealerCards[1] = BlackjackCard.Create(root, table.Dealer[1], false, CardPos(1, 0.3f)).gameObject;
                }
                shownHidden = table.DealerHidden;
            }
            SyncCards(dealerCards, table.Dealer, 0.3f, true);
            SyncCards(handCards, table.Hand, -0.04f, false);

            if (table.Result != shownResult)
            {
                shownResult = table.Result;
                PlayResult(shownResult);
            }
            RefreshTexts();
        }

        // Ajoute les cartes qui manquent (elles glissent depuis le sabot), ou range tout si la main a été vidée
        void SyncCards(List<GameObject> drawn, List<byte> cards, float z, bool dealer)
        {
            if (cards.Count < drawn.Count)
            {
                foreach (var c in drawn) Destroy(c);
                drawn.Clear();
            }
            for (int i = drawn.Count; i < cards.Count; i++)
            {
                bool hidden = dealer && i == 1 && table.DealerHidden;
                var card = BlackjackCard.Create(root, cards[i], hidden, CardPos(i, z));
                card.SlideFrom(shoe, SlideTime);
                drawn.Add(card.gameObject);
                AudioSource.PlayClipAtPoint(Sounds.Tick, card.transform.position, 0.7f);
            }
        }

        static Vector3 CardPos(int i, float z) => new Vector3(-0.2f + i * CardStep, TopY + 0.012f + i * 0.001f, z);

        void PlayResult(BlackjackResult result)
        {
            var at = PlayerPosition;
            switch (result)
            {
                case BlackjackResult.Blackjack: Sounds.Play(Sounds.VagueGagnee, at); break;
                case BlackjackResult.Won: Sounds.Play(Sounds.SingeObtenu, at); break;
                case BlackjackResult.Push: Sounds.Play(Sounds.Bouton, at); break;
                case BlackjackResult.Lost:
                case BlackjackResult.Bust: Sounds.Play(Sounds.ViePerdue, at, 0.8f, 0.9f); break;
            }
        }

        // Les totaux et l'écran du terminal ; refaits seulement si quelque chose a changé
        void RefreshTexts()
        {
            int money = Economy.Money;
            var now = (table.Dealer.Count, table.Hand.Count, table.DealerHidden, table.Bet, money, table.Phase, table.Message);
            if (now.Equals(shownTexts)) return;
            shownTexts = now;

            dealerTotal.text = table.Dealer.Count == 0 ? "" : table.DealerHidden ? "?" : BlackjackRules.Total(table.Dealer).ToString();
            handTotal.text = table.Hand.Count == 0 ? "" : BlackjackRules.Total(table.Hand).ToString();
            screenLimits.text = $"Min : {BlackjackRules.MinBet}\nMax : {money}";
            screenBet.text = table.CanType ? table.Bet.ToString() : table.Played.ToString();
            screenMessage.text = table.Message.Length > 0 ? table.Message
                : table.Phase switch
                {
                    BlackjackPhase.Betting => "Tape ta mise\npuis JOUER",
                    BlackjackPhase.Playing => "CARTE, RESTER\nou DOUBLER",
                    BlackjackPhase.Dealer => "Le croupier joue...",
                    _ => "",
                };
        }
    }
}
