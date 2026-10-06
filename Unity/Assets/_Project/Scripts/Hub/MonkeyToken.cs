using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Un singe qu'on prend à la main sur l'étagère (XR Grab Interactable).
    // Chaque case de la bibliothèque qui possède au moins un singe en pose un sur elle (voir LibrarySlot).
    //   - le saisir le sort de l'inventaire (GameState.Held) ;
    //   - pendant qu'on le tient, un rayon part de la main : là où il touche le plateau ou la carte (même de loin),
    //     le plateau montre l'aperçu vert/rouge et le halo de fusion ;
    //   - le lâcher le pose à cet endroit, ou le fusionne avec le singe identique qui s'y trouve ;
    //   - le lâcher sans viser le plateau ni la carte le range dans la bibliothèque.
    public class MonkeyToken : MonoBehaviour
    {
        const float AimReach = 30f;          // portée du rayon de pose, en mètres (assez pour viser la carte de loin)
        const float DropReach = 1.5f;        // sinon, on cherche le plateau juste sous le singe

        // Le singe tenu en main (un seul à la fois, comme GameState.Held).
        public static MonkeyToken Held { get; private set; }

        Monkey monkey;
        LibrarySlot slot;
        XRGrabInteractable grab;
        Transform hand;                      // la main (l'interacteur) qui le tient : le rayon part d'elle
        LineRenderer ray;                    // le rayon visible, de la main jusqu'au point visé
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

            var rb = piece.AddComponent<Rigidbody>();
            rb.isKinematic = true;     // il attend sur l'étagère sans tomber
            rb.useGravity = false;

            var grab = piece.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;   // il reste dans la main là où on l'a attrapé

            var token = piece.AddComponent<MonkeyToken>();
            token.monkey = monkey;
            token.slot = slot;
            token.grab = grab;
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
            grab.enabled = Held == null || Held == this;
            if (Held == this) DrawRay();
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

        void OnGrab(SelectEnterEventArgs args)
        {
            if (Held != null) return;
            Held = this;
            hand = args.interactorObject.transform;
            slot.Detach(this);                      // la case en pose un autre si on en a encore
            GameState.TakeFromInventory(monkey);
            PlayerRig.Buzz(args.interactorObject.transform, 0.3f);
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (Held != this) return;
            Held = null;

            bool placed = TryGetSurfacePoint(out var surface, out var point) && surface.Drop(point);
            if (!placed) GameState.ReturnHeld();    // lâché dans le vide : il retourne dans la bibliothèque
            PlayerRig.Buzz(args.interactorObject.transform, placed ? 0.7f : 0.2f);
            Destroy(gameObject);
        }

        // Le plateau (ou la carte) visé, et le point visé dessus :
        // d'abord avec le rayon de la main (de loin), sinon juste sous le singe (on le pose en le tenant au-dessus).
        public bool TryGetSurfacePoint(out PlacementSurface surface, out Vector3 point)
        {
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
