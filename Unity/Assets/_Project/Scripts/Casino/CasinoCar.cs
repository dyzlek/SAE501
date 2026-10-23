using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // La voiture du casino : une décapotable rouge des années 50, garée dans la bananeraie (elle mène au casino)
    // et sa jumelle garée dans le casino (elle ramène à la bananeraie). On appuie dessus (on la touche, on la vise
    // au rayon, ou on clique en mode PC) : elle klaxonne, un voile noir descend, on est posé à côté de l'autre voiture,
    // et le voile remonte. La caméra ne bouge jamais toute seule : pas de trajet en voiture (règle de confort).
    // Construite par le code au lancement (posée par le générateur : SAE > Générer le prototype).
    // Repère local : la voiture est en long sur X (le capot vers +X) ; le joueur arrive côté -Z, la portière face à lui.
    public class CasinoCar : MonoBehaviour, IPressable
    {
        public Level destination = Level.Casino;   // où elle emmène
        public Level home = Level.Hub;             // le niveau où elle est garée
        public string sign = "CASINO";             // écrit sur le panneau à côté d'elle

        static readonly List<CasinoCar> all = new List<CasinoCar>();
        static readonly Color Body = new Color(0.82f, 0.1f, 0.12f);
        static readonly Color Cream = new Color(0.96f, 0.92f, 0.82f);
        static readonly Color Chrome = new Color(0.78f, 0.8f, 0.84f);
        static readonly Color Tire = new Color(0.08f, 0.08f, 0.09f);
        static readonly Color Seat = new Color(0.45f, 0.22f, 0.1f);
        static readonly Color Glass = new Color(0.55f, 0.75f, 0.85f);
        static readonly Color Light = new Color(1f, 0.95f, 0.7f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Post = new Color(0.30f, 0.19f, 0.10f);
        static AudioClip honk;

        bool going;
        Transform arrival;   // là où on est posé en arrivant par cette voiture : devant la portière, dos à elle

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Awake()
        {
            arrival = new GameObject("Arrivée").transform;
            arrival.SetParent(transform, false);
            arrival.localPosition = new Vector3(0f, 0f, -2f);
            arrival.localRotation = Quaternion.Euler(0f, 180f, 0f);
            BuildCar();
            BuildSign();
            // Un seul collider pour toute la voiture : c'est lui qu'on touche ou qu'on vise
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.65f, 0f);
            box.size = new Vector3(3.1f, 1.3f, 1.5f);
            gameObject.AddComponent<XRSimpleInteractable>();
            gameObject.AddComponent<RayPress>().hoverScale = 1.02f;   // à peine : une voiture qui gonfle, c'est bizarre
        }

        public void Press()
        {
            if (going || ScreenCurtain.Busy || !PlayerRig.Local) return;
            going = true;
            var source = GetComponent<AudioSource>();
            if (!source) source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.PlayOneShot(Honk());
            StartCoroutine(Go());
        }

        System.Collections.IEnumerator Go()
        {
            yield return new WaitForSeconds(0.4f);   // le temps du klaxon
            yield return ScreenCurtain.Teleport(() =>
            {
                Levels.Go(destination);
                // La voiture d'en face, si sa scène est déjà chargée ; sinon le point d'arrivée du niveau (même endroit)
                foreach (var car in all)
                    if (car.home == destination && car != this) { PlayerRig.Local.TeleportTo(car.arrival); break; }
            });
            going = false;
        }

        // --- La voiture : des formes simples, aux couleurs franches ---

        void BuildCar()
        {
            var t = transform;
            Visuals.Box("Caisse", t, new Vector3(0f, 0.55f, 0f), new Vector3(2.9f, 0.42f, 1.35f), Body);
            Visuals.Box("Capot", t, new Vector3(0.85f, 0.8f, 0f), new Vector3(1.1f, 0.1f, 1.25f), Body);
            Visuals.Box("Coffre", t, new Vector3(-1.05f, 0.8f, 0f), new Vector3(0.75f, 0.1f, 1.25f), Body);
            Visuals.Box("Bande", t, new Vector3(0f, 0.62f, 0f), new Vector3(2.92f, 0.06f, 1.37f), Cream);   // la bande crème sur les flancs
            for (int side = -1; side <= 1; side += 2)
            {
                Visuals.Box("Pare-chocs", t, new Vector3(side * 1.48f, 0.4f, 0f), new Vector3(0.08f, 0.12f, 1.4f), Chrome);
                Visuals.Box("Portière", t, new Vector3(0f, 0.85f, side * 0.66f), new Vector3(1.1f, 0.16f, 0.04f), Body);
            }

            // L'habitacle ouvert : la banquette, le volant, le pare-brise
            Visuals.Box("Banquette", t, new Vector3(-0.35f, 0.82f, 0f), new Vector3(0.35f, 0.15f, 1.15f), Seat);
            Visuals.Box("Dossier", t, new Vector3(-0.52f, 1.0f, 0f), new Vector3(0.1f, 0.38f, 1.15f), Seat);
            var wheel = Part(PrimitiveType.Cylinder, "Volant", new Vector3(0.12f, 1.02f, 0.3f), new Vector3(0.26f, 0.015f, 0.26f), Tire);
            wheel.localRotation = Quaternion.Euler(0f, 0f, 60f);
            var glass = Visuals.Box("Pare-brise", t, new Vector3(0.32f, 1.07f, 0f), new Vector3(0.03f, 0.32f, 1.2f), Glass);
            glass.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);

            // Les roues (pneus noirs, enjoliveurs chromés) et les phares
            foreach (var x in new[] { -0.95f, 0.95f })
                for (int side = -1; side <= 1; side += 2)
                {
                    var tire = Part(PrimitiveType.Cylinder, "Roue", new Vector3(x, 0.32f, side * 0.62f), new Vector3(0.62f, 0.12f, 0.62f), Tire);
                    tire.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var cap = Part(PrimitiveType.Cylinder, "Enjoliveur", new Vector3(x, 0.32f, side * 0.69f), new Vector3(0.3f, 0.01f, 0.3f), Chrome);
                    cap.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
            for (int side = -1; side <= 1; side += 2)
                Part(PrimitiveType.Sphere, "Phare", new Vector3(1.42f, 0.66f, side * 0.45f), new Vector3(0.1f, 0.17f, 0.17f), Light);
            Visuals.Box("Calandre", t, new Vector3(1.46f, 0.55f, 0f), new Vector3(0.04f, 0.18f, 0.6f), Chrome);
        }

        // Le panneau sur son poteau, à côté du capot : où elle emmène
        void BuildSign()
        {
            var t = transform;
            Visuals.Box("Poteau", t, new Vector3(2.05f, 0.85f, -0.3f), new Vector3(0.08f, 1.7f, 0.08f), Post);
            Visuals.Box("Panneau", t, new Vector3(2.05f, 1.75f, -0.3f), new Vector3(0.9f, 0.32f, 0.05f), Post);
            Visuals.Box("Liseré", t, new Vector3(2.05f, 1.75f, -0.29f), new Vector3(0.96f, 0.38f, 0.04f), Gold);
            Visuals.Text(t, sign, new Vector3(2.05f, 1.78f, -0.335f), 0.18f, Gold, title: true);
            Visuals.Text(t, "appuie sur la voiture", new Vector3(2.05f, 1.64f, -0.335f), 0.055f, Cream);
        }

        Transform Part(PrimitiveType type, string name, Vector3 pos, Vector3 size, Color color)
        {
            var go = Visuals.Primitive(type);
            go.name = name;
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.AddComponent<ColorTint>().Set(color);
            return go.transform;
        }

        // Le klaxon « pouet-pouet » : deux notes, fabriquées par le code (pas de fichier son)
        static AudioClip Honk()
        {
            if (honk) return honk;
            const int rate = 22050;
            var samples = new float[(int)(rate * 0.42f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / rate;
                bool second = time > 0.22f;
                float local = second ? time - 0.22f : time;
                if (local > 0.18f) continue;                                 // un petit blanc entre les deux notes
                float freq = second ? 330f : 415f;
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * time)) * 0.5f + Mathf.Sin(2f * Mathf.PI * freq * 2f * time) * 0.2f;
                float envelope = Mathf.Min(1f, local / 0.01f) * Mathf.Min(1f, (0.18f - local) / 0.03f);
                samples[i] = wave * envelope * 0.35f;
            }
            honk = AudioClip.Create("Klaxon", samples.Length, 1, rate, false);
            honk.SetData(samples, 0);
            return honk;
        }
    }
}
