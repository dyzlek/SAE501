using System;
using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Le tutoriel intégré au début du jeu, présenté par Pat Fusty (le guide de Bloons) : UNE consigne à la fois,
    // écrite sur son ardoise dans la cabane, et une flèche dorée qui bondit au-dessus de l'objet à utiliser.
    // Chaque étape se valide toute seule quand le joueur l'a faite (on regarde l'état du jeu, pas de bouton « suivant »),
    // avec un carillon et une vibration. Une étape déjà faite est sautée. Le niveau 1 est le tutoriel (méthode VR-first).
    public class Tutorial : MonoBehaviour
    {
        public TextMesh text;              // l'ardoise de Pat
        public Transform arrow;            // la flèche qui montre quoi utiliser
        public Bananier bananier;
        public Panier panier;
        public ChestController chest;
        public Transform stall, library, board, launchButton;

        public static bool Done { get; private set; }   // fini une fois : REJOUER ne le remontre pas

        // Sans rechargement du domaine (Enter Play Mode rapide), les statiques survivent d'une partie à l'autre : on repart de zéro
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDone() => Done = false;

        struct Step
        {
            public string text;
            public Func<bool> done;
            public Transform target;
            public Step(string text, Func<bool> done, Transform target) { this.text = text; this.done = done; this.target = target; }
        }

        Step[] steps;
        int current;
        float endAt;                        // l'heure où le dernier message s'efface

        const float ArrowHeight = 0.5f;     // en mètres, au-dessus du haut de l'objet montré
        const float FinalMessageTime = 12f; // en secondes

        void Start()
        {
            steps = new[]
            {
                new Step("Salut ! Prends une banane\nsur l'étal derrière toi\n(serre le grip)", () => AnyBananaInHand() || panier.NbBananes > 0, stall),
                new Step("Lance-la dans le panier :\nlâche-la en bougeant la main", () => panier.NbBananes > 0, panier.transform),
                new Step($"Chaque banane rapporte de l'argent.\nRécolte de quoi ouvrir le coffre\n({chest.Price} bananes)", () => Economy.CanAfford(chest.Price) || chest.OpenedCount > 0, panier.transform),
                new Step("Touche le coffre pour l'ouvrir :\nil te donne un singe !", () => chest.OpenedCount > 0, chest.transform),
                new Step("Prends un singe dans la bibliothèque\net pose-le sur le plateau\n(ou lance-le dessus !)", () => GameState.Placed.Count > 0, library),
                new Step("Les ballons arrivent !\nAppuie sur LANCER", () => GameState.WavesWon > 0 || (WaveSpawner.Instance && (WaveSpawner.Instance.Running || WaveSpawner.Instance.Wave > 1)), launchButton),
            };
            if (Done) Finish(false);
            else Show();
        }

        void Update()
        {
            if (Done)
            {
                if (endAt > 0f && Time.time > endAt) { gameObject.SetActive(false); }
                return;
            }
            // Étapes déjà faites : on passe à la suivante (plusieurs d'un coup si besoin)
            bool advanced = false;
            while (current < steps.Length && steps[current].done()) { current++; advanced = true; }
            if (advanced)
            {
                Celebrate();
                if (current >= steps.Length) { Finish(true); return; }
                Show();
            }
            MoveArrow();
        }

        void Show()
        {
            text.text = $"<color=#FFD45A>PAT FUSTY</color>  <size=28>({current + 1}/{steps.Length})</size>\n{steps[current].text}";
        }

        // La flèche bondit au-dessus de l'objet montré et tourne sur elle-même : on la voit de partout
        void MoveArrow()
        {
            var target = steps[current].target;
            arrow.gameObject.SetActive(target);
            if (!target) return;
            float top = TopOf(target);
            arrow.position = new Vector3(target.position.x, top + ArrowHeight + 0.1f * Mathf.Sin(Time.time * 4f), target.position.z);
            arrow.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        }

        static float TopOf(Transform target)
        {
            float top = target.position.y;
            foreach (var r in target.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            return Mathf.Min(top, target.position.y + 2.5f);   // pas au plafond pour un grand meuble
        }

        void Celebrate()
        {
            Sfx.Play(Sfx.Sound.Chime, text.transform.position);
            if (PlayerRig.Local)
            {
                PlayerRig.Buzz(PlayerRig.Local.leftHand, 0.4f);
                PlayerRig.Buzz(PlayerRig.Local.rightHand, 0.4f);
            }
        }

        void Finish(bool justNow)
        {
            Done = true;
            arrow.gameObject.SetActive(false);
            text.text = "<color=#FFD45A>PAT FUSTY</color>\nBravo, tu sais tout !\nVa défendre la carte : SE TP.\nLes commandes : lève ta main gauche.";
            endAt = justNow ? Time.time + FinalMessageTime : Time.time;   // l'ardoise disparaît ensuite
        }

        bool AnyBananaInHand()
        {
            foreach (var b in bananier.BananesAuSol) if (b && b.EnMain) return true;
            return false;
        }
    }
}
