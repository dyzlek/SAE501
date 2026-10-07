using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Un singe qu'on prend à la main sur l'étagère (XR Grab Interactable en VR, clic en mode PC).
    // Chaque case de la bibliothèque qui possède au moins un singe en pose un sur elle (voir LibrarySlot).
    //   - le saisir le sort de l'inventaire (GameState.Held) ;
    //   - pendant qu'on le tient, un rayon part de la main : là où il touche le plateau ou la carte (même de loin),
    //     le plateau montre l'aperçu vert/rouge et le halo de fusion ;
    //   - le lâcher le pose à cet endroit, ou le fusionne avec le singe identique qui s'y trouve ;
    //   - le lâcher sans viser le plateau ni la carte le range dans la bibliothèque ;
    //   - le LANCER (lâché avec la main en mouvement) : il vole, et se pose (ou fusionne) là où il atterrit sur le plateau
    //     ou la carte ; s'il tombe ailleurs, il retourne dans la bibliothèque (idée de Maxens).
    public class MonkeyToken : MonoBehaviour
    {
        const float AimReach = 30f;          // portée du rayon de pose, en mètres (assez pour viser la carte de loin)
        const float DropReach = 1.5f;        // sinon, on cherche le plateau juste sous le singe
        const float HoldDistance = 0.12f;    // le singe tenu flotte à 12 cm devant la main
        const float ThrowSpeed = 1.5f;       // en m/s : lâché plus vite que ça, c'est un lancer (sinon, une pose)
        const float FlightTime = 4f;         // en secondes : sans atterrissage d'ici là, il retourne dans la bibliothèque

        // Le singe tenu en main (un seul à la fois, comme GameState.Held), ou en train de voler après un lancer.
        public static MonkeyToken Held { get; private set; }

        Monkey monkey;
        LibrarySlot slot;
        XRGrabInteractable grab;
        Rigidbody body;
        ThrowVelocity handSpeed;             // la vitesse de la main, pour le lancer
        Transform hand;                      // la main (l'interacteur) qui le tient : le rayon part d'elle
        LineRenderer ray;                    // le rayon visible, de la main jusqu'au point visé
        bool flying;                         // lancé, pas encore atterri
        Vector3 throwVelocity;               // l'élan à donner au premier pas de physique après le lâcher
        float landBy;                        // l'heure limite d'atterrissage
        static readonly RaycastHit[] hits = new RaycastHit[16];

        // Construit le singe à saisir : le cube du singe, un collider, un Rigidbody (exigé par XR Grab) et le XR Grab.
        public static MonkeyToken Create(LibrarySlot slot, Monkey monkey, Vector3 localPosition, float size)
        {
            var piece = Visuals.MonkeyPiece(monkey, slot.transform, localPosition, size, withLabel: false);
            piece.name = $"Singe à saisir {monkey}";
            // Visuals retire les colliders du cube en fin d'image (Destroy) : on le fait tout de suite,
            // sinon le XR Grab garderait un collider détruit.
            foreach (var c in piece.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
            piece.AddComponent<BoxCollider>().size = Vector3.one * size;   // avant le XR Grab : il récupère les colliders à sa création
            PlayerRig.IgnoreCollisions(piece);   // on ne peut pas monter sur le singe tenu et s'envoler

            var rb = piece.AddComponent<Rigidbody>();
            rb.isKinematic = true;     // il attend sur l'étagère sans tomber
            rb.useGravity = false;

            var grab = piece.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.useDynamicAttach = false;  // il se cale directement dans la main, quel que soit l'endroit visé
            grab.farAttachMode = InteractableFarAttachMode.Near;   // pris de loin, il vient jusqu'à la main (au lieu de rester au bout du rayon)
            // Tenu juste devant les doigts de Quincy, pas dans la main : un point d'accroche décalé vers l'arrière du singe
            var attach = new GameObject("Point de prise").transform;
            attach.SetParent(piece.transform, false);
            attach.localPosition = new Vector3(0f, 0f, -HoldDistance);
            grab.attachTransform = attach;
            piece.AddComponent<GrabReach>();  // on peut l'attraper de loin, jusqu'à 6 m (GrabReach.Reach)

            var token = piece.AddComponent<MonkeyToken>();
            token.monkey = monkey;
            token.slot = slot;
            token.grab = grab;
            token.body = rb;
            token.handSpeed = piece.AddComponent<ThrowVelocity>();
            piece.AddComponent<ThrowTrail>();   // la traînée dorée quand il vole
            token.Listen(true);
            return token;
        }

        void Listen(bool on)
        {
            if (!grab) return;
            if (on)
            {
                grab.selectEntered.AddListener(OnGrab);
                grab.selectExited.AddListener(OnRelease);
            }
            else
            {
                grab.selectEntered.RemoveListener(OnGrab);
                grab.selectExited.RemoveListener(OnRelease);
            }
        }

        void OnEnable() => Listen(true);
        void OnDisable() => Listen(false);

        // Une seule main peut tenir un singe : les autres singes ne sont pas saisissables pendant ce temps.
        void Update()
        {
            grab.enabled = !flying && (Held == null || Held == this);
            if (Held == this && !flying) DrawRay();
            if (flying && (Time.time > landBy || transform.position.y < FallGuard.FallHeight)) Land(null, default);   // perdu
        }

        // Juste après le lâcher, XRI remet le Rigidbody comme il était (cinématique) : on le rend à la physique
        // au pas suivant, et on lui donne l'élan de la main.
        void FixedUpdate()
        {
            if (!flying || throwVelocity == Vector3.zero) return;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = throwVelocity;
            throwVelocity = Vector3.zero;
        }

        // Il touche quelque chose en volant : s'il est sur le plateau ou la carte, il s'y pose (ou fusionne)
        void OnCollisionEnter(Collision collision)
        {
            if (!flying) return;
            Land(collision.collider.GetComponentInParent<PlacementSurface>(), collision.GetContact(0).point);
        }

        // Lancé : il part de la main avec sa vitesse, sans rayon ni aperçu ; il est toujours « tenu » (Held) jusqu'à
        // l'atterrissage, pour qu'aucun autre singe ne soit pris entre-temps.
        void Throw(Vector3 velocity)
        {
            flying = true;
            throwVelocity = velocity;
            landBy = Time.time + FlightTime;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // rapide : sans ça, il traverserait le plateau
            if (ray) ray.enabled = false;
        }

        // Fin du vol : posé sur 'surface' en 'point' si c'est possible, sinon rangé dans la bibliothèque
        void Land(PlacementSurface surface, Vector3 point)
        {
            flying = false;
            Held = null;
            bool placed = surface && surface.Drop(point);
            if (!placed) GameState.ReturnHeld();
            if (hand) PlayerRig.Buzz(hand, placed ? 0.7f : 0.2f);
            Destroy(gameObject);
        }

        // Le rayon blanc : de la main jusqu'au plateau / à la carte visés, sinon droit devant.
        void DrawRay()
        {
            if (!ray) ray = CreateRay();
            var end = TryGetSurfacePoint(out _, out var point) ? point : hand.position + hand.forward * 3f;
            ray.SetPosition(0, hand.position);
            ray.SetPosition(1, end);
        }

        LineRenderer CreateRay()
        {
            var line = new GameObject("Rayon de pose").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = Visuals.LineMaterial;
            line.startColor = line.endColor = Color.white;
            line.startWidth = 0.008f;
            line.endWidth = 0.004f;
            line.positionCount = 2;
            return line;
        }

        // En VR : la main serre le grip (XR Grab) ou le relâche.
        void OnGrab(SelectEnterEventArgs args)
        {
            handSpeed.Clear();
            if (Take(args.interactorObject.transform)) PlayerRig.Buzz(args.interactorObject.transform, 0.3f);
        }

        // Lâché : si la main bougeait vite, c'est un lancer ; sinon on le pose là où vise le rayon.
        void OnRelease(SelectExitEventArgs args)
        {
            if (Held != this) return;
            var velocity = handSpeed.Velocity;
            if (velocity.sqrMagnitude > ThrowSpeed * ThrowSpeed)
            {
                Throw(velocity);
                PlayerRig.Buzz(args.interactorObject.transform, 0.4f);
                return;
            }
            bool placed = Release();
            PlayerRig.Buzz(args.interactorObject.transform, placed ? 0.7f : 0.2f);
        }

        // Prendre le singe (en VR, ou au clic en mode PC). byHand = d'où part le rayon de pose.
        public bool Take(Transform byHand)
        {
            if (Held != null) return false;
            Held = this;
            hand = byHand;
            slot.Detach(this);                      // la case en pose un autre si on en a encore
            GameState.TakeFromInventory(monkey);
            return true;
        }

        // Lâcher le singe : posé (ou fusionné) là où vise le rayon, sinon rangé dans la bibliothèque. true = posé.
        public bool Release()
        {
            Held = null;
            bool placed = TryGetSurfacePoint(out var surface, out var point) && surface.Drop(point);
            if (!placed) GameState.ReturnHeld();
            Destroy(gameObject);
            return placed;
        }

        // Le plateau (ou la carte) visé, et le point visé dessus :
        // d'abord avec le rayon de la main (de loin), sinon juste sous le singe (on le pose en le tenant au-dessus).
        public bool TryGetSurfacePoint(out PlacementSurface surface, out Vector3 point)
        {
            if (flying) { surface = null; point = default; return false; }   // en vol : pas d'aperçu
            if (hand && Cast(hand.position, hand.forward, AimReach, out surface, out point)) return true;
            return Cast(transform.position, Vector3.down, DropReach, out surface, out point);
        }

        // Le premier objet touché par le rayon (sauf ce singe et le joueur) : est-ce un plateau ou la carte ?
        bool Cast(Vector3 origin, Vector3 direction, float reach, out PlacementSurface surface, out Vector3 point)
        {
            surface = null;
            point = default;
            // DefaultRaycastLayers ignore le joueur (couche Ignore Raycast, comme dans le cours)
            int count = Physics.RaycastNonAlloc(origin, direction, hits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform) || hits[i].distance >= best) continue;   // pas lui-même
                best = hits[i].distance;
                surface = hits[i].collider.GetComponentInParent<PlacementSurface>();
                point = hits[i].point;
            }
            return surface != null;
        }
    }
}
