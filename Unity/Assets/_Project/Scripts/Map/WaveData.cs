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
    //   vague 1 à 3   : ballons normaux seulement, on découvre l'arc et les singes
    //   vague 4       : premiers rapides       vague 8  : premiers blindés     vague 12 : premiers cœurs (se regonflent)
    //   vague 15      : premier boss (gros ballon lent), puis un toutes les 5 vagues ; dès la 50, à chaque vague
    //   vague 25, 50, 75 : un dirigeable rouge ; vague 100 : la finale, 4 dirigeables rouges -> victoire
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

            // Normaux : toujours là. 8 au début, ~68 à la vague 100 ; 1 couche, puis +1 toutes les 8 vagues ; de plus en plus serrés
            float interval = Mathf.Lerp(0.8f, 0.3f, Mathf.Clamp01((w - 1) / 60f));
            groups.Add(N(8 + w * 6 / 10, 1 + w / 8, interval));
            if (w >= 3) groups.Add(N(4 + w / 4, 2 + w / 8, interval));          // un 2e groupe, plus solide

            if (w >= 4) groups.Add(R(2 + (w - 4) * 3 / 5, 1 + (w - 4) / 10));     // rapides
            if (w >= 8) groups.Add(B(1 + (w - 8) / 2, 2 + (w - 8) / 10));         // blindés
            if (w >= 12 && w % 2 == 0) groups.Add(C(1 + (w - 12) / 6, 2 + (w - 12) / 8));   // cœurs, une vague sur deux

            // Boss : toutes les 5 vagues à partir de la 15, puis à chaque vague dès la 50
            if (w >= 15 && (w % 5 == 0 || w >= 50)) groups.Add(Boss(1 + (w - 15) / 20, 10 + w / 2));

            // Dirigeables rouges : aux vagues 25, 50, 75, et 4 pour la finale (100) ; en infini, toutes les 25 vagues
            if (w % 25 == 0) groups.Add(Blimp(w / 25, 60 + w));

            return new WaveData(groups.ToArray());
        }
    }
}
