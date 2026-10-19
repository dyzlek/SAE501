using System;
using UnityEngine;

namespace SAE
{
    public enum Sound { Pop, Shoot, Press, Deny, Coin, Place, Chest, WaveStart, WaveWon, WaveLost, Leak, Ding, Door, Victory, Bell }

    // Tous les sons du jeu, fabriqués par le code au lancement (pas de fichier audio : rien à télécharger, pas de licence).
    // Un son = une petite formule (onde + enveloppe) évaluée pour chaque échantillon. C'est du « chiptune » assumé,
    // dans l'esprit simple et cartoon du jeu ; on pourra les remplacer un par un par de vrais fichiers (#34).
    //   Sfx.Play(Sound.Pop, position)  : joue un son à cet endroit (3D, il vient de là) ; sans position, dans la tête.
    // 12 haut-parleurs (AudioSource) tournent à tour de rôle : jamais de création d'objet pendant le jeu (fps du Quest).
    // La musique d'ambiance est une boucle de notes pincées (gamme pentatonique), très douce.
    public static class Sfx
    {
        const int Rate = 22050;          // échantillons par seconde : assez pour des bips, deux fois moins lourd que 44100
        const int Voices = 12;
        const float MusicVolume = 0.1f;

        static AudioSource[] voices;
        static AudioSource music;
        static AudioClip calmMusic, tenseMusic;
        static AudioClip[] clips;
        static int next;
        static readonly System.Random Rng = new System.Random(501);

        // Sans rechargement du domaine (Enter Play Mode rapide), les statiques survivent d'une partie à l'autre
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { voices = null; music = null; calmMusic = tenseMusic = null; clips = null; next = 0; }

        // Au lancement : on prépare les sons et on lance la musique
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start() { Init(); music.Play(); }

        public static void Play(Sound sound, Vector3? at = null, float volume = 1f, float pitch = 1f)
        {
            Init();
            var voice = voices[next];
            next = (next + 1) % Voices;
            voice.clip = clips[(int)sound];
            voice.volume = volume;
            voice.pitch = pitch;
            voice.spatialBlend = at.HasValue ? 1f : 0f;   // 1 = vient de l'endroit, 0 = dans la tête
            if (at.HasValue) voice.transform.position = at.Value;
            voice.Play();
        }

        // Musique plus tendue pendant les vagues de boss et spéciales (#133), la musique calme le reste du temps
        public static void SetTension(bool tense)
        {
            Init();
            var clip = tense ? tenseMusic : calmMusic;
            if (music.clip == clip) return;
            music.clip = clip;
            music.Play();
        }

        // Crée les haut-parleurs et les sons, une seule fois (l'objet survit aux changements de scène)
        static void Init()
        {
            if (voices != null && voices[0]) return;
            var host = new GameObject("Sons");
            UnityEngine.Object.DontDestroyOnLoad(host);

            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var v = new GameObject("Voix " + i).AddComponent<AudioSource>();
                v.transform.SetParent(host.transform, false);
                v.playOnAwake = false;
                v.minDistance = 2f;    // plein volume à moins de 2 m, puis ça baisse
                v.maxDistance = 30f;
                v.rolloffMode = AudioRolloffMode.Linear;
                voices[i] = v;
            }

            clips = new AudioClip[Enum.GetValues(typeof(Sound)).Length];
            clips[(int)Sound.Pop] = Make("Pop", 0.12f, t => 0.5f * Noise() * Decay(t, 0.12f) + 0.5f * Sweep(t, 0.12f, 700f, 250f) * Decay(t, 0.12f));
            clips[(int)Sound.Shoot] = Make("Tir", 0.2f, t => 0.3f * Noise() * Mathf.Sin(Mathf.PI * t / 0.2f) + 0.4f * Sweep(t, 0.2f, 350f, 120f) * Decay(t, 0.2f));
            clips[(int)Sound.Press] = Make("Appui", 0.06f, t => 0.5f * Tone(t, 900f) * Decay(t, 0.06f));
            clips[(int)Sound.Deny] = Make("Refus", 0.18f, t => 0.25f * Mathf.Sign(Tone(t, 140f)) * Decay(t, 0.18f));
            clips[(int)Sound.Coin] = Make("Pièce", 0.25f, Melody(new[] { 988f, 1319f }, 0.07f, 0.18f));
            clips[(int)Sound.Place] = Make("Pose", 0.15f, t => 0.6f * Sweep(t, 0.15f, 160f, 70f) * Decay(t, 0.15f) + 0.2f * Noise() * Decay(t, 0.03f));
            clips[(int)Sound.Chest] = Make("Coffre", 0.6f, Melody(new[] { 523f, 659f, 784f, 1047f }, 0.1f, 0.3f));
            clips[(int)Sound.WaveStart] = Make("Vague", 0.6f, Melody(new[] { 392f, 392f, 523f }, 0.16f, 0.3f));
            clips[(int)Sound.WaveWon] = Make("Vague gagnée", 0.8f, Melody(new[] { 523f, 659f, 784f, 1047f }, 0.12f, 0.35f));
            clips[(int)Sound.WaveLost] = Make("Vague perdue", 0.9f, Melody(new[] { 392f, 349f, 311f, 262f }, 0.2f, 0.4f));
            clips[(int)Sound.Leak] = Make("Fuite", 0.25f, t => 0.3f * Mathf.Sign(Tone(t, 90f)) * Decay(t, 0.25f));
            clips[(int)Sound.Ding] = Make("Ding", 0.3f, Melody(new[] { 880f }, 0.1f, 0.3f));
            clips[(int)Sound.Door] = Make("Porte", 0.45f, t => 0.3f * Sweep(t, 0.45f, 180f, 300f) * Mathf.Sin(Mathf.PI * t / 0.45f) + 0.1f * Noise() * Mathf.Sin(Mathf.PI * t / 0.45f));
            clips[(int)Sound.Victory] = Make("Victoire", 1.8f, Melody(new[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1319f }, 0.18f, 0.5f));
            // Trois coups de cloche graves : une note et ses harmoniques « fausses » (×2,4 et ×3,9), qui sonnent métal
            clips[(int)Sound.Bell] = Make("Cloches", 2f, t =>
            {
                float sum = 0f;
                for (int k = 0; k < 3; k++)
                {
                    float local = t - k * 0.55f;
                    if (local < 0f) continue;
                    float env = Mathf.Exp(-local * 3f) * Mathf.Min(1f, local / 0.003f);
                    sum += env * (Tone(local, 330f) + 0.5f * Tone(local, 330f * 2.4f) + 0.25f * Tone(local, 330f * 3.9f));
                }
                return 0.3f * sum;
            });

