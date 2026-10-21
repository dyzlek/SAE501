using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // La météo de la carte, liée aux vagues de boss :
    //   Storm()  : la vague de boss est lancée, l'orage monte : des nuages arrivent, puis il pleut, la brume se resserre (on voit moins loin), le tonnerre gronde ;
    //   Clear()  : la vague est finie, le ciel se dégage doucement.
    // Un seul chiffre fait tout : Intensity, de 0 (beau temps) à 1 (orage), qui glisse vers sa cible.
    // Rien n'est enregistré dans la scène : WaveSpawner ajoute ce script au lancement, et les nuages, la pluie
    // et les sons sont fabriqués par le code (comme les cloches de WaveAnnouncer).
    // Casque : peu de particules (≈ 700 gouttes en l'air), 10 nuages d'un seul maillage, brume au lieu d'effets lourds.
    // Confort : le ciel change lentement, et l'éclair n'éclaire que doucement (pas de flash blanc dans les yeux).
    // La météo touche les deux niveaux (carte et hub) : chacun a sa lumière, ses nuages sont au-dessus du joueur, et la pluie
    // s'arrête sur le toit de la cabane. Au hub, la pluie et le tonnerre s'entendent étouffés.
    public class Weather : MonoBehaviour
    {
        public static Weather Instance { get; private set; }

        // --- Réglages (Inspector) ---
        public float gatherSeconds = 8f;      // l'orage monte (de 0 à 1)
        public float clearSeconds = 10f;      // le ciel se dégage
        public float cloudsLevel = 0.4f;      // Intensity quand seuls les nuages sont là
        public float rainStartsAt = 0.45f;   // en dessous, il ne pleut pas : les nuages sont d'abord en place (voir UpdateClouds)
        public float stormFogStart = 5f;      // en mètres : début de la brume à pleine tempête
        public float stormFogEnd = 45f;       // en mètres : on ne voit plus rien au-delà
        public float sunInStorm = 0.3f;       // part de la lumière du soleil qui reste à pleine tempête
        public float rainRate = 1100f;        // gouttes par seconde à pleine tempête
        public float thunderEvery = 9f;       // secondes entre deux éclairs, en moyenne

        // Le niveau de météo en cours : 0 beau temps, 1 orage
        public float Intensity { get; private set; }
        float target;

        // --- Constantes ---
        const int CloudCount = 10;
        const float CloudHeight = 26f;        // en mètres au-dessus de la carte
        const float CloudFarAway = 150f;      // d'où arrivent les nuages
        const float CloudNearMax = 38f;       // où ils s'arrêtent au plus près du centre
        const float RainHeight = 6f;          // la pluie tombe de 6 m au-dessus de la tête
        const float HubVolume = 0.35f;        // au hub, la pluie s'entend étouffée
        static readonly Color StormFog = new Color(0.36f, 0.39f, 0.44f);
        static readonly Color CloudLight = new Color(0.95f, 0.96f, 0.98f);
        static readonly Color CloudDark = new Color(0.27f, 0.29f, 0.34f);

        // --- La lumière de départ de chaque scène (relevée la 1re fois que le joueur y est : hub et carte) ---
        class Look
        {
            public Light sun;
            public float sunIntensity;
            public Color sunColor, sky, equator, ground, fogColor;
            public bool fog;
            public float fogStart, fogEnd;
            public Material skyMaterial;
            public float exposure;
            public bool lit;                  // la météo a touché à cette scène (à remettre comme avant par beau temps)
        }
        readonly System.Collections.Generic.Dictionary<Scene, Look> looks = new System.Collections.Generic.Dictionary<Scene, Look>();

        // --- Nuages, pluie, sons ---
        Transform clouds;
        Transform[] cloudBody;
        ColorTint[] cloudTint;
        Vector3[] cloudSpot;                  // où chaque nuage s'arrête (autour du centre de la carte)
        float lastCloudLevel = -1f;
        ParticleSystem rain;
        AudioSource rainSound, thunderSound;
        float nextBolt, flash, thunderAt;
        Camera head;

        // ---------------- Appelé par WaveSpawner ----------------
        public static void Gather() { if (Instance) Instance.target = Instance.cloudsLevel; }
        public static void Storm() { if (Instance) Instance.target = 1f; }
        public static void Clear() { if (Instance) Instance.target = 0f; }

        // Pour tester sans attendre un boss : clic droit sur le composant dans l'Inspector
        [ContextMenu("Tempête")] void TestStorm() => target = 1f;
        [ContextMenu("Beau temps")] void TestClear() => target = 0f;

        void Awake()
        {
            Instance = this;
            BuildClouds();
            BuildRain();
            BuildSounds();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // La scène de la carte est-elle celle où se trouve le joueur ? (sinon il est au hub : le son est étouffé)
        bool OnMap => gameObject.scene == SceneManager.GetActiveScene();

        void Update()
        {
            // Le niveau glisse vers sa cible : lentement quand les nuages arrivent, un peu plus vite quand ils partent
            float speed = target > Intensity ? 1f / gatherSeconds : 1f / clearSeconds;
            Intensity = Mathf.MoveTowards(Intensity, target, speed * Time.deltaTime);

            UpdateLightning();
            UpdateClouds();
            UpdateRainAndSound();

            // RenderSettings suit la scène active (celle où est le joueur) : hub ou carte, chacune garde sa propre base
            var scene = SceneManager.GetActiveScene();
            if (!looks.TryGetValue(scene, out var look)) looks[scene] = look = Capture(scene);
            // Beau temps : on ne touche plus à la lumière de la scène (une dernière fois pour la remettre comme avant)
            bool changing = Intensity > 0f || flash > 0f;
            if (changing || look.lit) ApplyLight(look);
            look.lit = changing;
        }

        void LateUpdate()
        {
            if (!head) head = Camera.main;
            if (!head) return;
            // Les nuages restent au-dessus du joueur, au hub comme sur la carte
            var p = head.transform.position;
            clouds.position = new Vector3(p.x, p.y - 1.6f + CloudHeight, p.z);
            // La pluie suit la tête du joueur (elle tombe là où il regarde, et pas sur toute la carte)
            if (rain.isPlaying) rain.transform.position = p + Vector3.up * RainHeight;
            if (!roof) BuildRoof();
        }

        // ---------------- Le toit de la cabane ----------------
        // Les gouttes n'ont pas de physique (trop cher pour le Quest) : rien ne les arrête, elles traversaient le toit.
        // On pose donc une forme invisible qui épouse la cabane (un « dôme » : cylindre de 3,4 m de rayon, coiffé d'une
        // demi-sphère qui suit le toit), et le module Trigger de la pluie fait disparaître toute goutte qui y entre.
        // Un seul collider, en trigger : il ne gêne ni le joueur, ni le rayon de téléportation (il ignore les triggers).
        const float RoofTop = 3f;             // les murs de la cabane font 3 m (HubLayout.CabinHeight)
        Collider roof;

        void BuildRoof()
        {
            var hubScene = SceneManager.GetSceneByName(Levels.HubScene);
            if (!hubScene.isLoaded) return;
            Transform hub = null;
            foreach (var go in hubScene.GetRootGameObjects()) if (go.name == "Hub") hub = go.transform;
            if (!hub) return;

            var shelter = new GameObject("Abri de la pluie");
            shelter.transform.SetParent(hub, false);
            shelter.layer = 2;   // « Ignore Raycast » : aucun rayon du jeu ne le voit
            var capsule = shelter.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.radius = HubLayout.CabinRadius;
            capsule.height = RoofTop + 2f * HubLayout.CabinRadius;   // la demi-sphère du haut monte à ~6,4 m, au-dessus du toit (4,8 m)
            capsule.center = new Vector3(0f, RoofTop / 2f, 0f);
            roof = capsule;

            var trigger = rain.trigger;
            trigger.enabled = true;
            trigger.AddCollider(roof);
            trigger.inside = ParticleSystemOverlapAction.Kill;   // la goutte disparaît en touchant le toit
            trigger.outside = ParticleSystemOverlapAction.Ignore;
            trigger.enter = ParticleSystemOverlapAction.Kill;
        }

        // ---------------- Lumière, brume ----------------
        // On relève une fois les réglages de la scène (soleil, ambiance, brume, ciel) : tout le reste du temps,
        // chaque valeur est recalculée depuis cette base, jamais depuis la valeur précédente.
        Look Capture(Scene scene)
        {
            var look = new Look { sun = RenderSettings.sun };
            if (!look.sun)
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Directional && l.gameObject.scene == scene) { look.sun = l; break; }
            if (look.sun) { look.sunIntensity = look.sun.intensity; look.sunColor = look.sun.color; }

            look.sky = RenderSettings.ambientSkyColor;
            look.equator = RenderSettings.ambientEquatorColor;
            look.ground = RenderSettings.ambientGroundColor;

            look.fog = RenderSettings.fog;
            look.fogColor = RenderSettings.fogColor;
            look.fogStart = look.fog ? RenderSettings.fogStartDistance : 60f;   // sans brume au départ : celle du prototype
            look.fogEnd = look.fog ? RenderSettings.fogEndDistance : 230f;

            // Une copie du ciel : on assombrit la copie, pas le fichier du projet
            if (RenderSettings.skybox && RenderSettings.skybox.HasProperty("_Exposure"))
            {
                look.skyMaterial = new Material(RenderSettings.skybox);
                RenderSettings.skybox = look.skyMaterial;
                look.exposure = look.skyMaterial.GetFloat("_Exposure");
            }
            return look;
        }

        void ApplyLight(Look look)
        {
            float k = Mathf.SmoothStep(0f, 1f, Intensity);

            if (look.sun)
            {
                // L'éclair ramène un peu de lumière, jamais plus que le soleil du beau temps
                float dim = Mathf.Lerp(1f, sunInStorm, k);
                look.sun.intensity = look.sunIntensity * Mathf.Lerp(dim, 1f, flash * 0.6f);
                look.sun.color = Dull(look.sunColor, k);
            }
            RenderSettings.ambientSkyColor = Dull(look.sky, k) * Mathf.Lerp(1f, 0.7f, k) * Mathf.Lerp(1f, 1.5f, flash);
            RenderSettings.ambientEquatorColor = Dull(look.equator, k) * Mathf.Lerp(1f, 0.7f, k);
            RenderSettings.ambientGroundColor = Dull(look.ground, k) * Mathf.Lerp(1f, 0.7f, k);

            RenderSettings.fog = look.fog || k > 0.001f;
            RenderSettings.fogColor = Color.Lerp(look.fogColor, StormFog, k);
            RenderSettings.fogStartDistance = Mathf.Lerp(look.fogStart, stormFogStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(look.fogEnd, stormFogEnd, k);

            if (look.skyMaterial) look.skyMaterial.SetFloat("_Exposure", Mathf.Lerp(look.exposure, look.exposure * 0.35f, k));
        }

        // Une couleur qui se grise et se refroidit avec la tempête
        static Color Dull(Color c, float k)
        {
            float g = c.grayscale * 0.8f;
            return Color.Lerp(c, new Color(g * 0.95f, g, g * 1.1f), k);
        }

        // ---------------- Éclairs et tonnerre ----------------
        // Un éclair éclaire doucement (voir ApplyLight), puis le tonnerre arrive après un délai (le son est plus lent que la lumière)
        void UpdateLightning()
        {
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 3f);   // l'éclair s'éteint en 1/3 s

            if (Intensity < 0.95f) { nextBolt = Time.time + thunderEvery; return; }
            if (Time.time >= nextBolt)
            {
                flash = 1f;
                thunderAt = Time.time + Random.Range(0.5f, 1.5f);
                nextBolt = Time.time + Random.Range(thunderEvery * 0.6f, thunderEvery * 1.4f);
            }
            if (thunderAt > 0f && Time.time >= thunderAt)
            {
                thunderAt = 0f;
                thunderSound.volume = OnMap ? 0.7f : 0.7f * HubVolume;
                thunderSound.pitch = Random.Range(0.85f, 1.1f);
                thunderSound.Play();
                Haptics.PlayBoth(Haptics.Thunder);
            }
        }

        // ---------------- Nuages ----------------
        // 10 nuages (trois boules fondues en un seul maillage) qui arrivent de loin, se rassemblent au-dessus de la carte,
        // grossissent et noircissent avec l'orage. Aucun collider, aucune ombre.
        void BuildClouds()
        {
            clouds = new GameObject("Nuages").transform;
            clouds.SetParent(transform, false);
            clouds.localPosition = Vector3.up * CloudHeight;

            var puffs = new[]
            {
                new CombineInstance { mesh = MeshBatch.Ball, transform = Matrix4x4.TRS(new Vector3(-0.32f, 0f, 0f), Quaternion.identity, new Vector3(0.7f, 0.55f, 0.7f)) },
                new CombineInstance { mesh = MeshBatch.Ball, transform = Matrix4x4.TRS(new Vector3(0.05f, 0.08f, 0.04f), Quaternion.identity, new Vector3(0.9f, 0.7f, 0.85f)) },
                new CombineInstance { mesh = MeshBatch.Ball, transform = Matrix4x4.TRS(new Vector3(0.38f, -0.04f, -0.03f), Quaternion.identity, new Vector3(0.65f, 0.5f, 0.65f)) },
            };
            var shape = new Mesh { name = "Nuage" };
            shape.CombineMeshes(puffs, true, true);
            shape.RecalculateBounds();

            cloudBody = new Transform[CloudCount];
            cloudTint = new ColorTint[CloudCount];
            cloudSpot = new Vector3[CloudCount];
            var rng = new System.Random(7);   // toujours les mêmes nuages, aux mêmes endroits
            for (int i = 0; i < CloudCount; i++)
            {
                float angle = (i + (float)rng.NextDouble() * 0.6f) / CloudCount * Mathf.PI * 2f;
                float radius = Mathf.Lerp(4f, CloudNearMax, (float)rng.NextDouble());
                cloudSpot[i] = new Vector3(Mathf.Cos(angle) * radius, (float)rng.NextDouble() * 6f, Mathf.Sin(angle) * radius);

                var go = Visuals.Box("Nuage", clouds, Vector3.zero, Vector3.one, CloudLight);
                go.GetComponent<MeshFilter>().sharedMesh = shape;
                var r = go.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                float size = Mathf.Lerp(16f, 26f, (float)rng.NextDouble());
                go.transform.localScale = new Vector3(size, size * 0.32f, size * 0.7f);
                go.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                cloudBody[i] = go.transform;
                cloudTint[i] = go.GetComponent<ColorTint>();
            }
            clouds.gameObject.SetActive(false);
        }

        void UpdateClouds()
        {
            bool visible = Intensity > 0.001f;
            if (clouds.gameObject.activeSelf != visible) clouds.gameObject.SetActive(visible);
            if (!visible || Mathf.Approximately(Intensity, lastCloudLevel)) return;   // rien ne bouge : rien à recalculer
            lastCloudLevel = Intensity;

            float k = Mathf.SmoothStep(0f, 1f, Intensity);
            float arrive = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.45f, Intensity));   // les nuages sont en place avant que la pluie ne commence
            var tint = Color.Lerp(CloudLight, CloudDark, k);
            for (int i = 0; i < CloudCount; i++)
            {
                // Du lointain (dans la même direction) jusqu'à sa place
                var spot = cloudSpot[i];
                var outward = new Vector3(spot.x, 0f, spot.z).normalized * CloudFarAway;
                cloudBody[i].localPosition = Vector3.Lerp(outward + Vector3.up * spot.y, spot, arrive);
                cloudTint[i].Set(tint);
            }
        }

        // ---------------- Pluie ----------------
        // Des traits fins qui tombent d'un carré de 24 m au-dessus de la tête. Matériau du jeu (le shader est dans le build).
        void BuildRain()
        {
            var go = new GameObject("Pluie");
            go.transform.SetParent(transform, false);
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // une goutte lâchée ne suit pas la tête
            main.startLifetime = 0.7f;
            main.startSpeed = 16f;
            main.startSize = 0.02f;
            main.startColor = new Color(0.78f, 0.85f, 0.95f, 0.4f);
            main.gravityModifier = 0f;
            main.maxParticles = 1000;

            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 24f, 0.1f);   // la boîte émet vers +Z : le plat est en X et Y, épais de 10 cm en Z
            shape.rotation = new Vector3(90f, 0f, 0f);   // couchée à plat au-dessus de la tête : les gouttes partent vers le bas

            var emission = rain.emission;
            emission.rateOverTime = 0f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2f;
            r.velocityScale = 0.04f;
            r.sharedMaterial = Visuals.LineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void UpdateRainAndSound()
        {
            float rainAmount = Mathf.InverseLerp(rainStartsAt, 0.8f, Intensity);   // 0 à 1 : pleine pluie dès 80 % de l'orage
            bool raining = rainAmount > 0f;

            if (raining && !rain.isPlaying) rain.Play();
            else if (!raining && rain.isPlaying) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // les dernières gouttes finissent de tomber
            var emission = rain.emission;
            emission.rateOverTime = rainRate * rainAmount;

            rainSound.volume = 0.3f * rainAmount * (OnMap ? 1f : HubVolume);
            if (raining && !rainSound.isPlaying) rainSound.Play();
            else if (!raining && rainSound.isPlaying) rainSound.Stop();
        }

        // ---------------- Sons ----------------
        // Pas de fichier audio : du bruit adouci (la pluie, un grondement), fabriqué au lancement
        const int Rate = 22050;

        void BuildSounds()
        {
            rainSound = NewSource("Son de pluie", MakeNoise("Pluie", 2f, 0.55f, false), true);
            thunderSound = NewSource("Son de tonnerre", MakeNoise("Tonnerre", 3.5f, 0.97f, true), false);
        }

        AudioSource NewSource(string sourceName, AudioClip clip, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;   // 2D : partout pareil
            return s;
        }

        // Du bruit blanc passé dans un filtre : plus smooth vaut grave (0,97 = grondement) ou aigu (0,55 = pluie).
        // decay = le son s'éteint au fil du clip (tonnerre) au lieu de rester égal (pluie en boucle).
        static AudioClip MakeNoise(string clipName, float seconds, float smooth, bool decay)
        {
            var data = new float[(int)(seconds * Rate)];
            var rng = new System.Random(3);
            float last = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)rng.NextDouble() * 2f - 1f;
                last = last * smooth + white * (1f - smooth);
                float t = i / (float)data.Length;
                float env = decay ? Mathf.Clamp01(t * 12f) * Mathf.Pow(1f - t, 2f) : 1f;
                data[i] = last * env * (smooth > 0.9f ? 6f : 1.6f);   // un bruit très filtré est faible : on le remonte
            }
            var clip = AudioClip.Create(clipName, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
