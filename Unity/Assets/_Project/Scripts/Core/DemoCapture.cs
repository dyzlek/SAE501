using System.Collections;
using System.IO;
using UnityEngine;

namespace SAE
{
    // Mode démo pour les captures d'écran : lancer le jeu avec  -demo "dossier"
    // Pose quelques singes, laisse partir la vague, prend 3 photos (hub, plateau, carte) puis quitte.
    public class DemoCapture : MonoBehaviour
    {
        string folder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-demo")
                    new GameObject("DemoCapture").AddComponent<DemoCapture>().folder = args[i + 1];
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);

            // Quelques singes de types et raretés différents, hors de la piste
            Place(MonkeyType.Classique, Rarity.Gris, -9f, 7.5f);
            Place(MonkeyType.Canon, Rarity.Bleu, 4.5f, 1.5f);
            Place(MonkeyType.Glace, Rarity.Violet, -1.5f, 1.5f);
            Place(MonkeyType.Sniper, Rarity.Rouge, 10.5f, -10.5f);
            Place(MonkeyType.Boomerang, Rarity.ArcEnCiel, -4.5f, -4.5f);
            Place(MonkeyType.Punaise, Rarity.Blanc, 10.5f, -1.5f);
            GameState.Held = new Monkey(MonkeyType.Colle, Rarity.Jaune);
            GameState.NotifyChanged();

            var player = PlayerController.Local;
            player.enabled = false; // on pilote la caméra nous-mêmes
            var cam = Camera.main.transform;

            yield return new WaitForSeconds(9f); // la 1re vague arrive

            Shoot(cam, new Vector3(1.6f, 2.3f, -1.6f), new Vector3(-0.6f, 0.9f, 1.8f), "1_hub.png");
            yield return new WaitForSeconds(0.5f);
            Shoot(cam, new Vector3(0f, 2.1f, 1.0f), new Vector3(0f, 0.8f, 2.25f), "2_plateau.png");
            yield return new WaitForSeconds(0.5f);
            Shoot(cam, new Vector3(0f, 22f, 16f), new Vector3(0f, 0f, 38f), "3_carte.png");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        static void Place(MonkeyType t, Rarity r, float x, float z)
        {
            var pos = new Vector2(x, z);
            if (MapLayout.CanPlace(pos, 0.6f)) GameState.Placed.Add(new PlacedMonkey(new Monkey(t, r), pos));
            else Debug.LogWarning($"Démo : position {pos} non valide");
        }

        void Shoot(Transform cam, Vector3 from, Vector3 lookAt, string file)
        {
            cam.SetPositionAndRotation(from, Quaternion.LookRotation(lookAt - from));
            StartCoroutine(CaptureNextFrame(Path.Combine(folder, file)));
        }

        static IEnumerator CaptureNextFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Capture : " + path);
        }
    }
}
