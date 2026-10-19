using System.Text;
using UnityEngine;

namespace SAE
{
    // Tableau dans le décor (hub et carte) : vague, vies, record et état. Il ne dit PAS ce qu'il y a dans la vague :
    // la surprise fait partie du jeu (critique du 7 oct.). Les vagues sont infinies : on affiche le record à battre.
    // Remplace l'affichage collé à l'écran, qui donne la nausée en VR.
    public class WaveBoard : MonoBehaviour
    {
        public TextMesh text;

        void Update()
        {
            if (!text) return;
            var spawner = WaveSpawner.Instance;
            if (!spawner) { text.text = AtHub(); return; }   // au hub : la carte n'est pas chargée
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#FFD45A>VAGUE {spawner.Wave}</color>");
            string lives = spawner.Lives <= 5 ? $"<color=#FF5555>{spawner.Lives}</color>" : spawner.Lives.ToString();
            sb.AppendLine($"Vies : {lives}   Record : {GameState.BestWave}");
            sb.Append($"<size=30>{spawner.Status}</size>");
            text.text = sb.ToString();
        }

        // Au hub : la prochaine vague (le numéro est gardé dans GameState), et comment la lancer
        static string AtHub()
        {
            int wave = GameState.WavesWon + 1;
            return $"<color=#FFD45A>VAGUE {wave}</color>\nRecord : {GameState.BestWave}\n<size=30>Prête : appuie sur LANCER</size>";
        }
    }
}
