using UnityEngine;

namespace SAE
{
    // Fait avancer le tutoriel (Tutorial.Tick) et montre ce qu'il faut toucher. Un seul, dans la scène Hub
    // (toujours chargée : c'est celle du joueur). Les indications :
    //   - une flèche dorée pointée vers le bas, la pointe juste au-dessus de la cible : elle rebondit doucement ;
    //   - pour la porte de la cabane, pas de flèche : un contour doré qui pulse autour du cadre, des deux côtés
    //     (on le voit depuis la cabane comme depuis la bananeraie) ;
    //   - une flèche au sol, à nos pieds, qui montre la direction de la cible quand elle est loin (plus de 2,5 m).
    //     Au sol plutôt que devant les yeux : rien n'est collé au visage (règles de confort) ;
    //   - un « ding » doux qui vient du tableau du tutoriel (son 3D) à chaque nouvelle étape, puis toutes les 8 s tant
    //     qu'il faut le lire et qu'on ne le regarde pas : on le retrouve à l'oreille, même dans son dos.
    // 12 nov. : plus de rayon de lumière vers le ciel (jugé moche), nouvelle flèche, contour de la porte, le ding.
    // La flèche montre la cible la plus proche du joueur dans le niveau où il est (il y a un tableau au hub et un dans la
    // bananeraie), ou, pour le premier singe, sa case dans la bibliothèque (LibrarySlot).
    public class TutorialRunner : MonoBehaviour
    {
        public Transform pointer;          // la flèche (posée par PrototypeGenerator ; son dessin est refait au lancement)

        const float BounceHeight = 0.06f, BounceSpeed = 3f;
        const float Gap = 0.05f;           // en mètres : entre la pointe de la flèche et la cible
        const float GuideDistance = 0.7f;  // en mètres : la flèche au sol, devant les pieds, vers la cible
        const float GuideMin = 2.5f;       // en mètres : plus près que ça, on voit la cible, pas besoin de la flèche au sol
        const float PingEvery = 8f;        // en secondes : le rappel sonore du tableau
        const float LookAngle = 50f;       // en degrés : on regarde le tableau s'il est à moins de ça du regard
        static readonly Color Gold = new Color(1f, 0.8f, 0.2f), GoldDark = new Color(0.6f, 0.4f, 0.05f);

        Transform guide;                   // la flèche au sol
        Transform doorFrames;              // le contour de la porte (deux cadres)
        ColorTint[] doorBars;
        bool doorRetried;
        Transform slotFrame;               // le cadre autour de la case du premier singe
        ColorTint[] slotBars;
        AudioSource pingSource;
        float lastPing = -100f;

        void Start()
        {
            if (!pointer) return;
            BuildArrow(pointer);
            guide = BuildGuide();
            doorFrames = BuildDoorOutline();
            pingSource = BuildPing();
            Tutorial.Changed += PingNow;
        }

        void OnDestroy() => Tutorial.Changed -= PingNow;

