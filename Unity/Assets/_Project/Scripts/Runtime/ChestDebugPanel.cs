using UnityEngine;
using UnityEngine.InputSystem;

namespace Sae501.Coffres
{
    // Menu de bêta-test (IMGUI), caché par défaut : F1 pour l'afficher ou le cacher.
    // Uniquement pour les tests sur PC, pas pour le jeu final.
    public class ChestDebugPanel : MonoBehaviour
    {
        public ChestController chest;
        public PlayerController player;

        bool visible;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                visible = !visible;
                player.SetMenuOpen(visible); // libère la souris pour cliquer sur les boutons
            }
        }

        void OnGUI()
        {
            if (!visible)
            {
                GUI.Label(new Rect(10, Screen.height - 26, 200, 22), "F1 : menu bêta");
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 330, 400), GUI.skin.box);
            GUILayout.Label("MENU BÊTA  (F1 pour fermer)");
            GUILayout.Label($"Argent : {SAE.Economy.Money}   (prix du coffre : {chest.Price})");
            GUILayout.Label($"Vagues vaincues : {chest.Progress}  (gel des proba à {chest.oddsSettings.freezeAfterWaves})   Coffres ouverts : {chest.OpenedCount}");

            GUILayout.Space(6);
            var odds = chest.CurrentOdds();
            for (int i = 0; i < odds.Length; i++)
                {
                var r = (Rarity)i;
                bool open = ChestOdds.IsUnlocked(chest.oddsSettings, r, chest.Progress);
                GUILayout.Label(open ? $"{r} : {odds[i] * 100f:0.00} %" : $"{r} : verrouillé (dès la vague {chest.oddsSettings.unlockAtWave[i]})");
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Argent -> 0")) SAE.GameState.Money = 0;
            if (GUILayout.Button("Argent +100")) SAE.Economy.Earn(100);
            if (GUILayout.Button("Remettre le compteur à 0")) chest.ResetOpenedCount();
            GUILayout.EndArea();
        }
    }
}
