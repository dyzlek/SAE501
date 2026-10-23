using UnityEngine;

namespace SAE
{
    // Le stand de fléchettes À GAINS du casino : une cible de pub accrochée au mur, son ardoise des points et du barème,
    // et à 2 m, le comptoir de lancer avec 3 fléchettes (elles reviennent toutes seules dessus) et les boutons de mise
    // (10, 50, 100) et JOUER. La partie se paie, et rapporte selon le total des 3 fléchettes (voir DartBoard).
    // Le geste est celui du vrai jeu : on prend la fléchette et on la LANCE (en VR, la vitesse de la main).
    // Construit par le code au lancement (posé par le générateur dans la scène Casino).
    // Repère local : +Z = vers le mur, le joueur est côté -Z ; l'origine est au pied du mur.
    public class DartStand : MonoBehaviour
    {
        const float BoardY = 1.73f;      // m : le centre de la cible, à la hauteur réglementaire
        const float ThrowLine = 2.0f;    // m entre le mur et le comptoir de lancer
        static readonly int[] Bets = { 10, 50, 100 };

        static readonly Color Cork = new Color(0.72f, 0.58f, 0.4f);
        static readonly Color Black = new Color(0.1f, 0.1f, 0.12f);
        static readonly Color Cream = new Color(0.93f, 0.88f, 0.74f);
        static readonly Color Green = new Color(0.15f, 0.55f, 0.3f);
        static readonly Color Red = new Color(0.8f, 0.12f, 0.12f);
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color DarkWood = new Color(0.30f, 0.19f, 0.10f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.15f);
        static readonly Color Chalk = new Color(0.95f, 0.94f, 0.88f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Orange = new Color(1f, 0.6f, 0.1f);
        static readonly Color PlayGreen = new Color(0.35f, 0.75f, 0.25f);
        static readonly Color KeyText = new Color(0.12f, 0.12f, 0.14f);

        DartBoard board;
        int bet = 10;
        TextMesh betText;

        void Awake()
        {
            BuildTarget();
            BuildCounter();
        }

        // La cible : un panneau de liège, puis les anneaux (5, 10, 25 points) et le mille rouge (50), aux rayons de DartBoard
        void BuildTarget()
        {
            var t = transform;
            Visuals.Box("Panneau", t, new Vector3(0f, BoardY, 0.03f), new Vector3(0.9f, 0.9f, 0.04f), DarkWood);
            Disc("Liège", new Vector3(0f, BoardY, -0.0f), 0.62f, Cork);
            Disc("5 points", new Vector3(0f, BoardY, -0.01f), 0.48f, Black);
            Disc("10 points", new Vector3(0f, BoardY, -0.015f), 0.26f, Cream);
            Disc("25 points", new Vector3(0f, BoardY, -0.02f), 0.12f, Green);
            Disc("Mille", new Vector3(0f, BoardY, -0.025f), 0.05f, Red);

            // Le collider plat devant la cible : c'est lui que les fléchettes touchent
            var target = new GameObject("Cible (jeu)").transform;
            target.SetParent(t, false);
            target.localPosition = new Vector3(0f, BoardY, -0.03f);
            var col = target.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.5f, 0.5f, 0.04f);
            col.center = new Vector3(0f, 0f, 0.02f);
            board = target.gameObject.AddComponent<DartBoard>();
            board.paid = true;

            // L'ardoise des points et du barème, à côté de la cible, et l'enseigne au-dessus
            Visuals.Box("Cadre", t, new Vector3(0.95f, BoardY, 0.02f), new Vector3(0.86f, 0.62f, 0.04f), DarkWood);
            Visuals.Box("Ardoise", t, new Vector3(0.95f, BoardY, 0f), new Vector3(0.8f, 0.56f, 0.02f), Slate);
            board.label = Visuals.Text(t, "", new Vector3(0.95f, BoardY, -0.02f), 0.05f, Chalk);
            Visuals.Box("Enseigne", t, new Vector3(0.45f, BoardY + 0.62f, 0.02f), new Vector3(1.9f, 0.28f, 0.05f), Black);
            Visuals.Box("Liseré", t, new Vector3(0.45f, BoardY + 0.62f, 0.025f), new Vector3(1.96f, 0.34f, 0.04f), Gold);
            Visuals.Text(t, "FLÉCHETTES", new Vector3(0.45f, BoardY + 0.62f, -0.01f), 0.19f, Gold, title: true);
        }

        // Le comptoir de lancer : les 3 fléchettes posées dessus, les mises et JOUER
        void BuildCounter()
        {
            var t = transform;
            const float CounterY = 1.0f;
            Visuals.Solid("Comptoir de lancer", t, new Vector3(0f, CounterY / 2f, -ThrowLine), new Vector3(1.2f, CounterY, 0.35f), Wood);
            Visuals.Box("Plateau", t, new Vector3(0f, CounterY + 0.01f, -ThrowLine), new Vector3(1.26f, 0.02f, 0.4f), DarkWood);
            Visuals.Box("Ligne de lancer", t, new Vector3(0f, 0.005f, -ThrowLine - 0.25f), new Vector3(1.4f, 0.01f, 0.05f), Gold);
            for (int i = 0; i < 3; i++)
            {
                var pos = t.TransformPoint(new Vector3(-0.42f + i * 0.12f, CounterY + 0.04f, -ThrowLine));
                Dart.Create(t, pos, t.rotation);   // pointe vers +Z : vers la cible
            }

            // Les boutons, sur un petit pupitre penché au bord du comptoir
            var c = new GameObject("Pupitre").transform;
            c.SetParent(t, false);
            c.localPosition = new Vector3(0.25f, CounterY + 0.06f, -ThrowLine - 0.12f);
            c.localRotation = Quaternion.Euler(40f, 0f, 0f);
            Visuals.Box("Socle", c, new Vector3(0.05f, 0f, 0.035f), new Vector3(0.62f, 0.16f, 0.05f), Black);
            for (int i = 0; i < Bets.Length; i++)
            {
                int amount = Bets[i];
                CasinoButton.Create(c, amount.ToString(), new Vector3(-0.18f + i * 0.12f, 0.02f, 0f), new Vector2(0.1f, 0.08f), Orange,
                                    () => Choose(amount), () => !board.Playing, 0.04f, KeyText);
            }
            CasinoButton.Create(c, "JOUER", new Vector3(0.24f, 0.01f, 0f), new Vector2(0.18f, 0.11f), PlayGreen,
                                () => board.StartPaidRound(bet), () => !board.Playing && bet <= Economy.Money, 0.045f);
            betText = Visuals.Text(c, "", new Vector3(-0.06f, -0.06f, -0.03f), 0.03f, Gold, title: true);
            ShowBet();
        }

        bool Choose(int amount)
        {
            if (board.Playing) return false;
            bet = amount;
            ShowBet();
            return true;
        }

        void ShowBet() => betText.text = $"mise : {bet}";

        // Un disque de la cible, debout face au joueur (un cylindre aplati, tourné de 90°)
        void Disc(string name, Vector3 pos, float diameter, Color color)
        {
            var go = Visuals.Primitive(PrimitiveType.Cylinder);
            go.name = name;
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(diameter, 0.005f, diameter);
            go.AddComponent<ColorTint>().Set(color);
        }
    }
}
