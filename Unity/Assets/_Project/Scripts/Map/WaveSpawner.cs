using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les vagues de ballons, en deux phases :
    // - PRÉPARATION : rien n'attaque, on gère les bananes, le coffre et le plateau, puis on appuie sur LANCER ;
    // - ATTAQUE : la vague suit sa liste de groupes, calculée par WaveBook.
    // Chaque vague commence avec toutes les vies (startLives). Perdre une vague ne fait pas tout recommencer :
    // on reprend AU DÉBUT de cette vague (singes posés et argent gardés).
    // Il n'y a PAS de fin : les vagues sont infinies, le but est d'aller le plus loin possible (record). Tous les
    // 10 vagues, un palier donne un bonus ; une vague sans aucun ballon passé est « parfaite » (+50 %).
    // Règle du GDD : éclater un ballon ne rapporte rien, finir une vague rapporte de l'argent.
    // Rien n'est affiché à l'écran (nausée en VR) : les tableaux WaveBoard du décor lisent Wave, Lives et Status.
    public class WaveSpawner : MonoBehaviour
    {
        // Les vagues de la scène Labyrinthe (null quand on est au hub)
        public static WaveSpawner Instance { get; private set; }

        public float balloonHeight = 1f;
        public int startLives = 20;

        List<Vector3> path;

        public int Wave { get; private set; } = 1;   // la vague en cours, ou la prochaine à lancer
        public int Lives { get; private set; }
        public bool Running { get; private set; }
        public string Status { get; private set; } = "Prépare-toi, puis appuie sur LANCER";

        // Pour le panneau d'annonce (EndScreen) : la vague qu'on vient de perdre, et le palier qu'on vient d'atteindre (vague, bonus)
        public event System.Action<int> WaveFailed;
        public event System.Action<int, int> MilestoneReached;

        public const int MilestoneEvery = 10;   // un palier toutes les 10 vagues

        public WaveData Current => WaveBook.Make(Wave);   // infini : la formule ne s'arrête jamais
        public int BossCount => Current.BossCount;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            path = new List<Vector3>();
            foreach (var p in MapLayout.PathPoints())
                path.Add(transform.position + p + Vector3.up * balloonHeight);
            Lives = startLives;
            var end = new GameObject("Ecran de fin").AddComponent<EndScreen>();   // victoire / vague perdue, dans le décor de la carte
            end.transform.SetParent(transform, false);
            end.spawner = this;
            if (Levels.LaunchOnArrival)
            {
                Levels.LaunchOnArrival = false;
                StartWave();
            }
        }

        // LANCER, depuis le hub ou la carte : la vague part (la carte tourne aussi quand on est au hub).
        // Si la carte n'est pas encore chargée (tout début de partie), elle partira dès qu'elle le sera.
        public static void Launch()
        {
            if (Instance) Instance.StartWave();
            else Levels.LaunchOnArrival = true;
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
            Sfx.Play(Sound.WaveStart);

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
                Tutorial.WaveLost();   // le tableau du tutoriel dit de la relancer
                Sfx.Play(Sound.WaveLost);
                WaveFailed?.Invoke(Wave);
            }
            else
            {
                GameState.WavesWon++;
                GameState.BestWave = Mathf.Max(GameState.BestWave, Wave);
                int reward = 20 + Wave * 10;   // à équilibrer avec la banane (5) et le coffre (40 + 15 par vague vaincue)
                bool perfect = Lives == startLives;   // aucun ballon n'est passé
                if (perfect) reward += reward / 2;
                bool milestone = Wave % MilestoneEvery == 0;
                int bonus = milestone ? 50 * (Wave / MilestoneEvery) : 0;   // palier 10 : +50, palier 20 : +100…
                Economy.Earn(reward + bonus);
                Status = $"Vague {Wave} {(perfect ? "PARFAITE" : "gagnée")} : +{reward + bonus}";
                if (milestone)
                {
                    Sfx.Play(Sound.Victory);
                    MilestoneReached?.Invoke(Wave, bonus);
                }
                else Sfx.Play(Sound.WaveWon);
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

        // Un ballon est passé. Vague 1 du tutoriel : aucun ne doit passer, elle est ratée tout de suite.
        public void BalloonEscaped(int layers)
        {
            Lives = Tutorial.StrictWave ? 0 : Mathf.Max(0, Lives - layers);
            Sfx.Play(Sound.Leak);
        }
    }
}
