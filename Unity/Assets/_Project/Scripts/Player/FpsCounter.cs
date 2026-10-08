using UnityEngine;

namespace SAE
{
    // Le nombre d'images par seconde, sur une petite plaque au poignet gauche, comme une montre : on tourne le poignet
    // pour le lire, il ne flotte jamais devant les yeux (règle de confort). But : vérifier au casque qu'on tient 72 fps.
    // Vert à 72 et plus (la fréquence du Quest), orange de 60 à 71, rouge en dessous.
    // Seulement dans l'éditeur (Quest Link) et les builds « Development Build » : jamais dans le build rendu.
    public class FpsCounter : MonoBehaviour
    {
        const float Refresh = 0.5f;    // secondes entre deux mises à jour : un chiffre qui bouge à chaque image est illisible
        const int Target = 72;         // fps visés sur le Quest
        static readonly Vector3 PlateSize = new Vector3(0.06f, 0.026f, 0.004f);   // en mètres
        const float TextScale = 0.026f;                                            // le texte fait ~1,3 cm de haut

        TextMesh label;
        int frames;
        float elapsed;

        void Awake()
        {
            if (!Debug.isDebugBuild) { enabled = false; return; }

            // La plaque, posée sur le dessus de la manette, côté poignet, inclinée vers le joueur
            var plate = Visuals.Box("Montre FPS", transform, new Vector3(0f, 0.035f, -0.07f), PlateSize, new Color(0.12f, 0.1f, 0.08f));
            plate.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            label = Visuals.Text(plate.transform, "", new Vector3(0f, 0f, -0.6f), 0.5f, Color.white);   // juste devant la plaque
            // La plaque est un cube aplati : on compense son échelle pour que le texte ne soit pas écrasé
            label.transform.localScale = new Vector3(TextScale / PlateSize.x, TextScale / PlateSize.y, 1f);
        }

        void Update()
        {
            frames++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < Refresh) return;

            int fps = Mathf.RoundToInt(frames / elapsed);
            label.text = $"{fps} fps";
            label.color = fps >= Target ? new Color(0.4f, 1f, 0.4f) : fps >= 60 ? new Color(1f, 0.7f, 0.2f) : new Color(1f, 0.3f, 0.3f);
            frames = 0;
            elapsed = 0f;
        }
    }
}
