using UnityEngine;

namespace SAE
{
    // Le bout du doigt d'une manette : une petite sphère qui enfonce ce qu'elle touche (IPressable),
    // avec une vibration pour confirmer. Sphère en trigger + Rigidbody cinématique : c'est ce qu'il faut
    // pour que Unity appelle OnTriggerEnter quand elle entre dans le collider d'un bouton.
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class HandPress : MonoBehaviour
    {
        public float cooldown = 0.4f;   // en secondes : un seul appui même si la main reste dans le bouton

        float nextPress;

        void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;      // c'est la manette qui la déplace, pas la physique
            rb.useGravity = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time < nextPress) return;
            var target = other.GetComponentInParent<IPressable>();
            if (target == null) return;

            nextPress = Time.time + cooldown;
            bool done = Tutorial.TryPress(target);   // pendant le tutoriel, seul ce qu'il demande est permis
            PlayerRig.Buzz(transform, done ? 0.6f : 0.15f);
        }
    }
}
