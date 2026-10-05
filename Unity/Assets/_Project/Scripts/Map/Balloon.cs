using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un ballon suit la piste. Ses points de vie = ses couches (la couleur change à chaque couche perdue).
    // Cliquer dessus le touche : c'est la place de l'arc de Quincy en attendant la VR.
    public class Balloon : MonoBehaviour, IClickable
    {
        public static readonly List<Balloon> All = new List<Balloon>();

        static readonly Color[] layerColors =
        {
            new Color(0.9f, 0.1f, 0.1f), new Color(0.2f, 0.5f, 1f), new Color(0.2f, 0.8f, 0.2f),
            new Color(1f, 0.9f, 0.1f), new Color(1f, 0.4f, 0.8f), new Color(0.1f, 0.1f, 0.1f),
        };

        public float baseSpeed = 2.5f;

        WaveSpawner spawner;
        List<Vector3> path;
        int nextPoint = 1;
        float hp;
        float slowFactor = 1f;
        float slowUntil;
        ColorTint tint;

        // Distance parcourue : les singes visent le ballon le plus avancé.
        public float Progress { get; private set; }

        public string GetHint(Vector3 point) => $"Ballon ({Mathf.CeilToInt(hp)} couche(s)) : tirer";

        public void Init(WaveSpawner owner, List<Vector3> points, int layers)
        {
            spawner = owner;
            path = points;
            hp = layers;
            transform.position = path[0];
            tint = GetComponent<ColorTint>();
            UpdateColor();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void OnClick(PlayerController player, Vector3 point) => Hit(1f);

        public void Hit(float damage)
        {
            if (damage <= 0f || hp <= 0f) return;
            hp -= damage;
            if (hp <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            UpdateColor();
        }

        public void Slow(float factor, float duration)
        {
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        void Update()
        {
            if (Time.time > slowUntil) slowFactor = 1f;
            float step = baseSpeed * slowFactor * Time.deltaTime;
            Progress += step;

            var target = path[nextPoint];
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
            int layer = Mathf.Clamp(Mathf.CeilToInt(hp) - 1, 0, layerColors.Length - 1);
            if (tint) tint.Set(layerColors[layer]);
        }
    }
}
