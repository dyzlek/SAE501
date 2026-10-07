using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les vagues de ballons, en deux phases :
    // - PRÉPARATION : rien n'attaque, on gère les bananes, le coffre et le plateau, puis on appuie sur LANCER ;
    // - ATTAQUE : la vague suit sa liste de groupes (WaveBook), écrite à l'avance.
    // Chaque vague commence avec toutes les vies (startLives). Perdre une vague ne fait pas tout recommencer :
    // on reprend AU DÉBUT de cette vague (singes posés et argent gardés).
    // Victoire après la vague 10 (le dirigeable rouge), puis mode infini pour qui veut continuer.
    // Règle du GDD : éclater un ballon ne rapporte rien, finir une vague rapporte de l'argent.
    // Rien n'est affiché à l'écran (nausée en VR) : les tableaux WaveBoard du décor lisent Wave, Lives et Status.
    public class WaveSpawner : MonoBehaviour
    {
        // Les vagues de la scène Labyrinthe (null quand on est au hub)
        public static WaveSpawner Instance { get; private set; }

        public float balloonHeight = 1f;
        public int startLives = 20;
        public List<WaveData> waves = WaveBook.Default();

        List<Vector3> path;

        public int Wave { get; private set; } = 1;   // la vague en cours, ou la prochaine à lancer
        public int Lives { get; private set; }
        public bool Running { get; private set; }
        public bool Won { get; private set; }         // la vague 10 a été gagnée
        public string Status { get; private set; } = "Prépare-toi, puis appuie sur LANCER";

        public int LastWrittenWave => waves.Count;
        public WaveData Current => Wave <= waves.Count ? waves[Wave - 1] : WaveBook.Endless(Wave);
        public int BossCount => Current.BossCount;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            path = new List<Vector3>();
            foreach (var p in MapLayout.PathPoints())
                path.Add(transform.position + p + Vector3.up * balloonHeight);
            Lives = startLives;
            // La scène est rechargée à chaque retour sur la carte : on reprend à la vague suivante (gardée dans GameState)
            Wave = GameState.WavesWon + 1;
            Won = GameState.WavesWon >= waves.Count;
            if (Levels.LaunchOnArrival)
            {
                Levels.LaunchOnArrival = false;
                StartWave();
            }
        }

        // LANCER, depuis le hub ou la carte : sur la carte, la vague part ; au hub, on part sur la carte et elle démarre à l'arrivée
        public static void Launch()
        {
            if (Instance) { Instance.StartWave(); return; }
            Levels.LaunchOnArrival = true;
            Levels.Load(Level.Carte);
        }

        // Appelé par le bouton LANCER (hub ou carte). Ne fait rien si une vague est déjà en cours.
        public void StartWave()
        {
            if (Running) return;
            StartCoroutine(RunWave(Current));
        }

        IEnumerator RunWave(WaveData wave)
        {
            Running = true;
            Lives = startLives;   // les vies repartent au maximum à chaque vague
            Status = "Attaque en cours !";

            // Les groupes, dans l'ordre
            foreach (var group in wave.groups)
            {
                yield return new WaitForSeconds(group.pause);
                for (int i = 0; i < group.count && Lives > 0; i++)
                {
                    Spawn(group.kind, group.layers);
                    yield return new WaitForSeconds(group.interval);
                }
                if (Lives <= 0) break;
            }

            while (Balloon.All.Count > 0 && Lives > 0) yield return null;

            if (Lives <= 0)
            {
                // Perdu : on efface les ballons et on remet la vague à zéro (même numéro, toutes les vies)
                foreach (var b in new List<Balloon>(Balloon.All)) Destroy(b.gameObject);
                Lives = startLives;
                Status = $"Vague {Wave} perdue : relance-la";
            }
            else
            {
                GameState.WavesWon++;
                int reward = 20 + Wave * 10;   // à équilibrer avec la banane (5) et le coffre (25 + 20 par vague vaincue)
                Economy.Earn(reward);
                if (Wave == LastWrittenWave)
                {
                    Won = true;
                    Status = $"VICTOIRE ! +{reward} · mode infini : LANCER";
                }
                else Status = $"Vague {Wave} gagnée : +{reward}";
                Wave++;
                Lives = startLives;   // la vague suivante repart avec toutes les vies (affiché pendant la préparation)
            }
            Running = false;
        }

        void Spawn(BalloonKind kind, int layers)
        {
            // Avec un modèle 3D, le ballon est un objet vide : Balloon.Init y pose le modèle et son collider.
            // Sans modèle, on garde l'ancienne sphère (capsule pour le dirigeable).
            var go = BalloonVisuals.Model(kind) ? new GameObject()
                : GameObject.CreatePrimitive(kind == BalloonKind.Dirigeable ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            go.name = kind.ToString();
            go.tag = Tags.Ballon;
            go.AddComponent<ColorTint>();
            go.AddComponent<Balloon>().Init(this, path, layers, kind);
            PlayerRig.IgnoreCollisions(go);   // le joueur traverse les ballons et les boss ; les flèches les touchent toujours
        }

        public void BalloonEscaped(int layers) => Lives = Mathf.Max(0, Lives - layers);
    }
}
