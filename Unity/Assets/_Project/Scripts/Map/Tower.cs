using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAE
{
    // Un singe posé sur la carte : il tire tout seul sur les ballons à sa portée.
    // Il fait son geste (TowerAnimator), puis lance son projectile (Projectile) : le ballon est touché quand il arrive.
    // Le Glace, lui, frappe le sol : une onde de froid bleue ralentit tout autour de lui d'un coup.
    public class Tower : MonoBehaviour
    {
        const int MaxProjectiles = 4;   // au plus 4 projectiles par tir (le Tireur en vise 4) : au-delà, ça ne se voit plus
        static readonly Color FrostColor = new Color(0.55f, 0.85f, 1f);

        Monkey monkey;
        TowerAnimator animator;
        float cooldown;

        public void Init(Monkey m)
        {
            monkey = m;
            animator = GetComponent<TowerAnimator>();
        }

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
            if (animator) animator.Attack(inRange[0].transform.position);
            StartCoroutine(Fire(inRange));
        }

        // Le tir part au moment où le bras passe devant (TowerAnimator.ReleaseDelay).
        IEnumerator Fire(List<Balloon> targets)
        {
            if (animator) yield return new WaitForSeconds(TowerAnimator.ReleaseDelay);
            targets.RemoveAll(b => !b);   // éclatés pendant le geste
            if (targets.Count == 0) yield break;

            if (monkey.type == MonkeyType.Glace)
            {
                Shockwave.Spawn(transform.position, MonkeyData.Range(monkey), FrostColor, false);
                foreach (var b in targets) Hit(b);
                yield break;
            }

            var hits = targets.Take(MonkeyData.Targets(monkey)).ToList();
            var from = animator ? animator.LaunchPoint : transform.position + Vector3.up * 0.6f;
            for (int i = 0; i < hits.Count; i++)
            {
                var b = hits[i];
                if (monkey.type == MonkeyType.Sniper) Shot(from, b.transform.position);   // la traînée de la balle
                // Le projectile touche en arrivant ; sans modèle (ou au-delà de MaxProjectiles), on touche tout de suite
                if (i >= MaxProjectiles || !Projectile.Launch(monkey.type, from, b, () => Hit(b))) Hit(b);
            }
        }

        // Ce que fait un tir sur un ballon : ralentir (Glace, Colle), exploser autour (Canon), percer des couches.
        void Hit(Balloon b)
        {
            if (!b) return;
            int pierce = MonkeyData.Pierce(monkey);
            if (monkey.type == MonkeyType.Canon)
            {
                // Explosion : touche aussi les ballons proches de la cible
                var center = b.transform.position;
                Shockwave.Spawn(center, 1.5f);
                foreach (var near in Balloon.All.Where(o => (o.transform.position - center).sqrMagnitude < 1.5f * 1.5f).ToList())
                    near.Pop(pierce);
                return;
            }
            if (monkey.type == MonkeyType.Glace) b.Slow(0.4f, 1.5f);
            if (monkey.type == MonkeyType.Colle) b.Slow(0.5f, 3f);
            b.Pop(pierce);
        }

        // Trait bref entre le singe et la cible.
        void Shot(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Tir");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = Visuals.LineMaterial;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.widthMultiplier = 0.06f;
            line.startColor = line.endColor = MonkeyData.IsRainbow(monkey.level) ? Color.magenta : MonkeyData.RarityColor(monkey.level);
            Destroy(go, 0.08f);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
