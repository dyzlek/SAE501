using System.Collections;
using UnityEngine;

namespace SAE
{
    // La course des singes du casino (comme la course de canards de « Gamble With Your Friends ») :
    //   1. on choisit son singe (5 couloirs, chacun avec sa cote : ×2 pour le favori… ×8 pour l'outsider) ;
    //   2. on mise (+10, +100, MAX, RETIRER), puis DÉPART : la mise est payée ;
    //   3. la course dure une douzaine de secondes, avec des accélérations au hasard. On ENCOURAGE son singe :
    //      au casque on secoue une manette (geste VR), au PC on tape sur ALLEZ ! ; à chaque fois, il accélère un peu ;
    //   4. le premier qui passe la ligne gagne : si c'est le nôtre, on touche mise × cote.
    // Les cotes changent à chaque course (le favori n'est pas toujours le même couloir).
    // Construite par le code au lancement (posée par le générateur dans la scène Casino). Repère local : la piste est en
    // long sur X (départ à gauche, arrivée à droite), le joueur est côté -Z, devant le pupitre des paris.
    // Chacun joue sa course chez lui (en coop aussi) : la mise et le gain passent par la bourse commune (Economy).
    public class MonkeyRace : MonoBehaviour
    {
        const int Lanes = 5;
        const float TopY = 0.9f;                     // m : le dessus de la piste
        const float StartX = -2.3f, FinishX = 2.3f;  // m : les lignes de départ et d'arrivée
        const float LaneStep = 0.27f;                // m entre deux couloirs
        const float BaseSpeed = 0.36f;               // m/s : ~13 s de course
        const float CheerBoost = 0.22f;              // m/s en plus pendant un encouragement
        const float CheerTime = 0.35f;               // s que dure un encouragement
        const float ShakeSpeed = 2.2f;               // m/s : une manette plus rapide que ça, c'est qu'on la secoue
        const float CheerCooldown = 0.18f;           // s entre deux encouragements comptés
        const float ResultsTime = 3f;                // s avant de pouvoir rejouer

        static readonly int[] OddsSet = { 2, 3, 4, 5, 8 };
        static readonly MonkeyType[] Runners = { MonkeyType.Classique, MonkeyType.Boomerang, MonkeyType.Sniper, MonkeyType.Glace, MonkeyType.Colle };
        static readonly Color[] LaneColors =
        {
            new Color(0.9f, 0.25f, 0.2f), new Color(0.25f, 0.55f, 0.95f), new Color(0.35f, 0.8f, 0.3f),
            new Color(0.95f, 0.8f, 0.2f), new Color(0.7f, 0.35f, 0.85f),
        };
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color DarkWood = new Color(0.30f, 0.19f, 0.10f);
        static readonly Color Track = new Color(0.55f, 0.42f, 0.28f);
        static readonly Color Frame = new Color(0.07f, 0.07f, 0.09f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.15f);
        static readonly Color Chalk = new Color(0.95f, 0.94f, 0.88f);
        static readonly Color Green = new Color(0.35f, 0.75f, 0.25f);
        static readonly Color Red = new Color(0.88f, 0.18f, 0.15f);
        static readonly Color Orange = new Color(1f, 0.6f, 0.1f);
        static readonly Color KeyText = new Color(0.12f, 0.12f, 0.14f);

        enum Phase { Betting, Racing, Results }

        Phase phase;
        int chosen = -1;               // le couloir de notre singe (-1 : pas encore choisi)
        int stake = 10, played;
        int winner = -1;
        readonly int[] odds = new int[Lanes];
        readonly float[] strength = new float[Lanes];   // le favori est un peu plus fort
        readonly float[] surge = new float[Lanes];      // l'accélération du moment (change au hasard)
        readonly float[] nextSurge = new float[Lanes];
        readonly Transform[] runners = new Transform[Lanes];
        readonly TextMesh[] laneLabels = new TextMesh[Lanes];
        float cheerUntil, nextCheer, raceTime;
        Vector3 lastLeft, lastRight;
        TextMesh board, stakeText;
        string message = "";

        bool CanBet => phase != Phase.Racing;
        bool CanStart => CanBet && chosen >= 0 && stake > 0 && stake <= Economy.Money;

        void Awake()
        {
            Build();
            NewOdds();
            Refresh();
        }

        // --- Les paris ---

