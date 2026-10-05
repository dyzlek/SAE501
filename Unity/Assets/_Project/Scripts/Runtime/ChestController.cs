using System.Collections;
using UnityEngine;

namespace Sae501.Coffres
{
    // Le coffre : vérifie la money, tire la rareté, joue l'animation du modèle puis la roulette.
    // Interact() est la seule porte d'entrée : la souris (DesktopInteractor) l'appelle aujourd'hui,
    // un XR Simple Interactable (selectEntered) pourra l'appeler en VR sans rien changer d'autre.
    public class ChestController : MonoBehaviour
    {
        public Wallet wallet;
        public RouletteView roulette;
        public ChestPrompt prompt;

        [Header("Règles")]
        public int requiredMoney = 5; // sous cette valeur : erreur. Ouvrir ne coûte rien.
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
        Coroutine hideRoutine;

        public bool IsInRange(Vector3 playerPosition) =>
            Vector3.Distance(playerPosition, transform.position) <= interactDistance;

        public float[] CurrentOdds() => ChestOdds.Compute(oddsSettings, OpenedCount);

        void Awake()
        {
            anim = GetComponentInChildren<Animation>();
            if (anim != null)
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

            if (wallet.Money < requiredMoney)
            {
                string msg = $"Pas assez de money ({wallet.Money}/{requiredMoney})";
                Debug.LogError(msg, this);
                prompt.ShowError(msg);
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
            Rarity result = ChestOdds.Roll(odds); // résultat décidé d'avance, la roulette ne fait que l'afficher

            PlayChestAnimation();
            yield return new WaitForSeconds(spinDelay);
            yield return roulette.Spin(result, odds, spinDuration);

            OpenedCount++;
            roulette.ShowMessage(result.ToString().ToUpper(), RarityInfo.ColorOf(result));
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
