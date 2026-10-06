using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Envoie les vagues de ballons sur la piste.
    // - Une vague ne part que quand le joueur appuie sur LANCER (StartWave) : il décide quand il est prêt.
    // - Chaque vague finit par un ou plusieurs boss (gros ballons lents et solides).
    // - Perdre une vague ne fait pas tout recommencer : on reprend AU DÉBUT de cette vague,
    //   avec les vies qu'on avait en la lançant (singes posés et argent gardés).
    // Règle du GDD : éclater un ballon ne rapporte rien, finir une vague rapporte de l'argent.
    // Rien n'est affiché à l'écran (nausée en VR) : les tableaux WaveBoard du décor lisent Wave, Lives et Status.
    public class WaveSpawner : MonoBehaviour
    {
        public float balloonHeight = 1f;
        public float spawnInterval = 0.7f;
        public int startLives = 20;
        public int wavesPerExtraBoss = 5;   // 1 boss, puis 1 de plus toutes les 5 vagues

        List<Vector3> path;
        int livesAtWaveStart;

        public int Wave { get; private set; } = 1;   // la vague en cours, ou la prochaine à lancer
        public int Lives { get; private set; }
        public bool Running { get; private set; }
        public string Status { get; private set; } = "Appuie sur LANCER";

        void Start()
        {
            path = new List<Vector3>();
            foreach (var p in MapLayout.PathPoints())
                path.Add(transform.position + p + Vector3.up * balloonHeight);
            Lives = startLives;
        }

        public int BossCount => 1 + (Wave - 1) / Mathf.Max(1, wavesPerExtraBoss);

        // Appelé par le bouton LANCER (hub ou carte). Ne fait rien si une vague est déjà en cours.
        public void StartWave()
        {
            if (Running) return;
            StartCoroutine(RunWave());
        }

        IEnumerator RunWave()
        {
            Running = true;
            livesAtWaveStart = Lives;
            Status = $"Vague {Wave} en cours";

            // Les ballons normaux, puis le ou les boss
            int count = 5 + Wave * 3;
            int maxLayers = 1 + Wave / 2;
            for (int i = 0; i < count && Lives > 0; i++)
            {
                Spawn(Random.Range(1, maxLayers + 1), false);
                yield return new WaitForSeconds(spawnInterval);
            }
            for (int i = 0; i < BossCount && Lives > 0; i++)
            {
                yield return new WaitForSeconds(spawnInterval * 2f);
                Spawn(maxLayers * 4 + Wave, true);
            }

            while (Balloon.All.Count > 0 && Lives > 0) yield return null;

            if (Lives <= 0)
            {
                // Perdu : on efface les ballons et on remet la vague à zéro (même numéro, mêmes vies qu'au départ)
                foreach (var b in new List<Balloon>(Balloon.All)) Destroy(b.gameObject);
                Lives = livesAtWaveStart;
                Status = $"Vague {Wave} perdue : relance-la";
            }
            else
            {
                GameState.WavesWon++;
                int reward = 20 + Wave * 10;   // à équilibrer avec la banane (5) et le coffre (25 + 20 par vague vaincue)
                Economy.Earn(reward);
                Status = $"Vague {Wave} gagnée : +{reward}";
                Wave++;
            }
            Running = false;
        }

        void Spawn(int layers, bool boss)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = boss ? "Boss" : "Ballon";
            go.tag = Tags.Ballon;
            go.transform.localScale = Vector3.one * (boss ? 1.8f : 0.9f);
            go.AddComponent<ColorTint>();
            go.AddComponent<Mirrored>();
            go.AddComponent<Balloon>().Init(this, path, layers, boss);
        }

        public void BalloonEscaped(int layers) => Lives = Mathf.Max(0, Lives - layers);
    }
}