        bool Choose(int lane)
        {
            if (!CanBet) return false;
            chosen = lane;
            message = "";
            Refresh();
            return true;
        }

        bool Bet(int amount)
        {
            if (!CanBet) return false;
            stake = amount < 0 ? Economy.Money : amount == 0 ? 0 : Mathf.Min(stake + amount, Mathf.Max(Economy.Money, 0));
            message = "";
            Refresh();
            return true;
        }

        bool StartRace()
        {
            if (!CanStart)
            {
                message = chosen < 0 ? "Choisis d'abord ton singe !" : "Mise d'abord des bananes !";
                Refresh();
                return false;
            }
            if (!Economy.TrySpend(stake, transform.position)) return false;
            played = stake;
            StartCoroutine(Race());
            return true;
        }

        // ALLEZ ! (au PC, ou en tapant dessus au casque)
        bool Cheer()
        {
            if (phase != Phase.Racing) return false;
            Boost();
            return true;
        }

        // --- La course ---

        IEnumerator Race()
        {
            phase = Phase.Racing;
            winner = -1;
            raceTime = 0f;
            message = "Secoue ta manette (ou ALLEZ !) pour encourager ton singe !";
            for (int i = 0; i < Lanes; i++) { SetRunner(i, StartX, 0f); nextSurge[i] = 0f; }
            if (PlayerRig.Local) { lastLeft = PlayerRig.Local.leftHand.position; lastRight = PlayerRig.Local.rightHand.position; }
            Sounds.Play(Sounds.BossAnnonce, transform.position, 0.6f, 1.3f);   // le « top départ »
            Refresh();

            while (winner < 0)
            {
                raceTime += Time.deltaTime;
                WatchShake();
                for (int i = 0; i < Lanes; i++)
                {
                    if (raceTime >= nextSurge[i])
                    {
                        surge[i] = Random.Range(0.55f, 1.45f);           // il accélère ou ralentit, au hasard
                        nextSurge[i] = raceTime + Random.Range(0.4f, 0.9f);
                    }
                    float speed = BaseSpeed * strength[i] * surge[i];
                    if (i == chosen && Time.time < cheerUntil) speed += CheerBoost;
                    float x = runners[i].localPosition.x + speed * Time.deltaTime;
                    SetRunner(i, x, speed);
                    if (x >= FinishX && winner < 0) winner = i;
                }
                yield return null;
            }

            // Le résultat : notre singe a gagné ? mise × cote
            phase = Phase.Results;
            bool won = winner == chosen;
            int gain = won ? played * odds[winner] : 0;
            if (won)
            {
                Economy.Earn(gain, runners[winner].position + Vector3.up * 0.3f, stakeBack: played);
                Sounds.Play(Sounds.VagueGagnee, transform.position);
            }
            else Sounds.Play(Sounds.ViePerdue, transform.position, 0.8f, 0.9f);
            message = won ? $"TON SINGE GAGNE ! +{gain}" : $"Le singe n°{winner + 1} gagne... perdu !";
            Refresh();

            yield return new WaitForSeconds(ResultsTime);
            for (int i = 0; i < Lanes; i++) SetRunner(i, StartX, 0f);
            phase = Phase.Betting;
            message = "";
            NewOdds();
            Refresh();
        }

        // Au casque : une manette qui bouge vite = on la secoue = un encouragement (pas plus d'un toutes les 0,18 s)
        void WatchShake()
        {
            var rig = PlayerRig.Local;
            if (!rig || !rig.IsVR || Time.deltaTime <= 0f) return;
            var left = rig.leftHand.position;
            var right = rig.rightHand.position;
            float fastest = Mathf.Max((left - lastLeft).magnitude, (right - lastRight).magnitude) / Time.deltaTime;
            lastLeft = left;
            lastRight = right;
            if (fastest > ShakeSpeed && Time.time >= nextCheer) Boost();
        }

        void Boost()
        {
            nextCheer = Time.time + CheerCooldown;
            cheerUntil = Time.time + CheerTime;
            if (chosen >= 0) AudioSource.PlayClipAtPoint(Sounds.Tick, runners[chosen].position, 0.6f);
        }

