using UnityEngine;

namespace SAE
{
    // L'annonce des vagues spéciales (boss et dirigeable rouge) : des cloches, puis un cor grave quand le boss entre sur la piste,
    // avec deux battements de cœur dans les manettes. Le son est en 2D : on l'entend du hub comme de la carte.
    // Les sons viennent de Resources/Sons/Boss (voir Sounds). S'ils manquent, on garde ceux fabriqués par le code
    // (somme de sinus qui s'éteignent, comme une vraie cloche).
    public static class WaveAnnouncer
    {
        const int Rate = 44100;
        static AudioSource source;
        static AudioClip bells, horn;

        // Une vague qui contient un boss ou un dirigeable
        public static bool IsSpecial(WaveData wave) => wave.BossCount > 0;

        // Ce que dit le tableau pour cette vague spéciale
        public static string Title(WaveData wave)
        {
            foreach (var g in wave.groups)
                if (g.kind == BalloonKind.Dirigeable) return "<color=#FF5555>DIRIGEABLE ROUGE EN APPROCHE !</color>";
            return "<color=#C77DFF>VAGUE DE BOSS !</color>";
        }

        // Au lancement d'une vague spéciale : trois coups de cloche
        public static void WaveStarts()
        {
            if (Sounds.Has(Sounds.BossAnnonce)) Sounds.Play(Sounds.BossAnnonce);
            else { if (!bells) bells = MakeBells(); Play(bells); }
            Haptics.PlayBoth(Haptics.Alarm);
        }

        // Le groupe du boss commence : son entrée (une pour le boss, une plus grosse pour le dirigeable), sinon un cor grave
        public static void BossEnters(BalloonKind kind)
        {
            string sound = kind == BalloonKind.Dirigeable ? Sounds.DirigeableEntree : Sounds.BossEntree;
            if (Sounds.Has(sound)) Sounds.Play(sound);
            else { if (!horn) horn = MakeHorn(); Play(horn); }
            Haptics.PlayBoth(Haptics.Alarm);
        }

        static void Play(AudioClip clip)
        {
            if (!source)
            {
                var go = new GameObject("WaveAnnouncer") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.spatialBlend = 0f;   // 2D : partout pareil
                source.volume = 0.6f;
            }
            source.PlayOneShot(clip);
        }

        // Trois coups de cloche (sol, sol, do) qui résonnent
        static AudioClip MakeBells()
        {
            float[] starts = { 0f, 0.45f, 0.9f };
            float[] notes = { 784f, 784f, 1046f };
            const float length = 2.6f;
            var data = new float[(int)(length * Rate)];
            for (int n = 0; n < starts.Length; n++)
            {
                int from = (int)(starts[n] * Rate);
                for (int i = from; i < data.Length; i++)
                {
                    float t = (i - from) / (float)Rate;
                    float f = notes[n];
                    // Les partiels d'une cloche (pas des multiples exacts : c'est ce qui sonne « métal »)
                    float v = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 2.5f)
                            + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 5f)
                            + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 5.4f * t) * Mathf.Exp(-t * 9f);
                    data[i] += v * 0.25f;
                }
            }
            return Clip("Cloches", data);
        }

        // Un cor grave : deux notes tenues, qui montent puis s'éteignent
        static AudioClip MakeHorn()
        {
            const float length = 1.6f;
            var data = new float[(int)(length * Rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float f = t < 0.5f ? 110f : 82.4f;   // la, puis mi plus grave
                float env = Mathf.Clamp01(t * 8f) * Mathf.Clamp01((length - t) * 2f);
                float v = Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(6f * Mathf.PI * f * t);
                data[i] = v * env * 0.3f;
            }
            return Clip("Cor", data);
        }

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
