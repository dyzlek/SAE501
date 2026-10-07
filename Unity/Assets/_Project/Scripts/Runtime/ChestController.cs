using System.Collections;
using UnityEngine;

namespace Sae501.Coffres
{
    // Le coffre : vérifie la money, tire la rareté, joue l'animation du modèle puis la roulette.
    // Interact() est la seule porte d'entrée : la souris (DesktopInteractor) l'appelle aujourd'hui,
    // un XR Simple Interactable (selectEntered) pourra l'appeler en VR sans rien changer d'autre.
    public class ChestController : MonoBehaviour
    {

        public RouletteView roulette;
        public ChestPrompt prompt;

        [Header("Règles")]
        // Le coffre suit la progression du joueur : plus on a vaincu de vagues, plus il est cher,
        // mais meilleur (raretés débloquées, meilleures chances). Payé avec l'argent commun (SAE.Economy).
        public int basePrice = 25;
        public int pricePerWave = 20;
        public int Progress => SAE.GameState.WavesWon;
        public int Price => basePrice + pricePerWave * Progress;
        public const int MonkeysPerChest = 1;   // toujours un seul singe : ce sont ses chances d'être rare qui montent avec les vagues
        // Raretés obtenues au dernier coffre (l'inventaire viendra les récupérer)
        public System.Collections.Generic.List<Rarity> LastResults { get; } = new System.Collections.Generic.List<Rarity>();
        // Type de singe de chaque résultat (index de SAE.MonkeyType), tiré en même temps que la rareté pour que la
        // roulette montre le vrai singe gagné. C'est ChestReward qui fournit le tirage (ses chances par vague).
        public System.Collections.Generic.List<int> LastTypes { get; } = new System.Collections.Generic.List<int>();
        public System.Func<int> rollType;
        public event System.Action<System.Collections.Generic.List<Rarity>> Opened;
        public ChestOddsSettings oddsSettings = new ChestOddsSettings();

        [Header("Timing (secondes)")]
        public float spinDelay = 1f;     // attente après le début de l'animation du coffre
        public float spinDuration = 5f;  // durée de la roulette
        public float hideDelay = 3f;     // la roulette disparaît 3 s après le résultat

        [Header("Interaction")]
        public float interactDistance = 3.5f; // distance max du joueur pour ouvrir

        public int OpenedCount { get; private set; }
        public bool IsBusy { get; private set; }

        Animation anim;
        string clipName;
        SAE.ChestLid lid;   // le coffre de la cabane : boing, couvercle et aura dorée (sinon, l'animation du modèle)
        Coroutine hideRoutine;

        public bool IsInRange(Vector3 playerPosition) =>
            Vector3.Distance(playerPosition, transform.position) <= interactDistance;

        public float[] CurrentOdds() => ChestOdds.Compute(oddsSettings, Progress);

        void Awake()
        {
            lid = GetComponent<SAE.ChestLid>();
            anim = GetComponentInChildren<Animation>();
            if (lid) { }
            else if (anim != null)
            {
                anim.playAutomatically = false;
                clipName = PickClip();
            }
            else
            {
                Debug.LogWarning("Aucun composant Animation sur le coffre : l'animation ne sera pas jouée. " +
                                 "Vérifier l'import du .glb (glTFast, Animation Method = Legacy).", this);
            }
            EnsureCollider();
        }

        public void Interact()
        {
            if (IsBusy) return;

            if (!SAE.Economy.TrySpend(Price, transform.position))
            {
                prompt.ShowError($"Pas assez d'argent ({SAE.Economy.Money}/{Price})");
                return;
            }
            StartCoroutine(OpenRoutine());
        }

        public void ResetOpenedCount() => OpenedCount = 0;

        IEnumerator OpenRoutine()
        {
            IsBusy = true;
            if (hideRoutine != null) StopCoroutine(hideRoutine); // une roulette encore affichée est reprise
            roulette.ShowMessage("", Color.white);
            roulette.SetVisible(true);

            var odds = CurrentOdds();            // probabilités AVANT cette ouverture
            // Résultats décidés d'avance (un par singe), la roulette ne fait que montrer le meilleur
            LastResults.Clear();
            LastTypes.Clear();
            for (int i = 0; i < MonkeysPerChest; i++)
            {
                LastResults.Add(ChestOdds.Roll(odds));
                LastTypes.Add(rollType != null ? rollType() : 0);
            }
            int best = 0;
            for (int i = 1; i < LastResults.Count; i++) if (LastResults[i] > LastResults[best]) best = i;
            Rarity result = LastResults[best];

            PlayChestAnimation();
            yield return new WaitForSeconds(spinDelay);
            yield return roulette.Spin(result, odds, spinDuration, LastTypes[best], rollType);

            OpenedCount++;
            roulette.ShowMessage(LastResults.Count == 1 ? result.ToString().ToUpper()
                                                        : $"{result.ToString().ToUpper()}  (+{LastResults.Count - 1})",
                                 RarityInfo.ColorOf(result));
            Opened?.Invoke(LastResults);
            IsBusy = false;
            hideRoutine = StartCoroutine(HideRouletteAfter(hideDelay));
        }

        IEnumerator HideRouletteAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            roulette.SetVisible(false);
            PlayChestAnimation(reverse: true); // le coffre se referme quand la roulette disparaît
            hideRoutine = null;
        }

        // Ouvre le coffre (animation normale) ou le referme (même animation à l'envers).
        void PlayChestAnimation(bool reverse = false)
        {
            if (lid) { if (reverse) lid.Close(); else lid.Open(); return; }
            if (anim == null || string.IsNullOrEmpty(clipName)) return;
            var state = anim[clipName];
            state.wrapMode = WrapMode.ClampForever; // le coffre reste ouvert pendant la roulette
            state.time = reverse ? state.length : 0f;
            state.speed = reverse ? -1f : 1f;
            anim.Play(clipName);
        }

        // Clip dont le nom contient "open" si possible, sinon le premier.
        string PickClip()
        {
            string first = null;
            foreach (AnimationState s in anim)
            {
                Debug.Log("Clip du coffre : " + s.name, this);
                if (first == null) first = s.name;
                if (s.name.ToLower().Contains("open")) return s.name;
            }
            return first;
        }

        // Un clic souris a besoin d'un collider : on en ajoute un autour du modèle s'il n'y en a pas.
        void EnsureCollider()
        {
            if (GetComponentInChildren<Collider>() != null) return;
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(b.center);
            box.size = new Vector3(b.size.x / transform.lossyScale.x, b.size.y / transform.lossyScale.y, b.size.z / transform.lossyScale.z);
        }
    }
}
