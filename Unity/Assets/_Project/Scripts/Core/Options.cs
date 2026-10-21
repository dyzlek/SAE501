using UnityEngine;

namespace SAE
{
    // Les réglages du joueur (menu OPTIONS), gardés sur le casque : volume de la musique, volume des sons, vibrations.
    // Music lit MusicVolume, Sounds lit SoundVolume, PlayerRig.Buzz lit Vibrations : rien d'autre à brancher.
    public static class Options
    {
        const string MusicKey = "SAE.Options.Musique", SoundKey = "SAE.Options.Sons", HapticsKey = "SAE.Options.Vibrations";
        public const int Steps = 10;   // les volumes vont de 0 à 10 (affichés « 7 / 10 »)

        public static event System.Action Changed;

        public static int Music { get => PlayerPrefs.GetInt(MusicKey, 8); set => Set(MusicKey, Mathf.Clamp(value, 0, Steps)); }
        public static int Sound { get => PlayerPrefs.GetInt(SoundKey, 10); set => Set(SoundKey, Mathf.Clamp(value, 0, Steps)); }
        public static bool Vibrations { get => PlayerPrefs.GetInt(HapticsKey, 1) == 1; set => Set(HapticsKey, value ? 1 : 0); }

        // 0 à 1, au carré : l'oreille entend en « décibels », un volume linéaire semblait ne presque pas changer entre 10 et 5
        public static float MusicVolume => Curve(Music);
        public static float SoundVolume => Curve(Sound);
        static float Curve(int step) { float v = step / (float)Steps; return v * v; }

        static void Set(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