        // Les cotes de la course : ×2, ×3, ×4, ×5, ×8 mélangées entre les couloirs ; plus la cote est basse, plus le singe est fort
        void NewOdds()
        {
            var shuffled = (int[])OddsSet.Clone();
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            for (int i = 0; i < Lanes; i++)
            {
                odds[i] = shuffled[i];
                strength[i] = 1.1f - 0.025f * odds[i];   // ×2 → 1,05 ; ×8 → 0,9
            }
        }

        // Un singe avance : il sautille d'autant plus qu'il va vite
        void SetRunner(int lane, float x, float speed)
        {
            float hop = Mathf.Abs(Mathf.Sin(raceTime * 14f + lane)) * Mathf.Clamp01(speed / BaseSpeed) * 0.05f;
            runners[lane].localPosition = new Vector3(x, TopY + 0.14f + hop, LaneZ(lane));
        }

        static float LaneZ(int lane) => 0.25f + lane * LaneStep;

        // --- La piste, le pupitre et l'ardoise ---

        void Build()
        {
            var t = transform;
            Visuals.Solid("Piste", t, new Vector3(0f, TopY / 2f, 0.8f), new Vector3(5.3f, TopY, 1.6f), Wood);
            Visuals.Box("Rebord", t, new Vector3(0f, TopY + 0.04f, 0.0f), new Vector3(5.3f, 0.08f, 0.05f), DarkWood);
            Visuals.Box("Rebord", t, new Vector3(0f, TopY + 0.04f, 1.6f), new Vector3(5.3f, 0.08f, 0.05f), DarkWood);
            for (int i = 0; i < Lanes; i++)
            {
                Visuals.Box($"Couloir {i + 1}", t, new Vector3(0f, TopY + 0.005f, LaneZ(i)), new Vector3(5.2f, 0.01f, LaneStep - 0.03f), i % 2 == 0 ? Track : Track * 0.85f);
                Visuals.Box("Dossard", t, new Vector3(StartX - 0.25f, TopY + 0.012f, LaneZ(i)), new Vector3(0.16f, 0.004f, LaneStep - 0.06f), LaneColors[i]);
                laneLabels[i] = Visuals.Text(t, "", new Vector3(StartX - 0.25f, TopY + 0.016f, LaneZ(i)), 0.06f, Color.white, title: true);
                laneLabels[i].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                runners[i] = Runner(i);
            }
            // La ligne d'arrivée en damier
            for (int k = 0; k < 10; k++)
                Visuals.Box("Damier", t, new Vector3(FinishX + (k % 2) * 0.06f, TopY + 0.013f, 0.12f + k * 0.14f), new Vector3(0.06f, 0.004f, 0.14f), k % 2 == 0 ? Color.white : Frame);

            BuildConsole();
            BuildBoard();
        }

        // Un coureur : le modèle du singe, tourné vers l'arrivée (+X), sur un petit disque de la couleur de son couloir
        Transform Runner(int lane)
        {
            var root = new GameObject($"Singe {lane + 1}").transform;
            root.SetParent(transform, false);
            var asset = MonkeyVisuals.Model(Runners[lane], out float yaw);
            if (asset)
            {
                var model = Visuals.FitModel(asset, yaw, root, 0.26f, out _);
                Visuals.RestPose(model.transform, root);
            }
            else Visuals.Box("Singe", root, Vector3.zero, Vector3.one * 0.2f, LaneColors[lane]);
            root.localRotation = Quaternion.Euler(0f, -90f, 0f);   // le modèle regarde vers -Z : on le tourne vers +X
            var disc = Visuals.Primitive(PrimitiveType.Cylinder);
            disc.name = "Socle";
            Visuals.Kill(disc.GetComponent<Collider>());
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = new Vector3(0f, -0.13f, 0f);
            disc.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
            disc.AddComponent<ColorTint>().Set(LaneColors[lane]);
            root.localPosition = new Vector3(StartX, TopY + 0.14f, LaneZ(lane));
            return root;
        }

