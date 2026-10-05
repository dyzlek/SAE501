using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Prendre les bananes de Maxens avec notre joueur (souris verrouillée, on vise au centre de l'écran) :
    // clic maintenu sur une banane = elle est en main, on la porte au-dessus du panier et on relâche.
    // Même logique que son TestSouris (Prise / Lachee), adaptée à la visée au centre. À remplacer par la main VR.
    public class BananaHand : MonoBehaviour
    {
        public float reach = 4f;
        public float holdDistance = 0.8f;   // la banane flotte devant la caméra
        public float follow = 20f;

        Camera cam;
        Banane held;
        int[] layers;
        Transform[] parts;
        const int IgnoreRaycast = 2;

        void Start() => cam = GetComponentInChildren<Camera>();

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !cam || Cursor.lockState != CursorLockMode.Locked) return;
            var ray = new Ray(cam.transform.position, cam.transform.forward);

            if (!held)
            {
                held = null;
                if (mouse.leftButton.wasPressedThisFrame
                    && Physics.Raycast(ray, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
                {
                    var b = hit.collider.GetComponentInParent<Banane>();
                    if (b && !b.Deposee) Take(b);
                }
                return;
            }

            // La banane suit la visée : posée sur ce qu'on vise (ex. au-dessus du panier), sinon devant soi
            var target = Physics.Raycast(ray, out var aim, reach, ~(1 << IgnoreRaycast), QueryTriggerInteraction.Ignore)
                ? aim.point + Vector3.up * 0.4f
                : ray.GetPoint(holdDistance);
            held.transform.position = Vector3.Lerp(held.transform.position, target, 1f - Mathf.Exp(-follow * Time.deltaTime));

            if (mouse.leftButton.wasReleasedThisFrame) Release();
        }

        void Take(Banane b)
        {
            held = b;
            held.Prise();
            // la banane tenue ne doit pas bloquer notre rayon de visée
            parts = held.GetComponentsInChildren<Transform>();
            layers = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++) { layers[i] = parts[i].gameObject.layer; parts[i].gameObject.layer = IgnoreRaycast; }
        }

        void Release()
        {
            if (held)
            {
                for (int i = 0; i < parts.Length; i++) if (parts[i]) parts[i].gameObject.layer = layers[i];
                held.Lachee(); // dans le panier si on est au-dessus, sinon elle retombe
            }
            held = null;
        }

        void OnDisable() => Release();
    }
}
