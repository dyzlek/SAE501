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

    // Le livre des vagues : les vagues sont INFINIES, calculées par une même formule, pour une difficulté qui monte sans
    // à-coups, avec des paliers façon Bloons TD (un nouveau ballon apparaît, puis revient de plus en plus souvent) :
    //   vague 1 à 3   : ballons normaux seulement, on découvre l'arc et les singes
    //   vague 4       : premiers rapides       vague 8  : premiers blindés     vague 12 : premiers cœurs (se regonflent)
    //   vague 15      : premier boss (gros ballon lent), puis un toutes les 5 vagues ; dès la 50, à chaque vague
    //   toutes les 25 vagues : des dirigeables rouges (1 à la 25, 2 à la 50, 3 à la 75, 4 à la 100…)
    //   toutes les 10 vagues : une VAGUE SPÉCIALE, à thème, en plus de la vague normale (voir Special)
    // Le but du jeu est d'aller le plus loin possible (record dans GameState.BestWave), il n'y a pas de dernière vague.
    // Tout se règle ici (nombres, couches, écarts) ; rien n'est gardé dans la scène.
    public static class WaveBook
    {
        static BalloonGroup N(int count, int layers, float interval) => new BalloonGroup(BalloonKind.Normal, count, layers, interval);
        static BalloonGroup R(int count, int layers) => new BalloonGroup(BalloonKind.Rapide, count, layers, 0.5f);
        static BalloonGroup B(int count, int layers) => new BalloonGroup(BalloonKind.Blinde, count, layers, 1f);
        static BalloonGroup C(int count, int layers) => new BalloonGroup(BalloonKind.Coeur, count, layers, 1f);
        static BalloonGroup Boss(int count, int layers) => new BalloonGroup(BalloonKind.Boss, count, layers, 2.5f, 2f);
        static BalloonGroup Blimp(int count, int layers) => new BalloonGroup(BalloonKind.Dirigeable, count, layers, 3f, 4f);

        // La vague w (1, 2, 3… sans limite) : chaque sorte de ballon arrive à son palier, puis son nombre et ses couches montent.
        public static WaveData Make(int w)
        {
            var groups = new List<BalloonGroup>();

            // Normaux : toujours là. 8 au début, ~68 à la vague 100 ; 1 couche, puis +1 toutes les 8 vagues ; de plus en plus serrés
            // (l'écart se stabilise à 0,3 s vers la vague 60 : au-delà, c'est le nombre de couches qui fait monter la difficulté)
            float interval = Mathf.Lerp(0.8f, 0.3f, Mathf.Clamp01((w - 1) / 60f));
            groups.Add(N(8 + w * 6 / 10, 1 + w / 8, interval));
            if (w >= 3) groups.Add(N(4 + w / 4, 2 + w / 8, interval));          // un 2e groupe, plus solide

            if (w >= 4) groups.Add(R(2 + (w - 4) * 3 / 5, 1 + (w - 4) / 10));     // rapides
            if (w >= 8) groups.Add(B(1 + (w - 8) / 2, 2 + (w - 8) / 10));         // blindés
            if (w >= 12 && w % 2 == 0) groups.Add(C(1 + (w - 12) / 6, 2 + (w - 12) / 8));   // cœurs, une vague sur deux

            // Boss : toutes les 5 vagues à partir de la 15, puis à chaque vague dès la 50
            if (w >= 15 && (w % 5 == 0 || w >= 50)) groups.Add(Boss(1 + (w - 15) / 20, 10 + w / 2));

            // Dirigeables rouges : toutes les 25 vagues, un de plus à chaque fois
            if (w % 25 == 0) groups.Add(Blimp(w / 25, 60 + w));

            // Vague spéciale : toutes les 10 vagues, un thème qui change (et qui oblige à autre chose que « tout au même endroit »)
            if (w % 10 == 0) groups.Add(Special(w));

            return new WaveData(groups.ToArray());
        }

        // Les trois thèmes tournent : RUSH (une nuée de rapides), BLINDAGE (une colonne de blindés), COEURS (des ballons qui se regonflent)
        public static string SpecialName(int w) => (w / 10 % 3) switch { 1 => "RUSH", 2 => "BLINDAGE", _ => "COEURS" };

        static BalloonGroup Special(int w) => (w / 10 % 3) switch
        {
            1 => new BalloonGroup(BalloonKind.Rapide, 20 + w, 2 + w / 12, 0.2f, 3f),
            2 => new BalloonGroup(BalloonKind.Blinde, 6 + w / 4, 3 + w / 10, 0.7f, 3f),
            _ => new BalloonGroup(BalloonKind.Coeur, 8 + w / 5, 3 + w / 8, 0.6f, 3f),
        };

        // Une vague « à annoncer » (#133) : vague spéciale (toutes les 10) ou vague de boss (toutes les 5 dès la 15)
        public static bool IsSpecial(int w) => w % 10 == 0;
        public static bool IsBoss(int w) => w >= 15 && (w % 5 == 0 || w >= 50);
        public static bool IsAnnounced(int w) => IsSpecial(w) || (w >= 15 && w % 5 == 0);

        // La météo de la vague (#131) : seulement sur les vagues annoncées, elle change à chaque fois
        // (vague 10 : pluie, 15 : brouillard, 20 : nuit, 25 : pluie…). Les autres vagues sont au grand jour.
        public static Weather WeatherOf(int w)
        {
            if (!IsAnnounced(w)) return Weather.Clair;
            return (w / 5 % 3) switch { 0 => Weather.Brouillard, 1 => Weather.Nuit, _ => Weather.Pluie };
        }
    }

    public enum Weather { Clair, Brouillard, Nuit, Pluie }
}
