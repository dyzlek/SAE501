using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Joueur clavier/souris, pour tester vite sans casque (mode PC : menu SAE → Mode de jeu).
    // ZQSD : marcher · souris : regarder · clic gauche : appuyer (boutons, coffre) ou prendre (singe, banane),
    // relâcher le clic : lâcher · A maintenu : fiche du singe visé · Échap : libérer la souris.
    // Mêmes règles qu'en VR : on prend un singe ou une banane jusqu'à 6 m (GrabReach.Reach), le singe tenu se pose
    // là où on vise, la banane se lâche au-dessus du panier.
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayer : MonoBehaviour
    {
        public float speed = 3.5f;              // en m/s
        public float mouseSensitivity = 0.1f;   // degrés par pixel
        public float reach = 30f;               // portée du clic sur les boutons, en mètres
        public float holdDistance = 0.8f;       // l'objet tenu flotte à cette distance devant les yeux

        CharacterController controller;
        Transform cam;
        float pitch;
        float verticalSpeed;
        MonkeyToken heldMonkey;
        Banane heldBanana;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            cam = GetComponentInChildren<Camera>().transform;
            LockCursor(true);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame) LockCursor(true);
                return;
            }

            Look(mouse.delta.ReadValue() * mouseSensitivity);
            Walk(kb);

            if (mouse.leftButton.wasPressedThisFrame) Click();
            if (heldMonkey) Carry(heldMonkey.transform);
            if (heldBanana) Carry(heldBanana.transform);
            if (mouse.leftButton.wasReleasedThisFrame) Release();
        }

        void Look(Vector2 delta)
        {
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            cam.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        // Les touches sont lues par position : ZQSD sur un clavier AZERTY
        void Walk(Keyboard kb)
        {
            var input = Vector2.zero;
            if (kb.wKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed) input.x -= 1;
            var move = transform.TransformDirection(new Vector3(input.x, 0f, input.y).normalized) * speed;
            verticalSpeed = controller.isGrounded ? -1f : verticalSpeed - 9.81f * Time.deltaTime;
            move.y = verticalSpeed;
            controller.Move(move * Time.deltaTime);
        }

        // Ce qu'on vise au centre de l'écran : un singe ou une banane à prendre (à 6 m au plus), sinon un bouton à enfoncer.
        void Click()
        {
            if (!Physics.Raycast(cam.position, cam.forward, out var hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return;

            bool inGrabReach = hit.distance <= GrabReach.Reach;
            var token = hit.collider.GetComponentInParent<MonkeyToken>();
            if (token)
            {
                if (inGrabReach && token.Take(cam)) heldMonkey = token;
                return;
            }
            var banana = hit.collider.GetComponentInParent<Banane>();
            if (banana)
            {
                if (inGrabReach && !banana.Deposee) { heldBanana = banana; banana.Prise(); }
                return;
            }
            hit.collider.GetComponentInParent<IPressable>()?.Press();
        }

        void Carry(Transform held)
        {
            var target = cam.position + cam.forward * holdDistance;
            held.position = Vector3.Lerp(held.position, target, 1f - Mathf.Exp(-20f * Time.deltaTime));
        }

        void Release()
        {
            if (heldMonkey) heldMonkey.Release();     // posé là où on vise, sinon rangé
            if (heldBanana) heldBanana.Lachee();      // elle tombe : dans le panier si on est au-dessus
            heldMonkey = null;
            heldBanana = null;
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // Mode PC seulement (jamais en VR) : un viseur et l'aide des touches, pour tester.
        void OnGUI()
        {
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
            GUI.Label(new Rect(10, Screen.height - 30, 900, 25),
                "Mode PC · ZQSD : marcher · Souris : regarder · Clic : appuyer / prendre, relâcher : lâcher · A : infos du singe · Échap : souris");
        }
    }
}