            // La musique : 32 notes pincées, une toutes les 0,45 s. Le bout des dernières notes est ramené au début (t + total),
            // pour que la boucle ne « claque » pas au raccord.
            var notes = new[]
            {
                262f, 330f, 392f, 523f, 440f, 392f, 330f, 392f, 294f, 392f, 440f, 587f, 523f, 440f, 392f, 330f,
                262f, 392f, 523f, 659f, 587f, 523f, 440f, 392f, 330f, 440f, 523f, 440f, 392f, 330f, 294f, 262f,
            };
            const float Step = 0.45f;
            float total = notes.Length * Step;
            var pluck = Melody(notes, Step, 1.2f);
            music = new GameObject("Musique").AddComponent<AudioSource>();
            music.transform.SetParent(host.transform, false);
            calmMusic = Make("Musique", total, t => pluck(t) + pluck(t + total));

            // La musique tendue : une basse qui martèle deux notes en mineur, deux fois plus vite
            var tense = new[] { 110f, 110f, 131f, 110f, 104f, 104f, 123f, 104f, 110f, 110f, 131f, 147f, 131f, 123f, 104f, 98f };
            const float TenseStep = 0.22f;
            float tenseTotal = tense.Length * TenseStep;
            var drum = Melody(tense, TenseStep, 0.3f);
            tenseMusic = Make("Musique tendue", tenseTotal, t => 1.6f * (drum(t) + drum(t + tenseTotal)));

            music.clip = calmMusic;
            music.loop = true;
            music.volume = MusicVolume;
            music.spatialBlend = 0f;
        }

        // --- Les briques pour fabriquer un son ---

        // Remplit un clip : wave(t) donne l'onde (de -1 à 1) à l'instant t, en secondes
        static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            var data = new float[(int)(seconds * Rate)];
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Tone(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t);

        // Un son qui glisse de hz0 à hz1 pendant 'duration' secondes
        static float Sweep(float t, float duration, float hz0, float hz1) =>
            Mathf.Sin(2f * Mathf.PI * (hz0 * t + (hz1 - hz0) * t * t / (2f * duration)));

        static float Noise() => (float)(Rng.NextDouble() * 2.0 - 1.0);

        // Part vite à fond, puis s'éteint doucement jusqu'à 0 à 'duration' (évite le « clic » de début et de fin)
        static float Decay(float t, float duration)
        {
            float left = Mathf.Clamp01(1f - t / duration);
            return Mathf.Min(1f, t / 0.004f) * left * left;
        }

        // Une suite de notes pincées : la note k démarre à k × step et dure 'length' secondes (hz = 0 : silence)
        static Func<float, float> Melody(float[] hz, float step, float length) => t =>
        {
            float sum = 0f;
            int last = Mathf.Min(hz.Length - 1, Mathf.FloorToInt(t / step));
            int first = Mathf.Max(0, Mathf.FloorToInt((t - length) / step));
            for (int k = first; k <= last; k++)
            {
                if (hz[k] <= 0f) continue;
                float local = t - k * step;
                if (local < 0f || local > length) continue;
                sum += (Tone(local, hz[k]) + 0.3f * Tone(local, hz[k] * 2f)) * Decay(local, length);
            }
            return 0.35f * sum;
        };
    }
}
