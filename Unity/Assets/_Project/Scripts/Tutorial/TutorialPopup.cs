using System.Collections;
using UnityEngine;

namespace SAE
{
    // Une annonce du tutoriel qui surgit devant le joueur : « BRAVO ! », « DOMMAGE ! », « BIEN POSÉ ! »...
    // Pour que les grands moments se sentent (on réussit, on rate), même sans regarder le tableau.
    // Elle est posée UNE fois dans le décor, à 2 m devant soi, puis flotte vers le haut et disparaît :
    // elle ne suit pas la tête (rien de collé au visage, règles de confort). Une seule à la fois.
    public class TutorialPopup : MonoBehaviour
    {
        const float Ahead = 2.2f;          // en mètres devant le joueur
        const float StaySeconds = 2.2f;
        const float PopTime = 0.3f;

        static TutorialPopup current;

        public static readonly Color Happy = new Color(1f, 0.82f, 0.2f);
        public static readonly Color Sad = new Color(1f, 0.35f, 0.3f);

        TextMesh label;
        Vector3 start;

        // size : la hauteur d'une ligne, en mètres. sound : le son qui l'accompagne (null : aucun)
        public static void Show(string text, Color color, float size = 0.3f, string sound = null)
        {
            var rig = PlayerRig.Local;
            if (!rig) return;
            if (current) Destroy(current.gameObject);
            var head = rig.HeadPosition;
            var forward = Vector3.ProjectOnPlane(rig.head ? rig.head.forward : Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            var go = new GameObject("Annonce du tutoriel");
            go.transform.position = head + forward.normalized * Ahead;
            current = go.AddComponent<TutorialPopup>();
            current.start = go.transform.position;
            current.label = Visuals.Label(go.transform, text, Vector3.zero, size, color, title: true);
            if (sound != null) Sounds.Play(sound, go.transform.position);
        }

        IEnumerator Start()
        {
            // Elle surgit en grossissant un peu trop, puis monte doucement, et rétrécit pour disparaître
            for (float t = 0f; t < StaySeconds; t += Time.deltaTime)
            {
                float pop = t < PopTime ? Mathf.Lerp(0f, 1.2f, t / PopTime) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((t - PopTime) / 0.2f));
                float end = Mathf.Clamp01((t - (StaySeconds - 0.3f)) / 0.3f);
                transform.localScale = Vector3.one * pop * (1f - end);
                transform.position = start + Vector3.up * (0.25f * t / StaySeconds);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy() { if (current == this) current = null; }
    }
}
