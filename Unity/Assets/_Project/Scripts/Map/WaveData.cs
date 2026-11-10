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
        public bool camo;                  // camouflés : seuls le Tireur et les singes Violet et plus les voient (MonkeyData.SeesCamo)

        public BalloonGroup(BalloonKind kind, int count, int layers, float interval = 0.7f, float pause = 1f, bool camo = false)
        {
            this.kind = kind; this.count = count; this.layers = layers; this.interval = interval; this.pause = pause; this.camo = camo;
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

        public bool HasCamo => groups.Exists(g => g.camo);
    }

    // Le livre des vagues : 100 vagues calculées par une même formule, pour une difficulté qui monte sans à-coups,
    // avec des paliers façon Bloons TD (un nouveau ballon apparaît, puis revient de plus en plus souvent) :
    //   vague 1 et 2  : ballons normaux seulement, on découvre l'arc et les singes
    //   vague 3       : premiers rapides       vague 6  : premiers blindés     vague 10 : premiers cœurs (se regonflent)
    //   vague 10      : premiers CAMOUFLÉS (seuls le Tireur et les singes Violet et plus les voient), à chaque vague ensuite ;
    //                   rapides camouflés dès la 20
    //   vague 12      : premier boss (gros ballon lent), puis un toutes les 4 vagues ; dès la 40, à chaque vague
    //   vague 25, 50, 75 : un dirigeable rouge ; vague 100 : la finale, 4 dirigeables rouges -> victoire
    // Le 23 oct., tout est durci (retour de Dylan : « les vagues sont trop faciles ») : plus de ballons, plus de couches,
    // plus serrés, chaque sorte arrive plus tôt.
    // Après la 100 : mode infini, la même formule continue.
    // Tout se règle ici (nombres, couches, écarts) ; rien n'est gardé dans la scène.
    public static class WaveBook
    {
        public const int Count = 100;   // la victoire

        // TEST (météo) : un boss à CHAQUE vague pour voir l'orage tout de suite. Remis à false (21 oct.) ; à passer à true pour retester la météo.
        const bool TestBossEveryWave = false;

        static BalloonGroup N(int count, int layers, float interval) => new BalloonGroup(BalloonKind.Normal, count, layers, interval);
        static BalloonGroup R(int count, int layers) => new BalloonGroup(BalloonKind.Rapide, count, layers, 0.45f);
        static BalloonGroup Camo(BalloonKind kind, int count, int layers) => new BalloonGroup(kind, count, layers, 0.6f, 1.5f, camo: true);
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

            // Normaux : toujours là. 10 au début, ~100 à la vague 100 ; 1 couche, puis +1 toutes les 6 vagues ; vite très serrés
            float interval = Mathf.Lerp(0.7f, 0.25f, Mathf.Clamp01((w - 1) / 40f));
            groups.Add(N(10 + w * 9 / 10, 1 + w / 6, interval));
            if (w >= 2) groups.Add(N(5 + w / 3, 2 + w / 6, interval));          // un 2e groupe, plus solide

            if (w >= 3) groups.Add(R(3 + (w - 3) * 4 / 5, 1 + (w - 3) / 7));      // rapides
            if (w >= 6) groups.Add(B(2 + (w - 6) * 2 / 3, 2 + (w - 6) / 7));     // blindés
            if (w >= 10 && w % 2 == 0) groups.Add(C(2 + (w - 10) / 4, 2 + (w - 10) / 6));   // cœurs, une vague sur deux

            // Camouflés : il faut un Tireur, ou des singes Violet et plus, pour les arrêter (ou bien viser à l'arc)
            if (w >= 10) groups.Add(Camo(BalloonKind.Normal, 2 + (w - 10) / 3, 1 + (w - 10) / 8));
            if (w >= 20) groups.Add(Camo(BalloonKind.Rapide, 1 + (w - 20) / 4, 1 + (w - 20) / 10));

            // Boss : toutes les 4 vagues à partir de la 12, puis à chaque vague dès la 40
            if (TestBossEveryWave || (w >= 12 && (w % 4 == 0 || w >= 40))) groups.Add(Boss(1 + Mathf.Max(0, w - 12) / 15, 14 + w * 7 / 10));

            // Dirigeables rouges : aux vagues 25, 50, 75, et 4 pour la finale (100) ; en infini, toutes les 25 vagues
            if (w % 25 == 0) groups.Add(Blimp(w / 25, 80 + w * 3 / 2));

            return new WaveData(groups.ToArray());
        }
    }
}
