using System;
using UnityEngine;

namespace Sae501.Coffres
{
    // Réglages des probabilités (modifiables dans l'inspecteur du coffre).
    [Serializable]
    public class ChestOddsSettings
    {
        // Poids relatifs de Gris, Vert, Bleu, Violet, Jaune, Rouge (le LGBT est géré à part).
        public float[] baseWeights = { 50f, 28f, 14f, 6f, 2f, 0.9f };

        // La progression se compte en VAGUES VAINCUES (avant : en coffres ouverts).
        // Nombre de vagues vaincues à partir duquel chaque rareté devient possible
        // (ordre : Gris, Vert, Bleu, Violet, Jaune, Rouge, LGBT).
        public int[] unlockAtWave = { 0, 0, 0, 1, 3, 5, 7 };

        // Une rareté qui vient de se débloquer monte en puissance sur ce nombre de vagues
        // (au lieu d'arriver d'un coup à sa chance normale).
        public int rampWaves = 3;

        // Chance de LGBT (0.001 = 0,1 %) quand il se débloque, puis au "gel".
        public float lgbtChanceAtStart = 0.001f;
        public float lgbtChanceAtFreeze = 0.05f;

        // À partir de ce nombre de vagues vaincues, la chance de LGBT arrête d'augmenter.
        public int freezeAfterWaves = 10;
    }

    // Calcul pur des probabilités (pas de MonoBehaviour : facile à tester).
    public static class ChestOdds
    {
        public static bool IsUnlocked(ChestOddsSettings s, Rarity r, int openedCount) =>
            openedCount >= s.unlockAtWave[(int)r];

        // Renvoie 7 probabilités (somme = 1), une par rareté.
        public static float[] Compute(ChestOddsSettings s, int openedCount)
        {
            var p = new float[RarityInfo.Count];

            // LGBT : 0 tant qu'il n'est pas débloqué, puis monte linéairement jusqu'au gel.
            float lgbt = 0f;
            int lgbtUnlock = s.unlockAtWave[(int)Rarity.LGBT];
            if (openedCount >= lgbtUnlock)
            {
                int span = Mathf.Max(1, s.freezeAfterWaves - lgbtUnlock);
                float t = Mathf.Clamp01((float)(openedCount - lgbtUnlock) / span);
                lgbt = Mathf.Lerp(s.lgbtChanceAtStart, s.lgbtChanceAtFreeze, t);
            }

            // Les autres raretés se partagent le reste selon leurs poids.
            // Une rareté fraîchement débloquée voit son poids monter progressivement.
            float sum = 0f;
            for (int i = 0; i < s.baseWeights.Length; i++)
            {
                if (!IsUnlocked(s, (Rarity)i, openedCount)) continue;
                float ramp = Mathf.Clamp01((float)(openedCount - s.unlockAtWave[i] + 1) / Mathf.Max(1, s.rampWaves));
                p[i] = s.baseWeights[i] * ramp;
                sum += p[i];
            }
            for (int i = 0; i < s.baseWeights.Length; i++) p[i] = p[i] / sum * (1f - lgbt);
            p[(int)Rarity.LGBT] = lgbt;
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
