using UnityEngine;

namespace SAE
{
    // Fait avancer le tutoriel (Tutorial.Tick) et promène la flèche dorée au-dessus de ce qu'il faut toucher.
    // Un seul, dans la scène Hub (toujours chargée : c'est celle du joueur).
    // La flèche : un chevron doré pointé vers le bas, qui rebondit et tourne doucement, avec une lueur qui pulse.
    public class TutorialRunner : MonoBehaviour
    {
        public Transform pointer;          // la flèche (construite par PrototypeGenerator)

        const float BounceHeight = 0.08f, BounceSpeed = 4f, SpinSpeed = 90f;

        void Update()
        {
            Tutorial.Tick();
            if (!pointer) return;

            var target = Tutorial.Active ? TutorialTarget.Find(Tutorial.Spot(Tutorial.Current)) : null;
            bool show = target && target.level == Levels.Current;
            if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            if (!show) return;

            float bounce = Mathf.Abs(Mathf.Sin(Time.time * BounceSpeed)) * BounceHeight;
            pointer.position = target.transform.position + Vector3.up * (target.height + bounce);
            pointer.rotation = Quaternion.Euler(0f, Time.time * SpinSpeed, 0f);
        }
    }
}