        // Le pupitre des paris, devant la piste, penché vers le joueur : 5 singes, les mises, DÉPART et ALLEZ !
        void BuildConsole()
        {
            var c = new GameObject("Pupitre des paris").transform;
            c.SetParent(transform, false);
            c.localPosition = new Vector3(0f, TopY - 0.05f, -0.1f);
            c.localRotation = Quaternion.Euler(35f, 0f, 0f);
            Visuals.Box("Pupitre", c, new Vector3(0f, 0f, 0.035f), new Vector3(2.75f, 0.3f, 0.05f), Frame);
            // À gauche, les 5 singes ; au milieu, les mises ; à droite, DÉPART et ALLEZ ! (le pupitre tient dans les bras écartés)
            for (int i = 0; i < Lanes; i++)
            {
                int lane = i;
                CasinoButton.Create(c, $"N°{i + 1}", new Vector3(-1.2f + i * 0.21f, 0.04f, 0f), new Vector2(0.18f, 0.14f), LaneColors[i],
                                    () => Choose(lane), () => CanBet, 0.045f);
            }
            CasinoButton.Create(c, "+10", new Vector3(-0.08f, 0.05f, 0f), new Vector2(0.17f, 0.11f), Orange, () => Bet(10), () => CanBet, 0.042f, KeyText);
            CasinoButton.Create(c, "+100", new Vector3(0.11f, 0.05f, 0f), new Vector2(0.17f, 0.11f), Orange, () => Bet(100), () => CanBet, 0.042f, KeyText);
            CasinoButton.Create(c, "MAX", new Vector3(0.30f, 0.05f, 0f), new Vector2(0.17f, 0.11f), Orange, () => Bet(-1), () => CanBet, 0.042f, KeyText);
            CasinoButton.Create(c, "RETIRER", new Vector3(0.52f, 0.05f, 0f), new Vector2(0.23f, 0.11f), Red, () => Bet(0), () => CanBet, 0.036f);
            stakeText = Visuals.Text(c, "", new Vector3(0.22f, -0.09f, -0.03f), 0.06f, Gold, title: true);
            CasinoButton.Create(c, "DÉPART", new Vector3(0.84f, 0f, 0f), new Vector2(0.34f, 0.22f), Green, StartRace, () => CanStart, 0.065f);
            CasinoButton.Create(c, "ALLEZ !", new Vector3(1.18f, 0f, 0f), new Vector2(0.3f, 0.22f), Orange, Cheer, () => phase == Phase.Racing, 0.06f, KeyText);
        }

        // L'ardoise et l'enseigne, derrière la piste
        void BuildBoard()
        {
            var t = transform;
            const float BoardY = 1.75f, BoardZ = 1.75f;
            Visuals.Box("Cadre", t, new Vector3(0f, BoardY, BoardZ + 0.02f), new Vector3(2.9f, 0.82f, 0.04f), DarkWood);
            Visuals.Box("Ardoise", t, new Vector3(0f, BoardY, BoardZ), new Vector3(2.8f, 0.72f, 0.02f), Slate);
            board = Visuals.Text(t, "", new Vector3(0f, BoardY, BoardZ - 0.02f), 0.075f, Chalk);
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Poteau", t, new Vector3(side * 1.5f, (BoardY + 0.7f) / 2f, BoardZ + 0.03f), new Vector3(0.08f, BoardY + 0.7f, 0.08f), DarkWood);
            Visuals.Box("Enseigne", t, new Vector3(0f, BoardY + 0.62f, BoardZ), new Vector3(2.4f, 0.3f, 0.05f), Frame);
            Visuals.Box("Liseré", t, new Vector3(0f, BoardY + 0.62f, BoardZ + 0.005f), new Vector3(2.46f, 0.36f, 0.04f), Gold);
            Visuals.Text(t, "COURSE DES SINGES", new Vector3(0f, BoardY + 0.62f, BoardZ - 0.035f), 0.2f, Gold, title: true);
        }

        // Les cotes sur les dossards, la mise, et l'ardoise
        void Refresh()
        {
            for (int i = 0; i < Lanes; i++)
                laneLabels[i].text = i == chosen ? $"<color=#FFD45A>{i + 1}</color>" : (i + 1).ToString();
            stakeText.text = $"mise {stake}";

            var text = new System.Text.StringBuilder();
            for (int i = 0; i < Lanes; i++)
            {
                string name = $"<color=#{ColorUtility.ToHtmlStringRGB(LaneColors[i])}>N°{i + 1}</color> ×{odds[i]}";
                text.Append(i == chosen ? $"[{name}]" : name).Append(i < Lanes - 1 ? "   " : "\n");
            }
            text.Append(message.Length > 0 ? message
                : phase == Phase.Betting ? "<color=#FFD45A>> Choisis ton singe, mise, puis DÉPART</color>" : "");
            board.text = text.ToString();
        }
    }
}
