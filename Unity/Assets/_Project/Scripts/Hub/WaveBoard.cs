using System.Text;
using UnityEngine;

namespace SAE
{
    // Tableau dans le décor (hub et carte) : vague, vies, état, et ce qui arrive dans la vague.
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
            string number = spawner.Wave <= spawner.LastWrittenWave ? $"{spawner.Wave} / {spawner.LastWrittenWave}" : $"{spawner.Wave} (infini)";
            sb.AppendLine($"<color=#FFD45A>VAGUE {number}</color>");
            string lives = spawner.Lives <= 5 ? $"<color=#FF5555>{spawner.Lives}</color>" : spawner.Lives.ToString();
            sb.AppendLine($"Vies : {lives}");
            sb.AppendLine($"<size=30>{spawner.Status}</size>");
            sb.Append($"<size=30>{Composition(spawner.Current)}</size>");
            text.text = sb.ToString();
        }

        // Au hub : la prochaine vague (le numéro est gardé dans GameState), et comment la lancer
        static readonly int WrittenWaves = WaveBook.Default().Count;

        static string AtHub()
        {
            int wave = GameState.WavesWon + 1;
            string number = wave <= WrittenWaves ? $"{wave} / {WrittenWaves}" : $"{wave} (infini)";
            var next = wave <= WrittenWaves ? WaveBook.Default()[wave - 1] : WaveBook.Endless(wave);
            return $"<color=#FFD45A>VAGUE {number}</color>\n<size=30>Prête : appuie sur LANCER</size>\n<size=30>{Composition(next)}</size>";
        }

        // Ce que contient la vague, par sorte de ballon : « 18 ballons · 5 rapides · 1 boss ».
        static string Composition(WaveData wave)
        {
            var counts = new int[System.Enum.GetValues(typeof(BalloonKind)).Length];
            foreach (var g in wave.groups) counts[(int)g.kind] += g.count;
            var parts = new System.Collections.Generic.List<string>();
            if (counts[(int)BalloonKind.Normal] > 0) parts.Add($"{counts[(int)BalloonKind.Normal]} ballons");
            if (counts[(int)BalloonKind.Rapide] > 0) parts.Add($"{counts[(int)BalloonKind.Rapide]} rapides");
            if (counts[(int)BalloonKind.Coeur] > 0) parts.Add($"<color=#FF7FB0>{counts[(int)BalloonKind.Coeur]} cœurs</color>");
            if (counts[(int)BalloonKind.Blinde] > 0) parts.Add($"<color=#B0B4BA>{counts[(int)BalloonKind.Blinde]} blindés</color>");
            if (counts[(int)BalloonKind.Boss] > 0) parts.Add($"<color=#C080E0>{counts[(int)BalloonKind.Boss]} boss</color>");
            if (counts[(int)BalloonKind.Dirigeable] > 0) parts.Add("<color=#FF4040>DIRIGEABLE</color>");
            return string.Join(" · ", parts);
        }
    }
}
