using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les sons du jeu, rangés dans Resources/Sons/<Catégorie>/<Son>/ : un dossier = un son. S'il contient plusieurs
    // fichiers (ex. les deux clics de bouton), on en tire un au hasard à chaque fois, pour que ça ne sonne pas répétitif.
    // Les sons sont chargés à leur première utilisation. Un son qui manque ne fait rien (pas d'erreur) :
    // on peut brancher le code avant d'avoir trouvé le son.
    //   Play(son, endroit) : son 3D, qui vient de cet endroit (en VR, on l'entend du bon côté) ;
    //   Play(son)          : son 2D, entendu partout pareil (annonces, voix de Quincy).
    // Beaucoup de fichiers commencent par un petit silence (jusqu'à 180 ms) : le son semblait arriver en retard.
    // On le saute automatiquement, pour que le son parte pile au moment de l'action.
    public static class Sounds
    {
        // Les noms des sons (= leurs dossiers), au même endroit pour ne jamais recopier une chaîne
        public const string BossAnnonce = "Boss/Annonce";
        public const string BossEntree = "Boss/Entree_Boss";
        public const string DirigeableEntree = "Boss/Entree_Dirigeable";
        public const string VagueGagnee = "Vagues/Gagnee";
        public const string VictoireFinale = "Vagues/Victoire_Finale";
        public const string VaguePerdue = "Vagues/Perdue";
        public const string ViePerdue = "Vagues/Vie_Perdue";
        public const string ArcAmelioration = "Arc/Amelioration";
        public const string ArcAmeliorationMax = "Arc/Amelioration_Max";
        public const string QuincyTouche = "Arc/Quincy_Touche";
        public const string ArcTendre = "Arc/Tendre";
        public const string ArcTir = "Arc/Tir";
        public const string BallonEclate = "Ballons/Eclate";
        public const string SingePose = "Singes/Pose";
        public const string Fusion = "Singes/Fusion";
        public const string CoffreOuverture = "Coffre/Ouverture";
        public const string CoffreFermeture = "Coffre/Fermeture";
        public const string SingeObtenu = "Coffre/Singe_Obtenu";
        public const string BananeVendue = "Bananes/Vendue";
        public const string AchatRefuse = "Interface/Achat_Refuse";
        public const string Bouton = "Interface/Bouton";
        public const string Teleportation = "Deplacement/Teleportation";
        public const string FlecheCentre = "Bonus/Fleche_Centre";
        public const string MusiqueAmbiance = "Musique/Ambiance";
        public const string MusiqueVague = "Musique/Vague";
        public const string MusiqueBoss = "Musique/Boss";

        static readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<AudioClip, int> starts = new Dictionary<AudioClip, int>();   // où commence vraiment le son
        const float SilenceLevel = 0.02f;   // en dessous, on considère que c'est du silence

        // Sans rechargement du domaine (Enter Play Mode rapide), les statiques survivent d'une partie à l'autre
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { clips.Clear(); starts.Clear(); nextQuincy = 0f; popFrame = -1; popsThisFrame = 0; }

        public static bool Has(string sound) => Clips(sound).Length > 0;

        // Un des fichiers du son, au hasard (null s'il n'y en a pas)
        public static AudioClip Clip(string sound)
        {
            var all = Clips(sound);
            return all.Length == 0 ? null : all[Random.Range(0, all.Length)];
        }

        static AudioClip[] Clips(string sound)
        {
            if (!clips.TryGetValue(sound, out var found))
            {
                found = Resources.LoadAll<AudioClip>("Sons/" + sound);
                clips[sound] = found;
            }
            return found;
        }

        // Joue le son une fois. Chaque son a sa propre petite source, détruite à la fin : plusieurs sons peuvent se superposer,
        // chacun avec sa hauteur (pitch).
        public static void Play(string sound, Vector3? where = null, float volume = 1f, float pitch = 1f)
        {
            var clip = Clip(sound);
            if (!clip) return;
            var go = new GameObject("Son " + clip.name);
            if (where.HasValue) go.transform.position = where.Value;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = where.HasValue ? 1f : 0f;
            source.minDistance = 2f;   // pleine puissance jusqu'à 2 m, puis il baisse avec la distance
            source.timeSamples = Start(clip);
            source.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        // Le premier échantillon qui n'est plus du silence (calculé une fois par fichier)
        static int Start(AudioClip clip)
        {
            if (starts.TryGetValue(clip, out int start)) return start;
            start = 0;
            if (clip.loadType != AudioClipLoadType.Streaming)   // un son lu en streaming ne donne pas ses échantillons
            {
                var data = new float[clip.samples * clip.channels];
                if (clip.GetData(data, 0))
                    for (int i = 0; i < data.Length; i++)
                        if (Mathf.Abs(data[i]) > SilenceLevel) { start = i / clip.channels; break; }
            }
            starts[clip] = start;
            return start;
        }

        // Le « tic » des roulettes (coffre et casino) : fabriqué par le code, une seule fois
        static AudioClip tick;
        public static AudioClip Tick => tick ? tick : tick = MakeTick();

        // Le tic : 30 ms d'un son aigu mêlé d'un peu de souffle, qui s'éteint très vite (comme un clic en plastique)
        static AudioClip MakeTick()
        {
            const int rate = 44100;
            int length = (int)(0.03f * rate);
            var data = new float[length];
            var noise = new System.Random(7);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float fade = Mathf.Exp(-t * 180f);
                float tone = 0.5f * Mathf.Sin(2f * Mathf.PI * 2400f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * 900f * t);
                float breath = 0.35f * (float)(noise.NextDouble() * 2.0 - 1.0);
                data[i] = (tone + breath) * fade * 0.8f;
            }
            var clip = AudioClip.Create("Tic", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Un ballon éclate. Quand les singes en éclatent toute une file, des dizaines de sons partiraient en même temps :
        // au plus 3 par image. La hauteur varie un peu à chaque fois, pour que ça ne sonne pas répétitif.
        const int MaxPopsPerFrame = 3;
        static int popFrame = -1, popsThisFrame;

        public static void Pop(Vector3 where)
        {
            if (Time.frameCount != popFrame) { popFrame = Time.frameCount; popsThisFrame = 0; }
            if (++popsThisFrame > MaxPopsPerFrame) return;
            Play(BallonEclate, where, 0.6f, Random.Range(0.9f, 1.15f));
        }

        // La voix de Quincy quand une flèche touche : pas à chaque fois (ce serait vite pénible), une chance sur 8,
        // et jamais deux fois en moins de 10 s. 'sure' : à coup sûr (un boss qui éclate), mais toujours pas deux fois de suite.
        const float QuincyChance = 1f / 8f;
        const float QuincyCooldown = 10f;
        static float nextQuincy;

        public static void Quincy(bool sure = false)
        {
            if (Time.time < nextQuincy || (!sure && Random.value > QuincyChance)) return;
            nextQuincy = Time.time + QuincyCooldown;
            Play(QuincyTouche);
        }
    }
}
