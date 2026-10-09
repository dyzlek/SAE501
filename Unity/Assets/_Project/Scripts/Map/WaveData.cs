using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les sortes de ballons.
    // Normal : perd une couche par point de dégât. Rapide : petit et vif. Blindé : gris, prend moitié moins
    // de dégâts et ne peut pas être ralenti. Boss : gros, lent, très solide. Dirigeable : le boss final rouge.
    // Coeur : regagne une couche toutes les quelques secondes (ajouté à la fin pour garder les numéros des autres).
    public enum BalloonKind { Normal, Rapide, Blinde, Boss, Dirigeable, Coeur }

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

    // Le livre des vagues : 100 vagues calculées par une même formule, pour une difficulté qui monte sans à-coups,
    // avec des paliers façon Bloons TD (un nouveau ballon apparaît, puis revient de plus en plus souvent) :
    //   vague 1       : 8 ballons simples (le tutoriel, à l'arc seul)
    //   vague 3       : premiers rapides       vague 6  : premiers blindés     vague 9  : premiers cœurs (se regonflent)
    //   vague 10      : premier boss (gros ballon lent), puis un toutes les 5 vagues ; dès la 35, à chaque vague
    //   vague 20, 40, 60, 80 : 1 à 4 dirigeables rouges ; vague 100 : la finale, 5 dirigeables rouges -> victoire
    // Après la 100 : mode infini, la même formule continue.
    // Tout se règle ici (nombres, couches, écarts) ; rien n'est gardé dans la scène.
    public static class WaveBook
    {
        public const int Count = 100;   // la victoire

        static BalloonGroup N(int count, int layers, float interval) => new BalloonGroup(BalloonKind.Normal, count, layers, interval);
        static BalloonGroup R(int count, int layers) => new BalloonGroup(BalloonKind.Rapide, count, layers, 0.5f);
        static BalloonGroup B(int count, int layers) => new BalloonGroup(BalloonKind.Blinde, count, layers, 1f);
        static BalloonGroup C(int count, int layers) => new BalloonGroup(BalloonKind.Coeur, count, layers, 1f);
        static BalloonGroup Boss(int count, int layers) => new BalloonGroup(BalloonKind.Boss, count, layers, 2.5f, 2f);
        static BalloonGroup Blimp(int count, int layers) => new BalloonGroup(BalloonKind.Dirigeable, count, layers, 3f, 4f);

        const int MaxGroup = 60;   // ballons au plus par groupe
        static int Cap(int count) => Mathf.Min(count, MaxGroup);

        public static List<WaveData> Default()
        {
            var list = new List<WaveData>();
            for (int w = 1; w <= Count; w++) list.Add(Make(w));
            return list;
        }

        // Après la vague 100 : la formule continue
        public static WaveData Endless(int wave) => Make(wave);

        // La vague w : chaque sorte de ballon arrive à son palier, puis son nombre et ses couches montent doucement.
        public static WaveData Make(int w)
        {
            var groups = new List<BalloonGroup>();

            // Vague 1 : le tutoriel, à l'arc seul (aucun ballon ne doit passer) : elle reste simple.
            if (w == 1) return new WaveData(N(8, 1, 0.8f));

            // DIFFICILE (9 oct.) : les ballons arrivent tôt, nombreux, serrés, et gagnent vite des couches.
            // Chaque groupe est plafonné à MaxGroup ballons : au-delà, ce sont les couches qui montent (le Quest suit).
            float interval = Mathf.Lerp(0.6f, 0.2f, Mathf.Clamp01((w - 2) / 40f));
            groups.Add(N(Cap(8 + w), 1 + w / 5, interval));                               // normaux : +1 couche toutes les 5 vagues
            groups.Add(N(Cap(3 + w / 2), 2 + w / 5, interval));                           // un 2e groupe, plus solide

            if (w >= 3) groups.Add(R(Cap(2 + (w - 3)), 1 + (w - 3) / 6));                 // rapides dès la 3
            if (w >= 6) groups.Add(B(Cap(1 + (w - 6) * 2 / 3), 2 + (w - 6) / 6));         // blindés dès la 6
            if (w >= 9) groups.Add(C(Cap(1 + (w - 9) / 4), 2 + (w - 9) / 5));             // cœurs dès la 9, à chaque vague

            // Boss : toutes les 5 vagues à partir de la 10, puis à chaque vague dès la 35
            if (w >= 10 && (w % 5 == 0 || w >= 35)) groups.Add(Boss(1 + (w - 10) / 12, 15 + w));

            // Dirigeables rouges : toutes les 20 vagues (1 à la 20, 2 à la 40... 5 pour la finale, la 100)
            if (w % 20 == 0) groups.Add(Blimp(w / 20, 100 + 2 * w));

            return new WaveData(groups.ToArray());
        }
    }
}
