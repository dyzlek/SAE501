using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les bruitages du jeu, FABRIQUÉS PAR LE CODE au lancement : pas de fichier son à importer ni de licence à vérifier.
    // Chaque son est une courte onde calculée échantillon par échantillon (une sinusoïde, du bruit, une enveloppe
    // qui décroît), rangée dans un AudioClip. Règle VR « un retour à chaque action » : son + vibration (PlayerRig.Buzz).
    // Les sons sont joués DANS le décor (son 3D) : un ballon qui éclate à gauche s'entend à gauche.
    public static class Sfx
    {
        public enum Sound { Click, Pop, Coin, Error, Whoosh, Twang, Thunk, Chime, Fanfare }

        const int Rate = 22050;   // échantillons par seconde : assez pour des bruitages, deux fois plus léger que 44 100
        static readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();

        // Joue un son à cet endroit. pitch : 1 = normal ; on le varie un peu pour que les répétitions ne lassent pas.
        public static void Play(Sound sound, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            var clip = Clip(sound);
            var go = new GameObject("Son " + sound);
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch * Random.Range(0.95f, 1.05f);
            source.spatialBlend = 1f;       // son 3D : il vient de l'endroit de l'action
            source.minDistance = 1.5f;
            source.maxDistance = 40f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            Object.Destroy(go, clip.length / source.pitch + 0.1f);
        }

        static AudioClip Clip(Sound sound)
        {
            if (clips.TryGetValue(sound, out var clip) && clip) return clip;
            var samples = sound switch
            {
                Sound.Click => Tone(0.04f, t => Mathf.Sin(2f * Mathf.PI * 1800f * t), 60f),
                Sound.Pop => Tone(0.09f, t => 0.6f * Noise() + 0.6f * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 250f, t / 0.09f) * t), 45f),
                Sound.Coin => Notes(0.07f, 988f, 1319f),
                Sound.Error => Tone(0.18f, t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 160f * t)) * 0.5f, 12f),
                Sound.Whoosh => Whoosh(0.3f),
                Sound.Twang => Tone(0.35f, t => Mathf.Sin(2f * Mathf.PI * 196f * t + 3f * Mathf.Sin(2f * Mathf.PI * 6f * t)) + 0.3f * Noise() * Mathf.Exp(-60f * t), 9f),
                Sound.Thunk => Tone(0.12f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 70f, t / 0.12f) * t) + 0.2f * Noise(), 30f),
                Sound.Chime => Notes(0.08f, 1047f, 1319f, 1568f, 2093f),
                _ => Notes(0.12f, 523f, 659f, 784f, 1047f),   // Fanfare : do mi sol do
            };
            clip = AudioClip.Create(sound.ToString(), samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            clips[sound] = clip;
            return clip;
        }

        static float Noise() => Random.Range(-1f, 1f);

        // Un son de 'duration' secondes : wave(t) donne l'onde, l'enveloppe exp(-decay·t) l'éteint peu à peu
        static float[] Tone(float duration, System.Func<float, float> wave, float decay)
        {
            var data = new float[Mathf.CeilToInt(duration * Rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float attack = Mathf.Clamp01(t * 500f);   // 2 ms de montée : pas de « clic » au début
                data[i] = 0.5f * attack * Mathf.Exp(-decay * t) * wave(t);
            }
            return data;
        }

        // Des notes jouées l'une après l'autre (pièce, carillon, fanfare), chacune de 'step' secondes
        static float[] Notes(float step, params float[] frequencies)
        {
            int noteLength = Mathf.CeilToInt(step * Rate);
            int tail = Mathf.CeilToInt(0.15f * Rate);   // la dernière note résonne un peu plus longtemps
            var data = new float[noteLength * frequencies.Length + tail];
            for (int n = 0; n < frequencies.Length; n++)
            {
                int length = n == frequencies.Length - 1 ? noteLength + tail : noteLength;
                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / Rate;
                    float attack = Mathf.Clamp01(t * 500f);
                    data[n * noteLength + i] += 0.35f * attack * Mathf.Exp(-8f * t) * Mathf.Sin(2f * Mathf.PI * frequencies[n] * t);
                }
            }
            return data;
        }

        // Un souffle : du bruit adouci (filtre passe-bas simple), qui monte puis retombe
        static float[] Whoosh(float duration)
        {
            var data = new float[Mathf.CeilToInt(duration * Rate)];
            float smooth = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float k = (float)i / data.Length;
                smooth += (Noise() - smooth) * 0.08f;     // garde les sons graves du bruit : ça souffle au lieu de grésiller
                data[i] = 1.2f * smooth * Mathf.Sin(k * Mathf.PI);
            }
            return data;
        }
    }
}
