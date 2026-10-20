using UnityEngine;

namespace SAE
{
    // Les triches de test (performances), partagées par le panneau PC (AdminPanel) et les boutons au casque (AdminButton).
    // Rien ici n'est appelé par le jeu normal.
    public static class AdminCheats
    {
        // Pose 'count' singes à des places libres tirées au hasard, avec les mêmes règles que la pose à la main.
        // type null = types au hasard. Renvoie le nombre posé (moins s'il n'y a plus de place).
        public static int FillMap(int count, MonkeyType? type, Rarity level)
        {
            int placed = 0;
            for (int tries = 0; tries < count * 50 && placed < count; tries++)
            {
                var pos = new Vector2(Random.Range(-MapLayout.HalfExtent, MapLayout.HalfExtent), Random.Range(-MapLayout.HalfExtent, MapLayout.HalfExtent));
                if (!MapLayout.CanPlace(pos, Placement.PathMargin) || GameState.Nearest(pos, Placement.Spacing) != null) continue;
                var t = type ?? (MonkeyType)Random.Range(0, MonkeyData.TypeCount);
                GameState.Placed.Add(new PlacedMonkey(new Monkey(t, level), pos));
                placed++;
            }
            GameState.NotifyChanged();
            Debug.Log($"Admin : {placed} singes posés.");
            return placed;
        }

        // La vague 100 ultra cheatée : 10 fois plus de ballons, 20 couches de plus, vies infinies.
        // Si la carte n'est pas chargée, la vague partira dès qu'elle le sera (WaveSpawner.Launch).
        public static void UltraWave()
        {
            WaveSpawner.CheatCount = 10;
            WaveSpawner.CheatLayers = 20;
            WaveSpawner.Invincible = true;
            if (Tutorial.Active) Tutorial.Finish();
            var spawner = WaveSpawner.Instance;
            if (spawner) spawner.SetWave(100);
            WaveSpawner.Launch();
        }

        // Remet les vagues normales (les réglages sont statiques et resteraient sinon au prochain Play)
        public static void ResetWaves()
        {
            WaveSpawner.CheatCount = 1;
            WaveSpawner.CheatLayers = 0;
            WaveSpawner.Invincible = false;
        }
    }
}
