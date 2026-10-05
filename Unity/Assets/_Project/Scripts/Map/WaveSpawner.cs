using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Envoie les vagues de ballons sur la piste.
    // Règle du GDD : éclater un ballon ne rapporte rien, finir une vague rapporte de l'argent.
    public class WaveSpawner : MonoBehaviour
    {
        public float tile = 3f;
        public float balloonHeight = 1f;
        public float pauseBetweenWaves = 4f;
        public float spawnInterval = 0.7f;
        public int startLives = 20;

        List<Vector3> path;
        int wave;
        int lives;
        string status = "";

        void Start()
        {
            path = new List<Vector3>();
            foreach (var cell in MapLayout.OrderedPath())
                path.Add(transform.position + MapLayout.CellLocal(cell.y, cell.x, tile) + Vector3.up * balloonHeight);
            lives = startLives;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            while (lives > 0)
            {
                wave++;
                for (float t = pauseBetweenWaves; t > 0; t -= Time.deltaTime)
                {
                    status = $"Vague {wave} dans {Mathf.CeilToInt(t)} s";
                    yield return null;
                }

                status = $"Vague {wave}";
                int count = 5 + wave * 3;
                int maxLayers = 1 + wave / 2;
                for (int i = 0; i < count && lives > 0; i++)
                {
                    Spawn(Random.Range(1, maxLayers + 1));
                    yield return new WaitForSeconds(spawnInterval);
                }

                while (Balloon.All.Count > 0 && lives > 0) yield return null;
                if (lives <= 0) break;

                int reward = 100 + wave * 20;
                GameState.Money += reward;
                status = $"Vague {wave} finie : +{reward}";
                yield return new WaitForSeconds(1.5f);
            }
            status = $"Perdu à la vague {wave}. Retourne au hub pour recommencer.";
        }

        void Spawn(int layers)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ballon";
            go.transform.localScale = Vector3.one * 0.9f;
            go.AddComponent<ColorTint>();
            go.AddComponent<Balloon>().Init(this, path, layers);
        }

        public void BalloonEscaped(int layers) => lives = Mathf.Max(0, lives - layers);

        void OnGUI()
        {
            GUI.Label(new Rect(10, 35, 600, 25), $"Vies : {lives}    {status}");
        }
    }
}
