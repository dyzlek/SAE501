using System.Collections;
using UnityEngine;

namespace SAE
{
    // La fin du jeu : quand la vague 10 (le dirigeable rouge) est gagnée, un feu d'artifice éclate au-dessus de cet objet
    // pendant quelques secondes. Il y en a un au hub et un sur la carte : on le voit où qu'on soit.
    // Le tableau de la vague dit VICTOIRE, et le bouton REJOUER s'allume (voir ActionCube).
    public class VictoryCelebration : MonoBehaviour
    {
        public float duration = 8f;        // en secondes
        public float interval = 0.35f;     // entre deux fusées
        public float radius = 4f;          // en mètres, autour de l'objet
        public float minHeight = 4f, maxHeight = 8f;

        static readonly Color[] colors =
        {
            new Color(1f, 0.85f, 0.2f), new Color(1f, 0.3f, 0.3f), new Color(0.3f, 0.6f, 1f),
            new Color(0.4f, 1f, 0.4f), new Color(1f, 0.5f, 0.9f), Color.white,
        };

        void OnEnable() => WaveSpawner.Victory += Celebrate;
        void OnDisable() => WaveSpawner.Victory -= Celebrate;

        void Celebrate() => StartCoroutine(Fireworks());

        IEnumerator Fireworks()
        {
            for (float t = 0f; t < duration; t += interval)
            {
                var flat = Random.insideUnitCircle * radius;
                var where = transform.position + new Vector3(flat.x, Random.Range(minHeight, maxHeight), flat.y);
                var color = colors[Random.Range(0, colors.Length)];
                Burst.Play(where, color, Color.Lerp(color, Color.white, 0.5f), 80, 4f, 8f, 0.12f, 0.4f, 1.4f);
                Sfx.Play(Sfx.Sound.Pop, where, 1f, 0.5f);   // un « boum » grave
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
