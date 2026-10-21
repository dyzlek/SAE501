using UnityEngine;

namespace SAE
{
    // La musique de fond, entendue partout (2D) et en boucle, selon le moment :
    //   préparation → musique d'ambiance ; vague en cours → musique de vague ; un boss ou un dirigeable en vie → musique du boss.
    // On passe de l'une à l'autre en fondu (2 s). Les musiques de vague et de boss repartent de leur début à chaque fois.
    // Posée sur le joueur par PlayerRig (rien à mettre dans les scènes, qui sont regénérées).
    // Les morceaux sont dans Resources/Sons/Musique (voir Sounds) ; s'il en manque un, la musique d'avant continue.
    public class Music : MonoBehaviour
    {
        const float BaseVolume = 0.25f; // bas : la musique ne doit pas couvrir les sons du jeu
        static float Volume => BaseVolume * Options.MusicVolume;   // × le réglage MUSIQUE du menu OPTIONS
        const float FadeTime = 2f;      // secondes pour passer d'une musique à l'autre

        AudioSource ambiance, wave, boss;

        void Awake()
        {
            ambiance = MakeSource(Sounds.Clip(Sounds.MusiqueAmbiance));
            wave = MakeSource(Sounds.Clip(Sounds.MusiqueVague));
            boss = MakeSource(Sounds.Clip(Sounds.MusiqueBoss));
            if (ambiance.clip) { ambiance.volume = Volume; ambiance.Play(); }
        }

        AudioSource MakeSource(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.playOnAwake = false;
            return source;
        }

        void Update()
        {
            // La musique qu'on veut entendre maintenant (si son morceau existe, sinon celle d'avant continue)
            var spawner = WaveSpawner.Instance;
            var wanted = ambiance;
            if (spawner && spawner.Running && wave.clip) wanted = wave;
            if (BossAlive() && boss.clip) wanted = boss;

            float step = Volume / FadeTime * Time.unscaledDeltaTime;
            Fade(ambiance, ambiance == wanted, step, restart: false);   // l'ambiance reprend où elle en était
            Fade(wave, wave == wanted, step, restart: true);
            Fade(boss, boss == wanted, step, restart: true);
        }

        // Monte ou baisse une musique. Celle qui revient repart de son début si 'restart', sinon de là où elle s'était arrêtée.
        static void Fade(AudioSource music, bool on, float step, bool restart)
        {
            if (!music.clip) return;
            if (on && !music.isPlaying)
            {
                if (restart) { music.Stop(); music.Play(); }
                else { music.UnPause(); if (!music.isPlaying) music.Play(); }
            }
            music.volume = Mathf.MoveTowards(music.volume, on ? Volume : 0f, step);
            if (!on && music.isPlaying && music.volume <= 0f) { if (restart) music.Stop(); else music.Pause(); }
        }

        static bool BossAlive()
        {
            foreach (var b in Balloon.All)
                if (b.Kind == BalloonKind.Boss || b.Kind == BalloonKind.Dirigeable) return true;
            return false;
        }
    }
}
