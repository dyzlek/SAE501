using System;
using UnityEngine;

namespace SAE
{
    // Réglages des chances de chaque TYPE de singe au coffre (la rareté est tirée à part, voir ChestOdds).
    // Au début, le coffre ne donne que des Classiques, puis les autres types arrivent petit à petit.
    [Serializable]
    public class MonkeyOddsSettings
    {
        // Ordre : Classique, Boomerang, Canon, Sniper, Punaise, Glace, Colle (comme MonkeyType).
        public float[] weights = { 30f, 20f, 15f, 10f, 20f, 15f, 15f };

        // Nombre de vagues vaincues à partir duquel chaque type peut sortir du coffre.
        public int[] unlockAtWave = { 0, 1, 5, 9, 2, 3, 7 };

        // Un type qui vient d'arriver monte en puissance sur ce nombre de vagues.
        public int rampWaves = 2;
    }

    // Calcul pur des chances par type (même principe que ChestOdds pour les raretés).
    public static class MonkeyOdds
    {
        public static bool IsUnlocked(MonkeyOddsSettings s, MonkeyType t, int wavesWon) =>
            wavesWon >= s.unlockAtWave[(int)t];

        // Renvoie une probabilité par type (somme = 1).
        public static float[] Compute(MonkeyOddsSettings s, int wavesWon)
        {
            var p = new float[MonkeyData.TypeCount];
            float sum = 0f;
            for (int i = 0; i < p.Length; i++)
            {
                if (!IsUnlocked(s, (MonkeyType)i, wavesWon)) continue;
                float ramp = Mathf.Clamp01((float)(wavesWon - s.unlockAtWave[i] + 1) / Mathf.Max(1, s.rampWaves));
                p[i] = s.weights[i] * ramp;
                sum += p[i];
            }
            for (int i = 0; i < p.Length; i++) p[i] /= sum;
            return p;
        }

        public static MonkeyType Roll(float[] probabilities)
        {
            float r = UnityEngine.Random.value;
            float acc = 0f;
            for (int i = 0; i < probabilities.Length; i++)
            {
                acc += probabilities[i];
                if (r < acc) return (MonkeyType)i;
            }
            return MonkeyType.Classique;   // sécurité (arrondis)
        }
    }
}
