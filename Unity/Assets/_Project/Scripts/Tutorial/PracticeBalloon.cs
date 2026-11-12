using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un ballon d'entraînement du tutoriel, sur la carte : avant la vraie vague 1, trois ballons flottent au-dessus du chemin,
    // au milieu de la carte, immobiles (ils dansent juste un peu). Il faut les éclater à l'arc : c'est clair (« éclate les 3 ballons »),
    // on voit tout de suite si on a touché, et on apprend à viser sans pression.
    // Ils ne suivent pas le chemin et ne sont pas des ballons de vague (Balloon) : la flèche les reconnaît (Arrow),
    // les éclate et continue sa course. TutorialRunner les fait apparaître pendant l'étape, et les retire après.
    public class PracticeBalloon : MonoBehaviour
    {
        const float Size = 0.7f;                       // en mètres : plus gros qu'un vrai ballon, pour réussir de loin
        // Où ils flottent : au-dessus du chemin, au milieu de la carte (repère de la carte, voir MapLayout), bien en face
        // du joueur qui arrive au sud. Avant, ils étaient juste devant lui... et cachés par le tableau du tutoriel.
        static readonly Vector3[] Spots = { new Vector3(-5f, 2.2f, 0.5f), new Vector3(0f, 2.8f, 0.5f), new Vector3(4.5f, 2.4f, 0.5f) };
        static readonly Color[] Colors = { new Color(0.95f, 0.2f, 0.2f), new Color(0.25f, 0.55f, 1f), new Color(0.3f, 0.85f, 0.35f) };

        static readonly List<PracticeBalloon> alive = new List<PracticeBalloon>();

        Vector3 home;
        float phase;
        bool popped;

        // Pendant l'étape d'entraînement (et sur la carte), il y a 'missing' ballons à éclater : on les fait apparaître
        // devant le joueur s'il n'y en a plus aucun. Le reste du temps, on les retire.
        public static void Keep(bool wanted, int missing)
        {
            alive.RemoveAll(b => !b);
            if (!wanted || missing <= 0)
            {
                foreach (var b in alive) Destroy(b.gameObject);
                alive.Clear();
                return;
            }
            var map = WaveSpawner.Instance;   // la carte : les ballons se posent dans son repère
            if (alive.Count > 0 || !map) return;
            for (int i = 0; i < missing && i < Spots.Length; i++)
                Create(map.transform.position + Spots[i], Colors[i]);
        }

        static void Create(Vector3 position, Color color)
        {
            var go = Visuals.Primitive(PrimitiveType.Sphere);   // on garde son collider : la flèche doit le toucher
            go.name = "Ballon d'entraînement";
            go.transform.position = position;
            go.transform.localScale = new Vector3(Size, Size * 1.15f, Size);   // un peu allongé, comme un ballon
            go.AddComponent<ColorTint>().Set(color);
            var knot = Visuals.Box("Nœud", go.transform, new Vector3(0f, -0.52f, 0f), new Vector3(0.12f, 0.08f, 0.12f), color * 0.8f);
            knot.name = "Nœud";
            var balloon = go.AddComponent<PracticeBalloon>();
            balloon.home = position;
            balloon.phase = Random.value * 6f;
            alive.Add(balloon);
        }

        // Il danse doucement (haut, bas, un peu de côté) : vivant, mais facile à viser
        void Update()
        {
            if (popped) return;
            float t = Time.time + phase;
            transform.position = home + new Vector3(Mathf.Sin(t * 0.7f) * 0.15f, Mathf.Sin(t * 1.3f) * 0.12f, 0f);
        }

        // Une flèche l'a touché (Arrow) : il éclate, avec le son des vrais ballons
        public void Pop()
        {
            if (popped) return;
            popped = true;
            Sounds.Play(Sounds.BallonEclate, transform.position, 1f, Random.Range(0.95f, 1.1f));
            Tutorial.PracticeHit();
            alive.Remove(this);
            Destroy(gameObject);
        }
    }
}
