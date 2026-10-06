using System.Text;
using UnityEngine;
using Sae501.Coffres;
using ChestRarity = Sae501.Coffres.Rarity;   // les 7 raretés du coffre (SAE.Rarity = celles des singes, à harmoniser au chantier B)

namespace SAE
{
    // Panneau à côté du coffre : ce que le coffre peut donner, en direct.
    // Vagues vaincues, prix, nombre de singes par coffre, la chance de chaque rareté (dans sa couleur)
    // et la chance de chaque type de singe.
    // Une rareté pas encore débloquée indique à partir de quelle vague elle le sera.
    // L'arc-en-ciel et le blanc ne sortent jamais du coffre (fusion seulement).
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
            for (int i = 0; i < chest.oddsSettings.unlockAtWave.Length; i++)
            {
                var r = (ChestRarity)i;
                string hex = ColorUtility.ToHtmlStringRGB(RarityInfo.ColorOf(r));
                string name = r.ToString();
                if (ChestOdds.IsUnlocked(chest.oddsSettings, r, GameState.WavesWon))
                    sb.AppendLine($"<color=#{hex}>{name}</color>  {Percent(odds[i])}");
                else
                    sb.AppendLine($"<color=#777777>{name}  dès la vague {chest.oddsSettings.unlockAtWave[i]}</color>");
            }
            sb.AppendLine("<color=#AAAAAA>Arc-en-ciel et Blanc : fusion seulement</color>");

            // Chances par type de singe, deux par ligne pour garder le panneau compact
            var reward = chest.GetComponent<ChestReward>();
            if (reward)
            {
                sb.AppendLine();
                sb.AppendLine("<b>SINGES</b>");
                var typeOdds = reward.CurrentTypeOdds();
                for (int i = 0; i < MonkeyData.TypeCount; i++)
                {
                    var t = (MonkeyType)i;
                    sb.Append(MonkeyOdds.IsUnlocked(reward.typeOdds, t, GameState.WavesWon)
                        ? $"{t} {Percent(typeOdds[i])}"
                        : $"<color=#777777>{t} : vague {reward.typeOdds.unlockAtWave[i]}</color>");
                    if (i % 2 == 1) sb.AppendLine();
                    else sb.Append("    ");
                }
                sb.AppendLine();
            }
            text.text = sb.ToString().TrimEnd();
        }

        static string Percent(float p) => p >= 0.1f ? $"{p * 100f:0} %" : p >= 0.01f ? $"{p * 100f:0.#} %" : $"{p * 100f:0.##} %";
    }
}
