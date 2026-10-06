using System;
using UnityEngine;

namespace Sae501.Coffres
{
    // Réglages des probabilités (modifiables dans l'inspecteur du coffre).
    // Le coffre ne donne que Gris → Rouge : l'arc-en-ciel (LGBT) et le blanc s'obtiennent UNIQUEMENT par fusion.
    [Serializable]
    public class ChestOddsSettings
    {
        // Poids relatifs de Gris, Vert, Bleu, Violet, Jaune, Rouge.
        public float[] baseWeights = { 50f, 28f, 14f, 6f, 2f, 0.9f };

        // Paliers : nombre de vagues vaincues à partir duquel chaque rareté peut sortir du coffre
        // (ordre : Gris, Vert, Bleu, Violet, Jaune, Rouge). Les raretés hautes arrivent tard.
        public int[] unlockAtWave = { 0, 0, 3, 7, 12, 20 };

        // Une rareté qui vient de se débloquer monte en puissance sur ce nombre de vagues
        // (au lieu d'arriver d'un coup à sa chance normale).
        public int rampWaves = 3;
    }

    // Calcul pur des probabilités (pas de MonoBehaviour : facile à tester).
    public static class ChestOdds
    {
        // LGBT (arc-en-ciel) n'a pas de palier : il n'est jamais débloqué au coffre.
        public static bool IsUnlocked(ChestOddsSettings s, Rarity r, int wavesWon) =>
            (int)r < s.unlockAtWave.Length && wavesWon >= s.unlockAtWave[(int)r];

        // Renvoie 7 probabilités (somme = 1), une par rareté ; celle de LGBT vaut toujours 0.
        public static float[] Compute(ChestOddsSettings s, int wavesWon)
        {
            var p = new float[RarityInfo.Count];

            // Les raretés débloquées se partagent 100 % selon leurs poids.
            // Une rareté fraîchement débloquée voit son poids monter progressivement.
            float sum = 0f;
            for (int i = 0; i < s.baseWeights.Length; i++)
            {
                if (!IsUnlocked(s, (Rarity)i, wavesWon)) continue;
                float ramp = Mathf.Clamp01((float)(wavesWon - s.unlockAtWave[i] + 1) / Mathf.Max(1, s.rampWaves));
                p[i] = s.baseWeights[i] * ramp;
                sum += p[i];
            }
            for (int i = 0; i < s.baseWeights.Length; i++) p[i] /= sum;
            return p;
        }

        // Tire une rareté selon les probabilités.
        public static Rarity Roll(float[] probabilities)
        {
            float r = UnityEngine.Random.value;
            float acc = 0f;
            for (int i = 0; i < probabilities.Length; i++)
            {
                acc += probabilities[i];
                if (r < acc) return (Rarity)i;
            }
            // Sécurité (arrondis) : la dernière rareté possible.
            for (int i = probabilities.Length - 1; i >= 0; i--)
                if (probabilities[i] > 0f) return (Rarity)i;
            return Rarity.Gris;
        }
    }
}
