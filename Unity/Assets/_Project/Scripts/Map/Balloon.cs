using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un ballon suit la piste. Sa vie = ses couches (la couleur change à chaque couche perdue).
    // Il n'y a pas de « dégâts » : un tir a une perforation, le nombre de couches qu'il peut percer (voir Pop).
    // Sa sorte (BalloonKind) change sa taille, sa vitesse et sa résistance :
    //   Rapide = petit et vif ; Blindé = gris, chaque couche coûte 2 de perforation, insensible au ralentissement ;
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
        int layers;
        float speed;
        float slowFactor = 1f;
        float slowUntil;
        ColorTint tint;

        public BalloonKind Kind { get; private set; }
        bool Armored => Kind == BalloonKind.Blinde || Kind == BalloonKind.Dirigeable;

        // Distance parcourue : les singes visent le ballon le plus avancé.
        public float Progress { get; private set; }

        public void Init(WaveSpawner owner, List<Vector3> points, int layerCount, BalloonKind kind)
        {
            spawner = owner;
            path = points;
            layers = layerCount;
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


        // Un tir qui peut percer 'pierce' couches touche ce ballon. Renvoie la perforation dépensée :
        // 1 par couche, 2 par couche blindée. Un tir trop faible pour une couche blindée s'y arrête (tout est dépensé).
        public int Pop(int pierce)
        {
            if (pierce <= 0 || layers <= 0) return 0;
            int cost = Armored ? 2 : 1;
            int popped = Mathf.Min(layers, pierce / cost);
            if (popped == 0) return pierce;

            layers -= popped;
            if (layers <= 0) Destroy(gameObject);
            else UpdateColor();
            return popped * cost;
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
                    spawner.BalloonEscaped(layers);
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
                    int layer = Mathf.Clamp(layers - 1, 0, layerColors.Length - 1);
                    tint.Set(layerColors[layer]);
                    break;
            }
        }
    }
}
