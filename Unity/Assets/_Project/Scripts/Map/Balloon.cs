using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un ballon suit la piste. Ses points de vie = ses couches (la couleur change à chaque couche perdue).
    // Sa sorte (BalloonKind) change sa taille, sa vitesse et sa résistance :
    //   Rapide = petit et vif ; Blindé = gris, moitié moins de dégâts, insensible au ralentissement ;
    //   Boss = gros ballon violet foncé et lent ; Dirigeable = le boss final rouge, énorme, insensible au ralentissement.
    // S'il atteint la sortie, il retire autant de vies qu'il lui reste de couches.
    public class Balloon : MonoBehaviour
    {
        public static readonly List<Balloon> All = new List<Balloon>();

        static readonly Color[] layerColors =
        {
            new Color(0.9f, 0.1f, 0.1f), new Color(0.2f, 0.5f, 1f), new Color(0.2f, 0.8f, 0.2f),
            new Color(1f, 0.9f, 0.1f), new Color(1f, 0.4f, 0.8f), new Color(0.1f, 0.1f, 0.1f),
        };
        static readonly Color ArmorColor = new Color(0.55f, 0.57f, 0.6f);
        static readonly Color BossColor = new Color(0.35f, 0.1f, 0.45f);
        static readonly Color BlimpColor = new Color(0.8f, 0.1f, 0.1f);

        public float baseSpeed = 2.5f;

        WaveSpawner spawner;
        List<Vector3> path;
        int nextPoint = 1;
        float hp;
        float speed;
        float slowFactor = 1f;
        float slowUntil;
        ColorTint tint;

        public BalloonKind Kind { get; private set; }
        bool Armored => Kind == BalloonKind.Blinde || Kind == BalloonKind.Dirigeable;

        // Distance parcourue : les singes visent le ballon le plus avancé.
        public float Progress { get; private set; }

        public void Init(WaveSpawner owner, List<Vector3> points, int layers, BalloonKind kind)
        {
            spawner = owner;
            path = points;
            hp = layers;
            Kind = kind;
            transform.position = path[0];
            tint = GetComponent<ColorTint>();

            // Taille et vitesse selon la sorte
            (float size, float speedFactor) = kind switch
            {
                BalloonKind.Rapide => (0.65f, 1.7f),
                BalloonKind.Blinde => (1f, 0.8f),
                BalloonKind.Boss => (1.8f, 0.6f),
                BalloonKind.Dirigeable => (2.2f, 0.35f),
                _ => (0.9f, 1f),
            };
            transform.localScale = kind == BalloonKind.Dirigeable ? new Vector3(size, size * 1.6f, size) : Vector3.one * size;
            speed = baseSpeed * speedFactor;
            UpdateColor();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);


        public void Hit(float damage)
        {
            if (damage <= 0f || hp <= 0f) return;
            hp -= Armored ? damage * 0.5f : damage;
            if (hp <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            UpdateColor();
        }

        public void Slow(float factor, float duration)
        {
            if (Armored) return;   // le blindage ne se laisse ni geler ni coller
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        void Update()
        {
            if (Time.time > slowUntil) slowFactor = 1f;
            float step = speed * slowFactor * Time.deltaTime;
            Progress += step;

            var target = path[nextPoint];
            // Le dirigeable est couché dans le sens de la marche
            if (Kind == BalloonKind.Dirigeable && target != transform.position)
                transform.rotation = Quaternion.LookRotation(target - transform.position) * Quaternion.Euler(90f, 0f, 0f);

            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                nextPoint++;
                if (nextPoint >= path.Count)
                {
                    spawner.BalloonEscaped(Mathf.CeilToInt(hp));
                    Destroy(gameObject);
                }
            }
        }

        void UpdateColor()
        {
            if (!tint) return;
            switch (Kind)
            {
                case BalloonKind.Blinde: tint.Set(ArmorColor); break;
                case BalloonKind.Boss: tint.Set(BossColor); break;
                case BalloonKind.Dirigeable: tint.Set(BlimpColor); break;
                default:
                    int layer = Mathf.Clamp(Mathf.CeilToInt(hp) - 1, 0, layerColors.Length - 1);
                    tint.Set(layerColors[layer]);
                    break;
            }
        }
    }
}
