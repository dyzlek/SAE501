using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Ce qu'on voit de la borne de blackjack (BlackjackTable fait les règles), façon « Gamble With Your Friends ».
    // Construite par le code au lancement, comme le panneau du classement. Repère local : +Z = vers le croupier,
    // le joueur est côté -Z. Tout est à portée de main, à hauteur de plan de travail :
    //   - un comptoir au tapis vert ; derrière, le croupier (un singe à chapeau) sous une arche noire et ses deux lampes ;
    //   - les cartes en grand sur le tapis, avec le total de chaque main sur une plaque debout (lisible de loin) ;
    //   - au bord avant du comptoir, la console penchée vers le joueur, plus basse que le tapis (elle ne cache pas
    //     les cartes) : DOUBLER (violet), RESTER (jaune), CARTE (rouge), JOUER (vert) ;
    //   - à sa droite, le terminal de mise : un pavé (chiffres, C, 000), l'écran (min, max, mise, message)
    //     et les mises rapides MIN, 1/4, 1/2, MAX (une part de la bourse).
    // Chaque image, on compare la partie à ce qui est dessiné : une carte en plus glisse depuis le sabot avec un « tic »,
    // la carte cachée du croupier se retourne, un résultat joue son son. Les textes ne sont refaits que s'ils changent.
    public class BlackjackView : MonoBehaviour
    {
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color DarkWood = new Color(0.30f, 0.19f, 0.10f);
        static readonly Color Felt = new Color(0.08f, 0.4f, 0.3f);
        static readonly Color Frame = new Color(0.07f, 0.07f, 0.09f);
        static readonly Color Lamp = new Color(1f, 0.92f, 0.6f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Screen = new Color(0.05f, 0.04f, 0.05f);
        static readonly Color Digital = new Color(1f, 0.3f, 0.25f);   // l'écran rouge du terminal
        static readonly Color Key = new Color(0.92f, 0.9f, 0.85f);
        static readonly Color KeyText = new Color(0.12f, 0.12f, 0.14f);
        static readonly Color Orange = new Color(1f, 0.6f, 0.1f);
        static readonly Color Purple = new Color(0.5f, 0.38f, 0.9f);
        static readonly Color Yellow = new Color(0.95f, 0.75f, 0.15f);
        static readonly Color Red = new Color(0.88f, 0.18f, 0.15f);
        static readonly Color Green = new Color(0.35f, 0.75f, 0.25f);

        const float TopY = 0.9f;                     // m : le dessus du comptoir
        const float FrontZ = -0.35f;                 // m : le bord avant du comptoir (côté joueur)
        const float DealerZ = 0.25f, HandZ = -0.08f; // m : les deux rangées de cartes
        const float CardStep = 0.115f;               // m entre deux cartes d'une main (une carte fait 10 cm de large)
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
        public Vector3 PlayerPosition => root.TransformPoint(0f, TopY, HandZ);

        void BuildBooth()
        {
            // Le comptoir et son tapis
            Visuals.Solid("Comptoir", root, new Vector3(0f, TopY / 2f, 0.05f), new Vector3(1.4f, TopY, 0.8f), Wood);
            Visuals.Box("Tapis", root, new Vector3(0f, TopY + 0.005f, 0.05f), new Vector3(1.34f, 0.01f, 0.74f), Felt);

            BuildDealer();

            // Les totaux, sur deux plaques debout : derrière les cartes du croupier, et à gauche des nôtres
            dealerTotal = Plaque("Total du croupier", new Vector3(0.52f, TopY + 0.06f, DealerZ + 0.04f), new Vector2(0.3f, 0.1f));
            handTotal = Plaque("Ton total", new Vector3(-0.53f, TopY + 0.06f, HandZ), new Vector2(0.22f, 0.1f));

            BuildConsole();
            BuildTerminal();
        }

        // Le croupier (le modèle du singe, sans halo de rareté), son chapeau posé sur la tête, son sabot,
        // et l'arche noire autour de lui avec ses deux lampes et l'enseigne
        void BuildDealer()
        {
            var spot = new Vector3(0f, TopY + 0.3f, 0.62f);
            var asset = MonkeyVisuals.Model(MonkeyType.Classique, out float yaw);
            float headTop = spot.y + 0.3f;
            if (asset)
            {
                var dealer = new GameObject("Croupier").transform;
                dealer.SetParent(root, false);
                dealer.localPosition = spot;
                var model = Visuals.FitModel(asset, yaw, dealer, 0.6f, out var fitted);
                Visuals.RestPose(model.transform, dealer);
                headTop = spot.y + fitted.y / 2f;
            }
            Cylinder("Bord du chapeau", new Vector3(0f, headTop + 0.004f, spot.z), new Vector3(0.24f, 0.004f, 0.24f), Frame);
            Cylinder("Chapeau", new Vector3(0f, headTop + 0.06f, spot.z), new Vector3(0.14f, 0.055f, 0.14f), Frame);

            shoe = new Vector3(-0.55f, TopY + 0.035f, DealerZ);
            Visuals.Box("Sabot", root, shoe, new Vector3(0.12f, 0.07f, 0.16f), DarkWood);

            const float ArchZ = 0.45f, ArchX = 0.68f, ArchTop = 2.1f;
            for (int side = -1; side <= 1; side += 2)
            {
                Visuals.Box("Montant", root, new Vector3(side * ArchX, (TopY + ArchTop) / 2f, ArchZ), new Vector3(0.08f, ArchTop - TopY, 0.08f), Frame);
                var corner = Visuals.Box("Coin", root, new Vector3(side * (ArchX - 0.12f), ArchTop + 0.09f, ArchZ), new Vector3(0.08f, 0.34f, 0.08f), Frame);
                corner.transform.localRotation = Quaternion.Euler(0f, 0f, side * 45f);
                var lamp = Visuals.Primitive(PrimitiveType.Sphere);
                lamp.name = "Lampe";
                Visuals.Kill(lamp.GetComponent<Collider>());
                lamp.transform.SetParent(root, false);
                lamp.transform.localPosition = new Vector3(side * (ArchX - 0.22f), ArchTop + 0.14f, ArchZ - 0.06f);
                lamp.transform.localScale = Vector3.one * 0.07f;
                lamp.AddComponent<ColorTint>().Set(Lamp);
            }
            Visuals.Box("Haut", root, new Vector3(0f, ArchTop + 0.21f, ArchZ), new Vector3(2f * ArchX - 0.36f, 0.08f, 0.08f), Frame);
            Visuals.Box("Enseigne", root, new Vector3(0f, ArchTop + 0.46f, ArchZ), new Vector3(1.4f, 0.32f, 0.05f), Frame);
            Visuals.Box("Liseré", root, new Vector3(0f, ArchTop + 0.46f, ArchZ + 0.005f), new Vector3(1.46f, 0.38f, 0.04f), Gold);
            Visuals.Text(root, "BLACKJACK", new Vector3(0f, ArchTop + 0.46f, ArchZ - 0.035f), 0.22f, Gold, title: true);
        }

        // La console des gros boutons, accrochée au bord avant du comptoir, penchée vers le joueur.
        // Son haut reste au niveau du tapis : elle ne cache pas les cartes.
        void BuildConsole()
        {
            var console = new GameObject("Console").transform;
            console.SetParent(root, false);
            console.localPosition = new Vector3(0f, TopY - 0.05f, FrontZ - 0.07f);
            console.localRotation = Quaternion.Euler(35f, 0f, 0f);   // la face des boutons regarde vers le haut et vers le joueur
            Visuals.Box("Pupitre", console, new Vector3(0f, 0f, 0.035f), new Vector3(1.12f, 0.17f, 0.05f), Frame);
            var size = new Vector2(0.23f, 0.12f);
            CasinoButton.Create(console, "DOUBLER", new Vector3(-0.405f, 0f, 0f), size, Purple, table.Double, () => table.CanDouble, 0.045f);
            CasinoButton.Create(console, "RESTER", new Vector3(-0.135f, 0f, 0f), size, Yellow, table.Stand, () => table.CanAct, 0.045f);
            CasinoButton.Create(console, "CARTE", new Vector3(0.135f, 0f, 0f), size, Red, table.Hit, () => table.CanAct, 0.05f);
            CasinoButton.Create(console, "JOUER", new Vector3(0.405f, 0f, 0f), size, Green, table.Play, () => table.CanPlay, 0.05f);
        }

        // Le terminal de mise, sur son pied, à droite du joueur, penché et tourné vers lui
        void BuildTerminal()
        {
            Visuals.Solid("Pied du terminal", root, new Vector3(0.98f, 0.5f, FrontZ + 0.02f), new Vector3(0.07f, 1f, 0.07f), Frame);
            var t = new GameObject("Terminal").transform;
            t.SetParent(root, false);
            t.localPosition = new Vector3(0.98f, 1.1f, FrontZ - 0.02f);
            t.localRotation = Quaternion.Euler(35f, 30f, 0f);   // penché en arrière, et tourné vers le joueur (à sa gauche)
            Visuals.Box("Boîtier", t, new Vector3(0f, 0f, 0.03f), new Vector3(0.64f, 0.42f, 0.04f), Frame);

            // Le pavé : 1 à 9, puis C, 0, 000 (touches claires, chiffres foncés)
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "000" };
            var keySize = new Vector2(0.075f, 0.075f);
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                var pos = new Vector3(-0.25f + (i % 3) * 0.085f, 0.135f - (i / 3) * 0.088f, 0f);
                if (k == "C") CasinoButton.Create(t, k, pos, keySize, Red, table.ClearBet, () => table.CanType, 0.045f);
                else CasinoButton.Create(t, k, pos, keySize, Key, () => table.TypeDigits(k), () => table.CanType, k.Length > 1 ? 0.03f : 0.045f, KeyText);
            }

            // L'écran
            Visuals.Box("Écran", t, new Vector3(0.06f, 0f, 0f), new Vector3(0.21f, 0.36f, 0.01f), Screen);
            screenLimits = Visuals.Text(t, "", new Vector3(0.06f, 0.13f, -0.01f), 0.03f, Color.white);
            screenBet = Visuals.Text(t, "", new Vector3(0.06f, 0.02f, -0.01f), 0.1f, Digital, title: true);
            screenMessage = Visuals.Text(t, "", new Vector3(0.06f, -0.11f, -0.01f), 0.027f, Digital);

            // Les mises rapides
            string[] names = { "MIN", "1/4", "1/2", "MAX" };
            float[] shares = { 0f, 0.25f, 0.5f, 1f };
            for (int i = 0; i < names.Length; i++)
            {
                float share = shares[i];
                CasinoButton.Create(t, names[i], new Vector3(0.24f, 0.135f - i * 0.088f, 0f), new Vector2(0.1f, 0.072f), Orange,
                                       () => table.QuickBet(share), () => table.CanType, 0.038f, KeyText);
            }
        }

        // Une petite plaque noire debout sur le tapis, bordée d'or, avec un texte doré (les totaux)
        TextMesh Plaque(string name, Vector3 pos, Vector2 size)
        {
            Visuals.Box(name, root, pos, new Vector3(size.x + 0.02f, size.y + 0.02f, 0.015f), Gold);
            Visuals.Box(name + " (fond)", root, pos + new Vector3(0f, 0f, -0.004f), new Vector3(size.x, size.y, 0.015f), Frame);
            return Visuals.Text(root, "", pos + new Vector3(0f, 0f, -0.013f), size.y * 0.55f, Gold, title: true);
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
                    dealerCards[1] = BlackjackCard.Create(root, table.Dealer[1], false, CardPos(1, DealerZ)).gameObject;
                }
                shownHidden = table.DealerHidden;
            }
            SyncCards(dealerCards, table.Dealer, DealerZ, true);
            SyncCards(handCards, table.Hand, HandZ, false);

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

        // Les cartes d'une main, de gauche à droite à partir du milieu-gauche du tapis (une main de 6 cartes tient)
        static Vector3 CardPos(int i, float z) => new Vector3(-0.3f + i * CardStep, TopY + 0.012f + i * 0.001f, z);

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

            dealerTotal.text = table.Dealer.Count == 0 ? "CROUPIER" : table.DealerHidden ? "CROUPIER : ?" : $"CROUPIER : {BlackjackRules.Total(table.Dealer)}";
            handTotal.text = table.Hand.Count == 0 ? "TOI" : BlackjackRules.Total(table.Hand).ToString();
            screenLimits.text = $"Min : {BlackjackRules.MinBet}\nMax : {money}";
            screenBet.text = table.CanType ? table.Bet.ToString() : table.Played.ToString();
            screenMessage.text = table.Message.Length > 0 ? table.Message
                : table.Phase switch
                {
                    BlackjackPhase.Betting => "Tape ta mise\npuis JOUER",
                    BlackjackPhase.Playing => "CARTE, RESTER\nou DOUBLER",
                    BlackjackPhase.Dealer => "Le croupier\njoue...",
                    _ => "",
                };
        }
    }
}
