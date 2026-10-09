using UnityEngine;

namespace SAE
{
    // Un bouton rond d'un tableau du tutoriel : SUIVANT (étape suivante) ou PASSER (fin du tutoriel).
    // On l'enfonce avec la main (HandPress) ou de loin avec le rayon (RayPress). Il s'enfonce un instant.
    public class TutorialButton : MonoBehaviour, IPressable
    {
        public enum Action { Next, Skip }

        public Action action;
        public Transform cap;              // la partie qui s'enfonce

        Vector3 rest;
        float pressed;

        void Start() { if (cap) rest = cap.localPosition; }

        void Update()
        {
            if (!cap) return;
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            cap.localPosition = rest + Vector3.down * (0.012f * pressed);
        }

        public void Press()
        {
            pressed = 1f;
            if (action == Action.Next) Tutorial.Next();
            else Tutorial.Finish();
        }
    }
}
