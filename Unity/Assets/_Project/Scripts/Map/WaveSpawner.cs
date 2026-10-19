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
    // 10 vagues, un palier donne un bonus ; une vague sans aucun ballon passé est « parfaite », et les vagues parfaites
    // d'affilée font monter un combo (+50 % par cran). Les vagues de boss et spéciales sont annoncées (cloches, musique,
    // panneau) et ont leur météo (WaveWeather).
    // Règle du GDD : éclater un ballon ne rapporte rien, finir une vague rapporte de l'argent.
    // Rien n'est affiché à l'écran (nausée en VR) : les tableaux WaveBoard du décor lisent Wave, Lives et Status.
    public class WaveSpawner : MonoBehaviour
    {
        // Les vagues de la scène Labyrinthe (null quand on est au hub)
        public static WaveSpawner Instance { get; private set; }

        public float balloonHeight = 1f;
        public int startLives = 20;

        List<Vector3> path;
        public IReadOnlyList<Vector3> Path => path;   // le chemin des ballons (piège à clous)

        public int Wave { get; private set; } = 1;   // la vague en cours, ou la prochaine à lancer
        public int Lives { get; private set; }
        public bool Running { get; private set; }
        public string Status { get; private set; } = "Prépare-toi, puis appuie sur LANCER";

        // Pour le panneau d'annonce (EndScreen) : la vague qu'on vient de perdre, et le palier qu'on vient d'atteindre (vague, bonus)
        public event System.Action<int> WaveFailed;
        public event System.Action<int, int> MilestoneReached;
        // Vague annoncée qui démarre (#133) : boss ou spéciale. Et un combo de vagues parfaites qui monte (#132).
        public event System.Action<int> SpecialWaveStarted;
        public event System.Action<int, int> ComboReached;   // (combo, bonus)

        public const int MilestoneEvery = 10;   // un palier toutes les 10 vagues

        // Combo (#132) : vagues parfaites d'affilée. Chaque vague parfaite ajoute +50 % de la récompense par cran
        // de combo (x1 : +50 %, x2 : +100 %…), jusqu'à MaxCombo. Un seul ballon passé, ou une vague perdue : retour à 0.
        public const int MaxCombo = 5;
        public int Combo { get; private set; }

        WaveWeather weather;

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
            weather = gameObject.AddComponent<WaveWeather>();   // brouillard, nuit, pluie des vagues annoncées
            BombRack.Build(transform);                          // les bombes à lancer, à côté du point d'arrivée
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
            weather.Set(WaveBook.WeatherOf(Wave));
            if (WaveBook.IsAnnounced(Wave))
            {
                // Cloches + musique plus tendue pendant toute la vague, et le panneau d'annonce
                Sfx.Play(Sound.Bell);
                Sfx.SetTension(true);
                Status = WaveBook.IsSpecial(Wave) ? "VAGUE SPÉCIALE !" : "Un boss arrive !";
                SpecialWaveStarted?.Invoke(Wave);
                yield return new WaitForSeconds(2f);   // on laisse sonner les cloches avant les premiers ballons
            }
            else Sfx.Play(Sound.WaveStart);

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

            weather.Set(Weather.Clair);   // le jour revient entre les vagues
            Sfx.SetTension(false);

            if (Lives <= 0)
            {
                // Perdu : on efface les ballons et on remet la vague à zéro (même numéro, toutes les vies)
                foreach (var b in new List<Balloon>(Balloon.All)) Destroy(b.gameObject);
                Lives = startLives;
                Combo = 0;
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
                Combo = perfect ? Mathf.Min(Combo + 1, MaxCombo) : 0;
                int comboBonus = reward / 2 * Combo;
                reward += comboBonus;
                GameState.BestCombo = Mathf.Max(GameState.BestCombo, Combo);
                bool milestone = Wave % MilestoneEvery == 0;
                int bonus = milestone ? 50 * (Wave / MilestoneEvery) : 0;   // palier 10 : +50, palier 20 : +100…
                Economy.Earn(reward + bonus);
                Records.WaveWon();   // classement et trophées, enregistrés sur le casque
                Status = perfect ? $"Vague {Wave} PARFAITE · COMBO x{Combo} : +{reward + bonus}" : $"Vague {Wave} gagnée : +{reward + bonus}";
                if (Combo >= 2 && !milestone) ComboReached?.Invoke(Combo, comboBonus);   // le palier a déjà son panneau
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
