using System.Collections;
using UnityEngine;

namespace SAE
{
    // Des vibrations différentes selon l'action, pour sentir ce qui se passe sans regarder.
    // Un motif = une suite d'impulsions (force 0 à 1, durée, pause avant la suivante), jouée dans une main ou les deux.
    //   Grab     : une banane attrapée (doux, bref)
    //   Coins    : une banane dans le panier (deux petits tics, « ding ding »)
    //   Golden   : une banane dorée dans le panier (trois tics qui montent)
    //   Hit      : la flèche a touché un ballon (tic sec dans la main de l'arc)
    //   BossPop  : un boss éclate et se divise (grosse secousse longue)
    //   Damage   : un ballon est passé, on perd des vies (grondement)
    //   Alarm    : vague spéciale annoncée (deux battements de cœur lents)
    //   Thunder  : le tonnerre gronde pendant l'orage du boss (une secousse sourde, puis un écho)
    //   Victory  : vague gagnée (trois tics rapides)   Defeat : vague perdue (long et sourd)
    // Les petites vibrations ponctuelles existantes (boutons, arc) passent toujours par PlayerRig.Buzz.
    // En mode PC il n'y a pas de manette : rien ne vibre, sans erreur.
    public static class Haptics
    {
        public struct Pulse
        {
            public float strength, duration, gap;
            public Pulse(float strength, float duration, float gap = 0f) { this.strength = strength; this.duration = duration; this.gap = gap; }
        }

        public static readonly Pulse[] Grab = { new Pulse(0.25f, 0.04f) };
        public static readonly Pulse[] Coins = { new Pulse(0.3f, 0.04f, 0.07f), new Pulse(0.3f, 0.04f) };
        public static readonly Pulse[] Golden = { new Pulse(0.35f, 0.05f, 0.07f), new Pulse(0.6f, 0.05f, 0.07f), new Pulse(0.95f, 0.12f) };
        public static readonly Pulse[] Hit = { new Pulse(0.45f, 0.03f) };
        public static readonly Pulse[] BossPop = { new Pulse(1f, 0.3f, 0.05f), new Pulse(0.5f, 0.2f) };
        public static readonly Pulse[] Damage = { new Pulse(0.5f, 0.06f, 0.04f), new Pulse(0.5f, 0.06f, 0.04f), new Pulse(0.5f, 0.06f) };
        public static readonly Pulse[] Alarm = { new Pulse(0.7f, 0.12f, 0.12f), new Pulse(0.5f, 0.12f, 0.5f), new Pulse(0.7f, 0.12f, 0.12f), new Pulse(0.5f, 0.12f) };
        public static readonly Pulse[] Thunder = { new Pulse(0.8f, 0.35f, 0.1f), new Pulse(0.35f, 0.4f) };
        public static readonly Pulse[] Victory = { new Pulse(0.5f, 0.05f, 0.06f), new Pulse(0.6f, 0.05f, 0.06f), new Pulse(0.9f, 0.15f) };
        public static readonly Pulse[] Defeat = { new Pulse(0.35f, 0.6f) };

        // Joue le motif dans la manette qui contient 'hand'
        public static void Play(Transform hand, Pulse[] pattern)
        {
            if (!hand || pattern == null) return;
            if (pattern.Length == 1) { PlayerRig.Buzz(hand, pattern[0].strength, pattern[0].duration); return; }
            Runner.Instance.StartCoroutine(Run(hand, pattern));
        }

        // Joue le motif dans les deux mains du joueur actif (événements de la partie : vague, boss, vies)
        public static void PlayBoth(Pulse[] pattern)
        {
            var rig = PlayerRig.Local;
            if (!rig) return;
            Play(rig.leftHand, pattern);
            if (rig.rightHand != rig.leftHand) Play(rig.rightHand, pattern);
        }

        static IEnumerator Run(Transform hand, Pulse[] pattern)
        {
            foreach (var p in pattern)
            {
                if (!hand) yield break;
                PlayerRig.Buzz(hand, p.strength, p.duration);
                float wait = p.duration + p.gap;
                if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
            }
        }

        // Un objet caché qui fait tourner les motifs (une classe statique ne peut pas lancer de coroutine)
        class Runner : MonoBehaviour
        {
            static Runner instance;
            public static Runner Instance
            {
                get
                {
                    if (!instance)
                    {
                        var go = new GameObject("Haptics") { hideFlags = HideFlags.HideInHierarchy };
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<Runner>();
                    }
                    return instance;
                }
            }
        }
    }
}
