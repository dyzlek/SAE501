using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Quand on gagne un trophée (Trophy), il apparaît DEVANT le joueur, où qu'il soit (hub ou carte) :
    // une copie de la coupe qui surgit et tourne, au-dessus d'une ardoise « SUCCÈS ! » qui dit comment on l'a eu
    // (« 1 000 bananes récoltées », « 50 vagues vaincues »), avec une fanfare et une vibration. Elle reste 5 s, puis s'en va.
    // Confort VR : l'annonce est posée UNE fois dans le décor, à 1,4 m devant les yeux, un peu sous le regard ;
    // elle ne suit pas la tête (rien de collé au visage). Si plusieurs trophées tombent en même temps, ils passent l'un après l'autre.
    public class TrophyPopup : MonoBehaviour
    {
        const float Distance = 1.4f;       // en mètres devant les yeux
        const float Below = 0.15f;         // un peu sous le regard
        const float StaySeconds = 5f;
        const float GrowSeconds = 0.4f;
        const float CupHeight = 0.3f;      // la copie de la coupe est mise à cette hauteur, quelle que soit sa taille sur l'étagère
        const float SpinSpeed = 90f;

        static readonly string[] TierNames = { "BRONZE", "ARGENT", "OR", "DIAMANT" };
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);

        static TrophyPopup instance;
        static AudioClip fanfare;
        readonly Queue<Trophy> waiting = new Queue<Trophy>();
        AudioSource source;

        // Appelé par Trophy au moment où il est gagné (il a déjà pris sa couleur)
        public static void Show(Trophy trophy)
        {
            if (!instance)
            {
                var go = new GameObject("Annonce de trophée");
                DontDestroyOnLoad(go);   // elle sert au hub comme sur la carte
                instance = go.AddComponent<TrophyPopup>();
            }
            instance.waiting.Enqueue(trophy);
            if (instance.waiting.Count == 1) instance.StartCoroutine(instance.Run());
        }

        IEnumerator Run()
        {
            while (waiting.Count > 0)
            {
                var trophy = waiting.Peek();
                if (trophy) yield return Present(trophy);
                waiting.Dequeue();
            }
        }

        IEnumerator Present(Trophy trophy)
        {
            var root = Build(trophy);
            PlayFanfare();
            Haptics.PlayBoth(Haptics.Victory);

            // Ça surgit (petit rebond), ça tourne, puis ça rétrécit et disparaît
            var cup = root.Find("Coupe");
            for (float t = 0f; t < StaySeconds; t += Time.deltaTime)
            {
                float grow = Mathf.Clamp01(t / GrowSeconds);
                float shrink = Mathf.Clamp01((StaySeconds - t) / GrowSeconds);
                float s = Mathf.Min(1f + 0.15f * Mathf.Sin(grow * Mathf.PI), shrink) * grow;
                root.localScale = Vector3.one * s;
                if (cup) cup.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.Self);
                yield return null;
            }
            Destroy(root.gameObject);
        }

        // L'ardoise et la copie de la coupe, devant le joueur, tournées vers lui
        Transform Build(Trophy trophy)
        {
            var root = new GameObject("Trophée gagné").transform;
            var cam = Camera.main;
            Vector3 eyes = cam ? cam.transform.position : Vector3.up * 1.6f;
            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;   // regard droit vers le haut ou le bas
            forward.Normalize();
            root.position = eyes + forward * Distance + Vector3.down * Below;
            root.rotation = Quaternion.LookRotation(forward);   // +Z s'éloigne du joueur : le texte se lit côté -Z

            // L'ardoise, sous la coupe
            Visuals.Box("Cadre", root, new Vector3(0f, -0.12f, 0f), new Vector3(0.74f, 0.3f, 0.03f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, -0.12f, -0.02f), new Vector3(0.68f, 0.24f, 0.01f), Slate);
            Visuals.Text(root, "SUCCÈS !", new Vector3(0f, -0.04f, -0.03f), 0.07f, Gold, title: true);
            Visuals.Text(root, Description(trophy), new Vector3(0f, -0.12f, -0.03f), 0.04f, Color.white);
            Visuals.Text(root, $"TROPHÉE {TierNames[Mathf.Clamp(trophy.tier, 0, TierNames.Length - 1)]}", new Vector3(0f, -0.2f, -0.03f), 0.03f, Trophy.TierColors[trophy.tier]);

            // La coupe : une copie de celle de l'armoire (mêmes formes, même couleur), sans son script
            var copy = Instantiate(trophy.gameObject, root);
            copy.name = "Coupe";
            Destroy(copy.GetComponent<Trophy>());
            foreach (var c in copy.GetComponentsInChildren<Collider>()) Destroy(c);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            copy.transform.localPosition = Vector3.zero;
            var bounds = Bounds(copy);
            float scale = bounds.size.y > 0.01f ? CupHeight / bounds.size.y : 1f;
            copy.transform.localScale = Vector3.one * scale;
            // On pose son pied 5 cm au-dessus de l'ardoise, centrée
            bounds = Bounds(copy);
            copy.transform.position += root.position + Vector3.up * 0.05f - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            root.localScale = Vector3.zero;   // elle surgira (Present)
            return root;
        }

        // « 1 000 bananes récoltées » / « 50 vagues vaincues »
        static string Description(Trophy t)
        {
            string n = t.target.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
            return t.goal == TrophyGoal.Vagues ? $"{n} vagues vaincues" : $"{n} bananes récoltées";
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Une petite fanfare (do mi sol do), fabriquée au lancement comme les autres sons du jeu
        void PlayFanfare()
        {
            if (!source)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f;
                source.volume = 0.5f;
            }
            if (!fanfare) fanfare = MakeFanfare();
            source.PlayOneShot(fanfare);
        }

        static AudioClip MakeFanfare()
        {
            const int Rate = 44100;
            float[] notes = { 523f, 659f, 784f, 1047f };
            float[] starts = { 0f, 0.12f, 0.24f, 0.36f };
            const float Length = 1.4f;
            var data = new float[(int)(Length * Rate)];
            for (int n = 0; n < notes.Length; n++)
            {
                float hold = n == notes.Length - 1 ? 0.9f : 0.25f;   // la dernière note tient
                for (int i = (int)(starts[n] * Rate); i < data.Length; i++)
                {
                    float t = i / (float)Rate - starts[n];
                    if (t > hold) break;
                    float env = Mathf.Min(1f, t / 0.01f) * (1f - t / hold);
                    data[i] += 0.25f * env * (Mathf.Sin(2f * Mathf.PI * notes[n] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[n] * t));
                }
            }
            var clip = AudioClip.Create("Fanfare", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
