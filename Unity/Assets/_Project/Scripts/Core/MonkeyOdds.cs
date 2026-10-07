using System;
using UnityEngine;

namespace SAE
{
    // Réglages des chances de chaque TYPE de singe au coffre (la rareté est tirée à part, voir ChestOdds).
    // Au début, le coffre ne donne que des Classiques ; les autres types se débloquent avec des bananes,
    // sur les plaques de la bibliothèque (avant : par vague vaincue ; changé après la critique du 7 oct.).
    [Serializable]
    public class MonkeyOddsSettings
    {
        // Ordre : Classique, Boomerang, Canon, Sniper, Punaise, Glace, Colle (comme MonkeyType).
        public float[] weights = { 30f, 20f, 15f, 10f, 20f, 15f, 15f };
    }

    // Calcul pur des chances par type (même principe que ChestOdds pour les raretés).
    public static class MonkeyOdds
    {
        // Renvoie une probabilité par type (somme = 1) : les types débloqués se partagent 100 % selon leurs poids.
        public static float[] Compute(MonkeyOddsSettings s)
        {
            var p = new float[MonkeyData.TypeCount];
            float sum = 0f;
            for (int i = 0; i < p.Length; i++)
            {
                if (!GameState.IsUnlocked((MonkeyType)i)) continue;
                p[i] = s.weights[i];
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
