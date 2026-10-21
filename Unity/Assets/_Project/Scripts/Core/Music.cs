using UnityEngine;

namespace SAE
{
    // La musique de fond, entendue partout (2D) et en boucle : la musique d'ambiance tout le temps,
    // remplacée par la musique du boss tant qu'un boss ou un dirigeable est en vie sur la carte.
    // On passe de l'une à l'autre en fondu (2 s), et la musique du boss repart de son début à chaque boss.
    // Posée sur le joueur par PlayerRig (rien à mettre dans les scènes, qui sont regénérées).
    // Les morceaux sont dans Resources/Sons/Musique (voir Sounds) ; s'ils manquent, il n'y a simplement pas de musique.
    public class Music : MonoBehaviour
    {
        const float Volume = 0.25f;     // bas : la musique ne doit pas couvrir les sons du jeu
        const float FadeTime = 2f;      // secondes pour passer d'une musique à l'autre

        AudioSource ambiance, boss;

        void Awake()
        {
            ambiance = MakeSource(Sounds.Clip(Sounds.MusiqueAmbiance));
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
            bool bossAlive = BossAlive();
            if (bossAlive && boss.clip && !boss.isPlaying) boss.Play();   // un nouveau boss : son morceau repart du début

            float step = Volume / FadeTime * Time.unscaledDeltaTime;
            boss.volume = Mathf.MoveTowards(boss.volume, bossAlive && boss.clip ? Volume : 0f, step);
            ambiance.volume = Mathf.MoveTowards(ambiance.volume, bossAlive && boss.clip ? 0f : Volume, step);
            if (!bossAlive && boss.isPlaying && boss.volume <= 0f) boss.Stop();
        }

        static bool BossAlive()
        {
            foreach (var b in Balloon.All)
                if (b.Kind == BalloonKind.Boss || b.Kind == BalloonKind.Dirigeable) return true;
            return false;
        }
    }
}
