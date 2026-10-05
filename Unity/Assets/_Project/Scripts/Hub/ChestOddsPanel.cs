using System.Text;
using UnityEngine;
using Sae501.Coffres;
using ChestRarity = Sae501.Coffres.Rarity;   // les 7 raretés du coffre (SAE.Rarity = celles des singes, à harmoniser au chantier B)

namespace SAE
{
    // Panneau à côté du coffre : ce que le coffre peut donner, en direct.
    // Vagues vaincues, prix, nombre de singes par coffre, et la chance de chaque rareté (dans sa couleur).
    // Une rareté pas encore débloquée indique à partir de quelle vague elle le sera.
    public class ChestOddsPanel : MonoBehaviour
    {
        public ChestController chest;
        public TextMesh text;

        int shownWaves = -1;
        int shownMoney = -1;

        void Update()
        {
            if (!chest || !text) return;
            if (shownWaves == GameState.WavesWon && shownMoney == Economy.Money) return;   // rien n'a changé
            shownWaves = GameState.WavesWon;
            shownMoney = Economy.Money;

            var sb = new StringBuilder();
            sb.AppendLine("<b>CHANCES DU COFFRE</b>");
            sb.AppendLine($"Vagues vaincues : {GameState.WavesWon}");
            string priceColor = Economy.CanAfford(chest.Price) ? "#FFD233" : "#999999";
            sb.AppendLine($"Prix : <color={priceColor}>{chest.Price}</color>   Singes : {chest.MonkeysPerChest}");
            sb.AppendLine();

            var odds = chest.CurrentOdds();
            for (int i = 0; i < RarityInfo.Count; i++)
            {
                var r = (ChestRarity)i;
                string hex = ColorUtility.ToHtmlStringRGB(RarityInfo.ColorOf(r));
                string name = r == ChestRarity.LGBT ? "Arc-en-ciel" : r.ToString();
                if (ChestOdds.IsUnlocked(chest.oddsSettings, r, GameState.WavesWon))
                    sb.AppendLine($"<color=#{hex}>{name}</color>  {Percent(odds[i])}");
                else
                    sb.AppendLine($"<color=#777777>{name}  dès la vague {chest.oddsSettings.unlockAtWave[i]}</color>");
            }
            text.text = sb.ToString().TrimEnd();
        }

        static string Percent(float p) => p >= 0.1f ? $"{p * 100f:0} %" : p >= 0.01f ? $"{p * 100f:0.#} %" : $"{p * 100f:0.##} %";
    }
}
