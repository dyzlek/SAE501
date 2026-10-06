using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAE
{
    // Un singe posé sur la carte : il tire tout seul sur les ballons à sa portée.
    public class Tower : MonoBehaviour
    {

        Monkey monkey;
        float cooldown;

        public void Init(Monkey m) => monkey = m;

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            float range = MonkeyData.Range(monkey);
            var inRange = Balloon.All
                .Where(b => Flat(b.transform.position - transform.position).sqrMagnitude <= range * range)
                .OrderByDescending(b => b.Progress)
                .ToList();
            if (inRange.Count == 0) return;

            cooldown = MonkeyData.FireDelay(monkey);
            Fire(inRange);
        }

        void Fire(List<Balloon> targets)
        {
            float damage = MonkeyData.Damage(monkey);
            var hits = targets.Take(MonkeyData.Targets(monkey)).ToList();

            if (monkey.type == MonkeyType.Canon)
            {
                // Explosion : touche aussi les ballons proches de la cible.
                var center = hits[0].transform.position;
                hits = targets.Where(b => (b.transform.position - center).sqrMagnitude < 1.5f * 1.5f).ToList();
            }

            foreach (var b in hits)
            {
                Shot(b.transform.position);
                if (monkey.type == MonkeyType.Glace) b.Slow(0.4f, 1.5f);
                if (monkey.type == MonkeyType.Colle) b.Slow(0.5f, 3f);
                b.Hit(damage);
            }
        }

        // Trait bref entre le singe et la cible.
        void Shot(Vector3 to)
        {
            var go = new GameObject("Tir");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = Visuals.LineMaterial;
            line.positionCount = 2;
            line.SetPosition(0, transform.position + Vector3.up * 0.6f);
            line.SetPosition(1, to);
            line.widthMultiplier = 0.06f;
            line.startColor = line.endColor = MonkeyData.IsRainbow(monkey.level) ? Color.magenta : MonkeyData.RarityColor(monkey.level);
            Destroy(go, 0.08f);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
