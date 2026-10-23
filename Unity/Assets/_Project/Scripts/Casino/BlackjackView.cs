using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace SAE
{
    // Ce qu'on voit de la table de blackjack (BlackjackTable fait les règles). Construit par le code au lancement,
    // comme le panneau du classement. Repère local : +Z = vers le croupier, les joueurs sont côté -Z.
    //   - une table en demi-lune au tapis vert, à hauteur de plan de travail (0,85 m) : tout est à portée de main ;
    //   - le croupier (un singe) derrière, son sabot de cartes, et l'ardoise des règles au-dessus de lui ;
    //   - 4 places au bord de la table, une par joueur (la couleur de son avatar), chacune avec 3 boutons debout sur le
    //     bord : +10 / +100 / RETIRER pendant les mises, puis CARTE / RESTER pendant son tour ;
    //   - DISTRIBUER au milieu du tapis, pour toute la table.
    // Chaque image, on compare la table (BlackjackTable.State) à ce qui est dessiné : une carte en plus glisse depuis le sabot
    // (avec un « tic »), un résultat joue son son. Ça marche pareil chez l'hôte et chez l'invité, qui reçoit la table toute faite.
    public class BlackjackView : MonoBehaviour
    {
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color DarkWood = new Color(0.30f, 0.19f, 0.10f);
        static readonly Color Felt = new Color(0.1f, 0.45f, 0.25f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.15f);
        static readonly Color Chalk = new Color(0.95f, 0.94f, 0.88f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Play = new Color(0.3f, 0.65f, 0.3f);
        static readonly Color Stop = new Color(0.75f, 0.3f, 0.2f);
        static readonly Color[] SeatColors =   // les mêmes que les avatars du coop (CoopAvatar)
        {
            new Color(1f, 0.8f, 0.3f), new Color(0.3f, 0.6f, 1f), new Color(0.4f, 0.85f, 0.4f), new Color(1f, 0.45f, 0.75f),
        };

        const float TableY = 0.85f, TableW = 2.6f, TableD = 1.0f;   // m : la table
        const float SeatStep = 0.64f;                                // m entre deux places
        const float CardStep = 0.045f;                              // m : les cartes d'une main se chevauchent en éventail
        const float SlideTime = 0.3f;                                // s : une carte glisse du sabot à sa place

        BlackjackTable table;
        Transform root;
        Vector3 shoe;                                                // le sabot, d'où partent les cartes
        readonly List<GameObject> dealerCards = new List<GameObject>();
        readonly List<GameObject>[] seatCards = new List<GameObject>[BlackjackState.Seats];
        readonly TextMesh[] seatTexts = new TextMesh[BlackjackState.Seats];
        readonly TextMesh[,] buttonTexts = new TextMesh[BlackjackState.Seats, 3];
        readonly GameObject[] clearButtons = new GameObject[BlackjackState.Seats];
        GameObject dealButton;
        TextMesh board, dealerText;
        BlackjackState shown;                                        // la table telle qu'elle est dessinée
        bool shownHidden = true;

        public static BlackjackView Build(BlackjackTable table)
        {
            var view = table.gameObject.AddComponent<BlackjackView>();
            view.table = table;
            view.root = table.transform;
            view.BuildFurniture();
            return view;
        }

        // Le milieu d'une place, au bord de la table (pour les sons et les « +40 » de la bourse)
        public Vector3 SeatPosition(int seat) => root.TransformPoint(SeatX(seat), TableY, -0.3f);

        static float SeatX(int seat) => (seat - (BlackjackState.Seats - 1) / 2f) * SeatStep;

        void BuildFurniture()
        {
            Visuals.Solid("Table de blackjack", root, new Vector3(0f, TableY / 2f, 0f), new Vector3(TableW, TableY, TableD), Wood);
            Visuals.Box("Tapis", root, new Vector3(0f, TableY + 0.005f, 0f), new Vector3(TableW - 0.06f, 0.01f, TableD - 0.06f), Felt);
            Visuals.Box("Rebord", root, new Vector3(0f, TableY + 0.02f, -TableD / 2f + 0.02f), new Vector3(TableW, 0.04f, 0.04f), DarkWood);

            // Le croupier, debout derrière la table, et son sabot à sa droite (vue du joueur : à gauche)
            var dealer = Visuals.MonkeyPiece(new Monkey(MonkeyType.Classique, Rarity.Gris), root, new Vector3(0f, TableY + 0.3f, TableD / 2f + 0.25f), 0.6f, withLabel: false);
            dealer.name = "Croupier";
            shoe = new Vector3(-0.7f, TableY + 0.06f, 0.3f);
            Visuals.Box("Sabot", root, shoe, new Vector3(0.12f, 0.1f, 0.18f), DarkWood);
            dealerText = FlatText(new Vector3(0f, TableY + 0.012f, 0.05f), 0.05f, Chalk);

            // Les 4 places
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                seatCards[i] = new List<GameObject>();
                float x = SeatX(i);
                Visuals.Box($"Place {i + 1}", root, new Vector3(x, TableY + 0.011f, -0.22f), new Vector3(0.5f, 0.002f, 0.3f), SeatColors[i] * 0.55f);
                seatTexts[i] = FlatText(new Vector3(x, TableY + 0.014f, -0.4f), 0.035f, Color.white);
                for (int b = 0; b < 3; b++)
                {
                    int seat = i, slot = b;
                    var holder = new GameObject($"Bouton {b + 1}").transform;
                    holder.SetParent(root, false);
                    var pos = new Vector3(x + (b - 1) * 0.165f, TableY + 0.07f, -TableD / 2f - 0.03f);
                    buttonTexts[i, b] = WorldButton.Create(holder, "", pos, new Vector2(0.15f, 0.08f), b == 2 ? Stop : Play, () => Pressed(seat, slot), 0.035f);
                    if (b == 2) clearButtons[i] = holder.gameObject;
                }
            }

            // DISTRIBUER, au milieu du tapis
            var deal = new GameObject("Distribuer").transform;
            deal.SetParent(root, false);
            WorldButton.Create(deal, "DISTRIBUER", new Vector3(0f, TableY + 0.07f, -0.02f), new Vector2(0.36f, 0.09f), Gold * 0.8f, () => table.Press(BlackjackTable.LocalSeat, BlackjackOp.Deal), 0.04f);
            dealButton = deal.gameObject;

            // L'ardoise des règles, derrière le croupier, et l'enseigne
            const float BoardY = 1.95f, BoardZ = TableD / 2f + 0.55f, BoardW = 1.9f, BoardH = 0.62f;
            Visuals.Box("Cadre", root, new Vector3(0f, BoardY, BoardZ + 0.02f), new Vector3(BoardW + 0.08f, BoardH + 0.08f, 0.04f), DarkWood);
            Visuals.Box("Ardoise", root, new Vector3(0f, BoardY, BoardZ), new Vector3(BoardW, BoardH, 0.02f), Slate);
            board = Visuals.Text(root, "", new Vector3(0f, BoardY, BoardZ - 0.02f), 0.045f, Chalk);
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Poteau", root, new Vector3(side * (BoardW / 2f + 0.07f), (BoardY + 0.4f) / 2f, BoardZ + 0.03f), new Vector3(0.06f, BoardY + 0.4f, 0.06f), Wood);
            Visuals.Box("Enseigne", root, new Vector3(0f, BoardY + 0.45f, BoardZ), new Vector3(BoardW + 0.2f, 0.2f, 0.05f), DarkWood);
            Visuals.Text(root, "BLACKJACK", new Vector3(0f, BoardY + 0.45f, BoardZ - 0.03f), 0.13f, Gold, title: true);
        }

        // Un texte posé à plat sur le tapis, lisible depuis la place du joueur
        TextMesh FlatText(Vector3 pos, float height, Color color)
        {
            var text = Visuals.Text(root, "", pos, height, color);
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return text;
        }

        // Le bouton 'slot' d'une place : son rôle dépend de la phase (miser, ou jouer)
        void Pressed(int seat, int slot)
        {
            bool betting = table.State.phase == BlackjackPhase.Betting;
            var op = slot switch
            {
                0 => betting ? BlackjackOp.AddTen : BlackjackOp.Hit,
                1 => betting ? BlackjackOp.AddHundred : BlackjackOp.Stand,
                _ => BlackjackOp.Clear,
            };
            table.Press(seat, op);
        }

        // --- Chaque image : dessiner ce qui a changé ---

        void Update()
        {
            var state = table.State;
            if (state.Equals(shown) && state.dealerHidden == shownHidden) return;

            // Le croupier : sa carte cachée se retourne quand il joue
            if (state.dealerHidden != shownHidden && dealerCards.Count > 1)
            {
                Destroy(dealerCards[1]);
                dealerCards[1] = BlackjackCard.Create(root, state.dealer[1], false, DealerCardPos(1)).gameObject;
            }
            SyncCards(dealerCards, state.dealer, DealerCardPos, i => i == 1 && state.dealerHidden);

            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                int seat = i;
                var s = state[i];
                SyncCards(seatCards[i], s.cards, c => SeatCardPos(seat, c), _ => false);
                if (s.status != shown[i].status) PlayResult(s.status, shown[i].status, i);
            }

            shown = state;
            shownHidden = state.dealerHidden;
            RefreshTexts(state);
        }

        // Ajoute les cartes qui manquent (elles glissent depuis le sabot), ou range tout si la main a été vidée
        void SyncCards(List<GameObject> drawn, FixedList32Bytes<byte> cards, System.Func<int, Vector3> place, System.Func<int, bool> faceDown)
        {
            if (cards.Length < drawn.Count)
            {
                foreach (var c in drawn) Destroy(c);
                drawn.Clear();
            }
            for (int i = drawn.Count; i < cards.Length; i++)
            {
                var card = BlackjackCard.Create(root, cards[i], faceDown(i), place(i));
                card.SlideFrom(shoe, SlideTime);
                drawn.Add(card.gameObject);
                AudioSource.PlayClipAtPoint(Sounds.Tick, card.transform.position, 0.7f);
            }
        }

        static Vector3 DealerCardPos(int i) => new Vector3(-0.1f + i * (CardStep + 0.03f), TableY + 0.012f + i * 0.001f, 0.25f);
        static Vector3 SeatCardPos(int seat, int i) => new Vector3(SeatX(seat) - 0.1f + i * CardStep, TableY + 0.014f + i * 0.001f, -0.2f + i * 0.005f);

        void PlayResult(SeatStatus status, SeatStatus before, int seat)
        {
            if (status == SeatStatus.Lost && before == SeatStatus.Bust) return;   // le son de la main sautée a déjà été joué
            var at = SeatPosition(seat);
            switch (status)
            {
                case SeatStatus.Blackjack: Sounds.Play(Sounds.VagueGagnee, at); break;
                case SeatStatus.Won: Sounds.Play(Sounds.SingeObtenu, at); break;
                case SeatStatus.Push: Sounds.Play(Sounds.Bouton, at); break;
                case SeatStatus.Bust:
                case SeatStatus.Lost: Sounds.Play(Sounds.ViePerdue, at, 0.8f, 0.9f); break;
            }
        }

        void RefreshTexts(BlackjackState state)
        {
            bool betting = state.phase == BlackjackPhase.Betting;
            dealButton.SetActive(betting);
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                buttonTexts[i, 0].text = betting ? "+10" : "CARTE";
                buttonTexts[i, 1].text = betting ? "+100" : "RESTER";
                buttonTexts[i, 2].text = "RETIRER";
                clearButtons[i].SetActive(betting);
                seatTexts[i].text = SeatLine(i, state[i]);
            }
            dealerText.text = state.dealer.Length == 0 ? "" : state.dealerHidden ? "Croupier : ?" : $"Croupier : {BlackjackRules.Total(state.dealer)}";

            var text = new StringBuilder();
            text.Append("<size=36>Le croupier tire jusqu'à 16, reste à 17 · gagné ×2 · blackjack ×2,5</size>\n");
            text.Append(state.phase switch
            {
                BlackjackPhase.Betting => "<color=#FFD45A>> Misez à votre place, puis DISTRIBUER</color>",
                BlackjackPhase.Dealing => "Le croupier distribue...",
                BlackjackPhase.Playing => "<color=#FFD45A>> CARTE pour tirer, RESTER pour s'arrêter (21 au plus !)</color>",
                BlackjackPhase.Dealer => "Le croupier joue...",
                _ => "Résultats !",
            });
            board.text = text.ToString();
        }

        // « JOUEUR 2 · mise 20 · 17 » puis le résultat ; « TOI » à sa propre place
        static string SeatLine(int seat, BlackjackSeat s)
        {
            string who = seat == BlackjackTable.LocalSeat ? "TOI" : $"JOUEUR {seat + 1}";
            string line = $"{who} · mise {s.bet}";
            if (s.cards.Length > 0) line += $" · {BlackjackRules.Total(s.cards)}";
            return s.status switch
            {
                SeatStatus.Bust => line + "\n<color=#FF7A6A>SAUTÉ</color>",
                SeatStatus.Stood => line + "\nreste",
                SeatStatus.Blackjack => line + "\n<color=#FFD45A>BLACKJACK !</color>",
                SeatStatus.Won => line + "\n<color=#8CE08C>GAGNÉ</color>",
                SeatStatus.Lost => line + "\n<color=#FF7A6A>PERDU</color>",
                SeatStatus.Push => line + "\négalité",
                _ => line,
            };
        }
    }
}
