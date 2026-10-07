using System.Text;
using UnityEngine;

namespace SAE
{
    // Tableau dans le décor (hub et carte) : vague, vies et état. Il ne dit PAS ce qu'il y a dans la vague :
    // la surprise fait partie du jeu (critique du 7 oct.). Remplace l'affichage collé à l'écran, qui donne la nausée en VR.
    public class WaveBoard : MonoBehaviour
    {
        public TextMesh text;

        void Update()
        {
            if (!text) return;
            var spawner = WaveSpawner.Instance;
            if (!spawner) { text.text = AtHub(); return; }   // au hub : la carte n'est pas chargée
            var sb = new StringBuilder();
            string number = spawner.Wave <= spawner.LastWrittenWave ? $"{spawner.Wave} / {spawner.LastWrittenWave}" : $"{spawner.Wave} (infini)";
            sb.AppendLine($"<color=#FFD45A>VAGUE {number}</color>");
            string lives = spawner.Lives <= 5 ? $"<color=#FF5555>{spawner.Lives}</color>" : spawner.Lives.ToString();
            sb.AppendLine($"Vies : {lives}");
            sb.Append($"<size=30>{spawner.Status}</size>");
            text.text = sb.ToString();
        }

        // Au hub : la prochaine vague (le numéro est gardé dans GameState), et comment la lancer
        static readonly int WrittenWaves = WaveBook.Default().Count;

        static string AtHub()
        {
            int wave = GameState.WavesWon + 1;
            string number = wave <= WrittenWaves ? $"{wave} / {WrittenWaves}" : $"{wave} (infini)";
            return $"<color=#FFD45A>VAGUE {number}</color>\n<size=30>Prête : appuie sur LANCER</size>\n<size=24><color=#B8C8B8>Les commandes ? Lève ta main gauche devant tes yeux</color></size>";
        }
    }
}
