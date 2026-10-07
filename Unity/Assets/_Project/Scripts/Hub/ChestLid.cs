using System.Collections;
using UnityEngine;

namespace SAE
{
    // L'ouverture du coffre de la cabane (modèle Art/Coffre/Coffre.glb, couvercle séparé sur sa charnière) :
    // un petit « boing » (le coffre s'écrase puis rebondit), le couvercle s'ouvre, et une aura dorée s'allume.
    // Le coffre reste à sa place (il ne tourne plus vers le joueur). ChestController appelle Open() et Close().
    // Quand le joueur a assez d'argent pour l'ouvrir, le coffre se trémousse de temps en temps pour l'inviter à le faire.
    public class ChestLid : MonoBehaviour
    {
        public Sae501.Coffres.ChestController chest;
        public Transform lid;              // Coffre_Couvercle : son origine est sur la charnière
        public Transform glowAnchor;       // le centre du coffre, où s'allume l'aura
        public Vector3 glowSize = new Vector3(0.8f, 0.5f, 0.5f);   // taille de l'aura (largeur, hauteur, profondeur), en mètres
        public float openAngle = -110f;    // rotation du couvercle autour de son axe X, en degrés
        public float boingTime = 0.3f;     // secondes
        public float openTime = 0.45f;
        public float boingStrength = 0.15f;
        public float wiggleEvery = 2.5f;   // secondes entre deux trémoussements (quand on peut l'ouvrir)
        public float wiggleTime = 0.5f;
        public float wiggleAngle = 4f;     // degrés

        static readonly Color Gold = new Color(1f, 0.78f, 0.2f);

        Vector3 restScale;
        Quaternion closedRotation;
        Aura glow;
        Quaternion restRotation;
        float nextWiggle;
        bool isOpen;

        void Awake()
        {
            restScale = transform.localScale;
            closedRotation = lid.localRotation;
            restRotation = transform.localRotation;
        }

        // Le petit appel : seulement s'il est fermé, au repos, et que le joueur peut payer
        void Update()
        {
            if (isOpen || !chest || chest.IsBusy || !SAE.Economy.CanAfford(chest.Price))
            {
                nextWiggle = Time.time + 0.5f;
                return;
            }
            if (Time.time < nextWiggle) return;
            nextWiggle = Time.time + wiggleEvery;
            StartCoroutine(Wiggle());
        }

        // Il se balance de gauche à droite, de moins en moins, en sautillant un peu
        IEnumerator Wiggle()
        {
            for (float t = 0f; t < wiggleTime && !isOpen; t += Time.deltaTime)
            {
                float k = t / wiggleTime;
                float side = Mathf.Sin(k * Mathf.PI * 4f) * (1f - k) * wiggleAngle;
                transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, side);
                yield return null;
            }
            transform.localRotation = restRotation;
        }

        public bool IsOpen => isOpen;

        public void Open()
        {
            isOpen = true;
            StopAllCoroutines();
            transform.localRotation = restRotation;
            StartCoroutine(OpenRoutine());
        }

        public void Close()
        {
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator OpenRoutine()
        {
            // Boing : le coffre s'écrase un peu (plus large, moins haut) puis rebondit, en gardant son volume
            for (float t = 0f; t < boingTime; t += Time.deltaTime)
            {
                float k = t / boingTime;
                float squash = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * boingStrength;   // -> écrasé, étiré, repos
                transform.localScale = Vector3.Scale(restScale, new Vector3(1f + squash, 1f - squash, 1f + squash));
                yield return null;
            }
            transform.localScale = restScale;

            // L'aura dorée s'allume pendant que le couvercle s'ouvre
            if (!glow) glow = Aura.Add(glowAnchor.gameObject, Gold, glowSize);
            if (glow) glow.gameObject.SetActive(true);
            yield return TurnLid(openAngle, openTime);
        }

        IEnumerator CloseRoutine()
        {
            if (glow) glow.gameObject.SetActive(false);
            yield return TurnLid(0f, openTime);
            isOpen = false;
        }

        // Tourne le couvercle de sa position actuelle jusqu'à 'angle', en ralentissant à la fin.
        IEnumerator TurnLid(float angle, float duration)
        {
            var from = lid.localRotation;
            var to = closedRotation * Quaternion.Euler(angle, 0f, 0f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                lid.localRotation = Quaternion.Slerp(from, to, k);
                yield return null;
            }
            lid.localRotation = to;
        }
    }
}