        void Update()
        {
            Tutorial.Tick();
            // L'entraînement à l'arc : les ballons à éclater apparaissent devant le joueur, sur la carte
            PracticeBalloon.Keep(Tutorial.Current == Tutorial.Step.Practice && Levels.Current == Level.Carte, Tutorial.PracticeLeft);
            if (!pointer) return;

            var spot = Tutorial.Showing ? Tutorial.Spot(Tutorial.Current) : TutorialSpot.None;
            if (spot == TutorialSpot.Door && !doorFrames && !doorRetried) { doorRetried = true; doorFrames = BuildDoorOutline(); }   // la porte n'était peut-être pas prête au lancement
            bool door = spot == TutorialSpot.Door && Levels.Current == Level.Hub && doorFrames;
            var position = Vector3.zero;
            bool slot = spot == TutorialSpot.FirstMonkey && Levels.Current == Level.Hub;
            bool show = !door && !slot && Where(spot, out position);
            ShowSlot(slot);
            if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            if (door) Where(spot, out position);   // pour la flèche au sol
            ShowGuide(show || door, position);
            ShowDoor(door);
            PingWhenLost(spot);
            if (!show) return;

            // La flèche, bien droite, la pointe vers le bas juste au-dessus de la cible ; elle rebondit doucement.
            // Elle se tourne vers le joueur (on voit toujours une face de la pointe, sans tourner sur elle-même).
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * BounceSpeed)) * BounceHeight;
            pointer.position = position + Vector3.up * (Gap + bounce);
            var look = PlayerRig.Local ? PlayerRig.Local.HeadPosition - position : Vector3.forward;
            look.y = 0f;
            pointer.rotation = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;
        }

        // La flèche au sol : à nos pieds, tournée vers la cible, seulement si elle est loin
        void ShowGuide(bool show, Vector3 target)
        {
            var rig = PlayerRig.Local;
            bool far = false;
            var to = Vector3.zero;
            if (show && rig)
            {
                to = target - rig.HeadPosition;
                to.y = 0f;
                far = to.magnitude > GuideMin;
            }
            if (guide.gameObject.activeSelf != far) guide.gameObject.SetActive(far);
            if (!far) return;
            var head = rig.HeadPosition;
            var feet = new Vector3(head.x, rig.transform.position.y + 0.03f, head.z);   // un peu au-dessus du sol
            var dir = to.normalized;
            float slide = Mathf.Repeat(Time.time * 0.6f, 1f) * 0.25f;   // elle avance un peu, puis recommence : « par là ! »
            guide.SetPositionAndRotation(feet + dir * (GuideDistance + slide), Quaternion.LookRotation(dir));
        }

        // Le contour de la porte : allumé, il pulse entre l'or et l'or sombre
        void ShowDoor(bool on)
        {
            if (!doorFrames) return;
            if (doorFrames.gameObject.activeSelf != on) doorFrames.gameObject.SetActive(on);
            if (!on) return;
            var c = Color.Lerp(GoldDark, Gold, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            foreach (var bar in doorBars) bar.Animate(c);
        }

        // La case du premier singe dans la bibliothèque : un cadre doré qui pulse autour d'elle, tourné vers le centre de la
        // cabane (12 nov. : une flèche entre les étagères, c'était moche)
        void ShowSlot(bool on)
        {
            if (!slotFrame) slotFrame = BuildFrame("Cadre de la case (tutoriel)", 0.34f, 0.3f, 0.03f, out slotBars);
            var target = on ? LibrarySlot.Find(new Monkey(MonkeyType.Classique, Rarity.Gris)) : null;
            on = on && target;
            if (slotFrame.gameObject.activeSelf != on) slotFrame.gameObject.SetActive(on);
            if (!on) return;
            var p = target.transform.position;
            var inward = new Vector3(-p.x, 0f, -p.z).normalized;   // vers le centre de la cabane, là où est le joueur
            slotFrame.SetPositionAndRotation(p + Vector3.up * 0.13f + inward * 0.12f, Quaternion.LookRotation(-inward));
            var c = Color.Lerp(GoldDark, Gold, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            foreach (var bar in slotBars) bar.Animate(c);
        }

        // Un cadre rectangulaire (deux montants, un haut, un bas) de w × h, centré sur l'origine, dans le plan XY
        Transform BuildFrame(string name, float w, float h, float bar, out ColorTint[] bars)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, true);
            var list = new System.Collections.Generic.List<ColorTint>();
            void Piece(Vector3 pos, Vector3 size)
            {
                var go = Visuals.Box("Cadre", root, pos, size, Gold);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                list.Add(go.GetComponent<ColorTint>());
            }
            Piece(new Vector3(-w / 2f, 0f, 0f), new Vector3(bar, h + bar, 0.015f));
            Piece(new Vector3(w / 2f, 0f, 0f), new Vector3(bar, h + bar, 0.015f));
            Piece(new Vector3(0f, h / 2f, 0f), new Vector3(w + bar, bar, 0.015f));
            Piece(new Vector3(0f, -h / 2f, 0f), new Vector3(w + bar, bar, 0.015f));
            bars = list.ToArray();
            root.gameObject.SetActive(false);
            return root;
        }

        // --- Le ding du tableau ---

        void PingNow()
        {
            if (Tutorial.Showing) Ping(force: true);
        }

        // Pendant une étape à lire, si on ne regarde pas le tableau : un ding toutes les PingEvery secondes
        void PingWhenLost(TutorialSpot spot)
        {
            if (spot != TutorialSpot.Panel || Time.time - lastPing < PingEvery) return;
            Ping(force: false);
        }

        void Ping(bool force)
        {
            var rig = PlayerRig.Local;
            if (!pingSource || !rig) return;
            var board = TutorialTarget.Nearest(TutorialSpot.Panel, Levels.Current, rig.HeadPosition);
            if (!board) return;
            if (!force && Camera.main)
            {
                var to = board.transform.position - Camera.main.transform.position;
                if (Vector3.Angle(Camera.main.transform.forward, to) < LookAngle) { lastPing = Time.time; return; }   // il le regarde déjà
            }
            lastPing = Time.time;
            pingSource.transform.position = board.transform.position;
            pingSource.volume = 0.5f * Options.SoundVolume;
            pingSource.Play();
        }

        // Une source 3D et son ding, fabriqué par le code : deux notes douces (mi puis la) qui s'éteignent, 0,5 s
        static AudioSource MakePing()
        {
            const int rate = 44100;
            int length = (int)(0.5f * rate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float a = Mathf.Sin(2f * Mathf.PI * 659f * t) * Mathf.Exp(-t * 9f);                          // mi
                float b = t < 0.12f ? 0f : Mathf.Sin(2f * Mathf.PI * 880f * (t - 0.12f)) * Mathf.Exp(-(t - 0.12f) * 7f);   // la
                data[i] = 0.35f * (a + b) * Mathf.Clamp01(t / 0.005f);   // 5 ms d'attaque : pas de clic au début
            }
            var clip = AudioClip.Create("Ding du tutoriel", length, 1, rate, false);
            clip.SetData(data, 0);
            var source = new GameObject("Ding du tutoriel").AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.spatialBlend = 1f;      // 3D : on l'entend venir du tableau
            source.minDistance = 2f;
            source.maxDistance = 25f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }
        AudioSource BuildPing()
        {
            var source = MakePing();
            source.transform.SetParent(transform, false);
            return source;
        }

        // --- Ce qu'on fabrique au lancement ---

        // La flèche : une tige et une pointe à quatre faces, en or, pointée vers le bas. La pointe est à l'origine
        // de l'objet : posée sur une cible, elle la touche presque (avant : un V et une boule, très au-dessus des meubles).
        static void BuildArrow(Transform pointer)
        {
            for (int i = pointer.childCount - 1; i >= 0; i--) Destroy(pointer.GetChild(i).gameObject);
            pointer.localScale = Vector3.one;
            const float HeadH = 0.16f, HeadW = 0.17f, ShaftH = 0.2f, ShaftW = 0.065f;
            var shaft = Visuals.Box("Tige", pointer, new Vector3(0f, HeadH + ShaftH / 2f - 0.01f, 0f), new Vector3(ShaftW, ShaftH, ShaftW), Gold);
            var head = new GameObject("Pointe");
            head.transform.SetParent(pointer, false);
            head.AddComponent<MeshFilter>().sharedMesh = Pyramid(HeadW, HeadH);
            head.AddComponent<MeshRenderer>().sharedMaterial = shaft.GetComponent<MeshRenderer>().sharedMaterial;
            head.AddComponent<ColorTint>().Set(Gold);
            foreach (var r in pointer.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Une pyramide à base carrée, la pointe en bas à l'origine, la base en haut (à la hauteur h)
        static Mesh Pyramid(float w, float h)
        {
            float s = w / 2f;
            var tip = Vector3.zero;
            Vector3[] b = { new Vector3(-s, h, -s), new Vector3(s, h, -s), new Vector3(s, h, s), new Vector3(-s, h, s) };
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 4; i++)   // les 4 faces, chacune ses sommets (ombrage net)
            {
                int k = verts.Count;
                verts.Add(tip); verts.Add(b[i]); verts.Add(b[(i + 1) % 4]);   // sens des aiguilles vu de dehors : face visible
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
            }
            int t = verts.Count;          // le dessus (la base)
            verts.AddRange(b);
            tris.AddRange(new[] { t, t + 2, t + 1, t, t + 3, t + 2 });
            var mesh = new Mesh { name = "Pointe de flèche" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // La flèche au sol : un chevron doré à plat (deux branches), la pointe vers +Z
        Transform BuildGuide()
        {
            var root = new GameObject("Flèche au sol du tutoriel").transform;
            root.SetParent(transform, false);
            foreach (int side in new[] { -1, 1 })
            {
                var bar = Visuals.Box("Branche", root, new Vector3(side * 0.07f, 0f, 0f), new Vector3(0.05f, 0.015f, 0.24f), Gold);
                bar.transform.localRotation = Quaternion.Euler(0f, -side * 40f, 0f);
            }
            root.gameObject.SetActive(false);
            return root;
        }

        // Le contour de la porte : un cadre doré de chaque côté du mur, un peu plus grand que l'ouverture.
        // La porte est posée par PrototypeGenerator avec sa cible (TutorialSpot.Door) au milieu du battant fermé ;
        // le mur est tourné vers le centre de la cabane (l'origine du hub).
        Transform BuildDoorOutline()
        {
            var target = TutorialTarget.Nearest(TutorialSpot.Door, Level.Hub, Vector3.zero);
            if (!target) { Debug.LogWarning("Tutoriel : pas de cible pour la porte, pas de contour."); return null; }
            const float W = 1.6f, H = 2.55f, Bar = 0.08f, Out = 0.3f;   // l'ouverture fait 1,3 × 2,4 m, le cadre la déborde
            var leaf = target.transform.position;
            var outward = new Vector3(leaf.x, 0f, leaf.z).normalized;   // du centre de la cabane vers dehors
            if (outward == Vector3.zero) return null;
            // Le milieu du mur (rayon de la cabane), pas le battant : il est dans l'épaisseur du mur, côté dehors, et le
            // cadre intérieur, calé sur lui, finissait caché dans les rondins (12 nov.)
            var center = outward * HubLayout.CabinRadius;
            float floor = transform.position.y;                              // le sol du hub
            var root = new GameObject("Contour de la porte (tutoriel)").transform;
            root.SetParent(transform, true);
            root.SetPositionAndRotation(new Vector3(center.x, floor, center.z), Quaternion.LookRotation(outward));
            var bars = new System.Collections.Generic.List<ColorTint>();
            foreach (float side in new[] { -Out, Out })   // dedans, puis dehors
            {
                void Piece(Vector3 pos, Vector3 size)
                {
                    var go = Visuals.Box("Contour", root, pos, size, Gold);
                    go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    bars.Add(go.GetComponent<ColorTint>());
                }
                Piece(new Vector3(-W / 2f, H / 2f, side), new Vector3(Bar, H, 0.02f));   // montants
                Piece(new Vector3(W / 2f, H / 2f, side), new Vector3(Bar, H, 0.02f));
                Piece(new Vector3(0f, H, side), new Vector3(W + Bar, Bar, 0.02f));       // le haut
            }
            doorBars = bars.ToArray();
            root.gameObject.SetActive(false);
            return root;
        }

        // Où poser la pointe de la flèche pour cet endroit (false : rien à montrer dans le niveau où est le joueur)
        static bool Where(TutorialSpot spot, out Vector3 position)
        {
            position = default;
            if (spot == TutorialSpot.None) return false;
            if (spot == TutorialSpot.FirstMonkey)
            {
                var slot = LibrarySlot.Find(new Monkey(MonkeyType.Classique, Rarity.Gris));
                if (!slot || Levels.Current != Level.Hub) return false;
                position = slot.transform.position + Vector3.up * 0.1f;   // le singe dans sa case
                return true;
            }
            var target = TutorialTarget.Nearest(spot, Levels.Current, PlayerRig.Local ? PlayerRig.Local.HeadPosition : Vector3.zero);
            if (!target) return false;
            position = target.transform.position + Vector3.up * target.height;   // la pointe à la hauteur prévue par la cible
            return true;
        }
    }
}
