using UnityEngine;

namespace SAE
{
    // Un tableau du tutoriel, dans le décor (pas collé au visage) : un au hub, un dans la bananeraie, un sur la carte.
    // Tous montrent la même étape (Tutorial.Current). À chaque nouvelle étape :
    //   - le titre surgit (il grossit un peu trop puis se pose), en doré ;
    //   - la consigne s'écrit lettre par lettre, comme à la craie ;
    //   - une gerbe de petits ballons colorés éclate derrière le tableau ;
    //   - SUIVANT n'apparaît que sur les étapes à lire.
    // Le tutoriel fini, le tableau disparaît (visual éteint).
    public class TutorialBoard : MonoBehaviour
    {
        public GameObject visual;          // tout ce qui se voit (ardoise, textes, boutons) : éteint à la fin
        public TextMesh title;
        public TextMesh body;
        public TextMesh progress;          // « 3 / 12 »
        public GameObject nextButton;      // SUIVANT
        public ParticleSystem confetti;

        const float LettersPerSecond = 45f;
        const float PopTime = 0.45f;

        float shownAt;                     // moment où l'étape s'est affichée
        string fullBody = "";
        Vector3 titleScale;

        void Awake() => titleScale = title.transform.localScale;

        void OnEnable()
        {
            Tutorial.Changed += Show;
            Show();
        }

        void OnDisable() => Tutorial.Changed -= Show;

        void Show()
        {
            bool on = Tutorial.Active;
            visual.SetActive(on);
            if (!on) return;
            var step = Tutorial.Current;
            title.text = Tutorial.Title(step);
            fullBody = Tutorial.Body(step);
            body.text = "";
            progress.text = $"{(int)step + 1} / {(int)Tutorial.Step.Done}";
            nextButton.SetActive(Tutorial.NeedsNext(step));
            shownAt = Time.time;
            if (confetti) confetti.Play();
        }

        void Update()
        {
            if (!visual.activeSelf) return;
            float t = Time.time - shownAt;

            // Le titre surgit : de 0 à 115 %, puis revient à 100 %
            float k = Mathf.Clamp01(t / PopTime);
            float pop = k < 0.7f ? Mathf.Lerp(0f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
            title.transform.localScale = titleScale * pop;

            // La consigne s'écrit petit à petit
            int letters = Mathf.Min(fullBody.Length, Mathf.FloorToInt(t * LettersPerSecond));
            if (body.text.Length != letters) body.text = fullBody.Substring(0, letters);
        }
    }
}
