using System;
using System.Collections.Generic;

namespace SAE
{
    // Les sortes de ballons.
    // Normal : perd une couche par point de dégât. Rapide : petit et vif. Blindé : gris, prend moitié moins
    // de dégâts et ne peut pas être ralenti. Boss : gros, lent, très solide. Dirigeable : le boss final rouge.
    public enum BalloonKind { Normal, Rapide, Blinde, Boss, Dirigeable }

    // Un groupe de ballons identiques envoyés les uns après les autres.
    [Serializable]
    public class BalloonGroup
    {
        public BalloonKind kind;
        public int count = 5;
        public int layers = 1;
        public float interval = 0.7f;      // secondes entre deux ballons du groupe
        public float pause = 1f;           // secondes d'attente avant le groupe

        public BalloonGroup(BalloonKind kind, int count, int layers, float interval = 0.7f, float pause = 1f)
        {
            this.kind = kind; this.count = count; this.layers = layers; this.interval = interval; this.pause = pause;
        }
    }

    // Une vague = une suite de groupes, joués dans l'ordre.
    [Serializable]
    public class WaveData
    {
        public List<BalloonGroup> groups = new List<BalloonGroup>();

        public WaveData(params BalloonGroup[] g) => groups.AddRange(g);

        public int BossCount
        {
            get
            {
                int n = 0;
                foreach (var g in groups) if (g.kind == BalloonKind.Boss || g.kind == BalloonKind.Dirigeable) n += g.count;
                return n;
            }
        }
    }

    // Le livre des vagues : les 10 vagues écrites à la main (modifiables dans l'inspecteur du WaveSpawner),
    // puis, après la victoire, des vagues calculées pour le mode infini.
    public static class WaveBook
    {
        static BalloonGroup N(int count, int layers, float interval = 0.7f) => new BalloonGroup(BalloonKind.Normal, count, layers, interval);
        static BalloonGroup R(int count, int layers) => new BalloonGroup(BalloonKind.Rapide, count, layers, 0.5f);
        static BalloonGroup B(int count, int layers) => new BalloonGroup(BalloonKind.Blinde, count, layers, 1f);
        static BalloonGroup Boss(int count, int layers) => new BalloonGroup(BalloonKind.Boss, count, layers, 2.5f, 2f);

        public static List<WaveData> Default() => new List<WaveData>
        {
            new WaveData(N(8, 1, 0.9f), Boss(1, 5)),                                   // 1 : découverte
            new WaveData(N(10, 1), N(6, 2), Boss(1, 6)),                              // 2
            new WaveData(N(12, 2), R(5, 1), Boss(1, 8)),                              // 3 : premiers rapides
            new WaveData(N(10, 2), R(8, 2), N(6, 3), Boss(1, 10)),                    // 4
            new WaveData(N(15, 3), B(3, 3), Boss(2, 12)),                             // 5 : premiers blindés
            new WaveData(R(15, 2), N(10, 4), B(5, 3), Boss(2, 14)),                   // 6
            new WaveData(N(20, 4, 0.5f), B(8, 4), Boss(2, 16)),                       // 7
            new WaveData(R(20, 3), B(10, 5), Boss(3, 18)),                            // 8
            new WaveData(N(25, 5, 0.4f), R(15, 4), B(12, 5), Boss(3, 20)),            // 9
            new WaveData(N(20, 5, 0.5f), B(15, 6), Boss(3, 20),                       // 10 : le dirigeable rouge
                         new BalloonGroup(BalloonKind.Dirigeable, 1, 150, 1f, 4f)),
        };

        // Mode infini (après la vague 10) : de plus en plus de ballons, de plus en plus solides.
        public static WaveData Endless(int wave)
        {
            int k = wave - 10;
            return new WaveData(N(20 + 3 * k, 5 + k / 2, 0.4f), R(10 + 2 * k, 4 + k / 3), B(10 + 2 * k, 6 + k / 2),
                                Boss(3 + k / 3, 20 + 3 * k));
        }
    }
}
