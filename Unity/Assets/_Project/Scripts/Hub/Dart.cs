using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Une fléchette à lancer sur la cible du mur (DartBoard). On la prend (XR Grab, de près ou de loin),
    // on la lance (la vitesse de la main, ThrowVelocity) : elle vole pointe en avant et se plante si elle touche la cible.
    // Quelques secondes après, elle revient toute seule sur son présentoir : on ne ramasse rien par terre (confort VR).
    // La pointe est vers +Z.
    [RequireComponent(typeof(Rigidbody))]
    public class Dart : MonoBehaviour, IThrowable
    {
        const float ReturnDelay = 3f;     // en secondes, après s'être plantée ou posée
        const float LostDelay = 6f;       // en secondes : partie trop loin, elle revient quand même
        const float Length = 0.18f;       // en mètres

        public Vector3 home;              // sa place sur le présentoir
        public Quaternion homeRotation;

        Rigidbody body;
        XRGrabInteractable grab;
        ThrowVelocity handSpeed;
        bool flying;
        Vector3 throwVelocity;
        float returnAt = -1f;

        // Fabrique une fléchette (fût en bois, pointe en fer, ailettes rouges) posée en 'position'
        public static Dart Create(Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("Fléchette");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            Visuals.Box("Fût", go.transform, new Vector3(0, 0, -0.01f), new Vector3(0.012f, 0.012f, 0.12f), new Color(0.45f, 0.3f, 0.18f));
            Visuals.Box("Pointe", go.transform, new Vector3(0, 0, 0.07f), new Vector3(0.004f, 0.004f, 0.04f), new Color(0.6f, 0.6f, 0.62f));
            Visuals.Box("Ailette", go.transform, new Vector3(0, 0, -0.07f), new Vector3(0.05f, 0.002f, 0.04f), new Color(0.85f, 0.15f, 0.12f));
            Visuals.Box("Ailette", go.transform, new Vector3(0, 0, -0.07f), new Vector3(0.002f, 0.05f, 0.04f), new Color(0.85f, 0.15f, 0.12f));

            var col = go.AddComponent<CapsuleCollider>();   // avant le XR Grab : il récupère les colliders à sa création
            col.direction = 2;                               // le long de Z
            col.radius = 0.05f;   // plus large que la fléchette : facile à viser et à attraper
            col.height = Length;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // petite et rapide
            var g = go.AddComponent<XRGrabInteractable>();
            g.throwOnDetach = false;                         // c'est Dart qui la lance (voir ThrowVelocity)
            g.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Near;   // prise de loin, elle vient dans la main
            go.AddComponent<GrabReach>();
            var dart = go.AddComponent<Dart>();
            dart.home = position;
            dart.homeRotation = rotation;
            return dart;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            handSpeed = gameObject.AddComponent<ThrowVelocity>();
            gameObject.AddComponent<ThrowTrail>();
            PlayerRig.IgnoreCollisions(gameObject);
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }

        void OnGrab(SelectEnterEventArgs args)
        {
            Grab();
            PlayerRig.Buzz(args.interactorObject.transform, 0.2f);
        }

        void OnRelease(SelectExitEventArgs args) => Throw(handSpeed.Velocity);

        // Prise dans la main (VR) ou au clic (mode PC)
        public void Grab()
        {
            handSpeed.Clear();
            flying = false;
            returnAt = -1f;
            body.isKinematic = true;
        }

        // Lancée avec cette vitesse (la physique prend le relais au prochain pas, voir FixedUpdate)
        public void Throw(Vector3 velocity)
        {
            throwVelocity = velocity;
            flying = true;
            returnAt = Time.time + LostDelay;
        }

        void FixedUpdate()
        {
            if (!flying) return;
            if (body.isKinematic)   // XRI la remet comme avant (cinématique) au lâcher : on la rend à la physique
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = throwVelocity;
            }
            if (body.linearVelocity.sqrMagnitude > 1f) body.MoveRotation(Quaternion.LookRotation(body.linearVelocity));   // pointe en avant
        }

        // Elle touche quelque chose : la cible (elle s'y plante et marque) ou autre chose (elle tombe)
        void OnCollisionEnter(Collision collision)
        {
            if (!flying) return;
            flying = false;
            returnAt = Time.time + ReturnDelay;
            var board = collision.collider.GetComponent<DartBoard>();
            if (!board) return;
            var point = collision.GetContact(0).point;
            body.isKinematic = true;
            transform.position = point - transform.forward * (Length * 0.4f);   // la pointe enfoncée dans la cible
            board.Hit(point);
        }

        void Update()
        {
            if (returnAt > 0f && Time.time > returnAt && !(grab && grab.isSelected)) ReturnHome();
        }

        void ReturnHome()
        {
            returnAt = -1f;
            flying = false;
            body.isKinematic = true;
            body.useGravity = false;
            transform.SetPositionAndRotation(home, homeRotation);
        }
    }
}
