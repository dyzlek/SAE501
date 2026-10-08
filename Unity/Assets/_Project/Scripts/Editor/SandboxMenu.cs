using UnityEditor;

namespace SAE
{
    // Menu SAE > Bac à sable : coche / décoche le mode où tous les singes sont débloqués et gratuits (voir GameState.Sandbox).
    // Le choix est gardé sur ce PC (EditorPrefs) et pris en compte au prochain Play. Il ne part jamais dans un build.
    public static class SandboxMenu
    {
        const string MenuPath = "SAE/Bac à sable (tous les singes gratuits)";

        [MenuItem(MenuPath)]
        static void Toggle() => EditorPrefs.SetBool(GameState.SandboxPref, !EditorPrefs.GetBool(GameState.SandboxPref, false));

        [MenuItem(MenuPath, true)]
        static bool ShowCheck()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(GameState.SandboxPref, false));
            return true;
        }
    }
}
