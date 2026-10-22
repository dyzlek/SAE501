using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SAE
{
    // Les ballons du coop. Chez l'hôte, ce sont les vrais (ils suivent la piste). Chez l'invité, ce sont des copies
    // (Balloon.Remote) : l'hôte envoie 15 fois par seconde le numéro, la sorte, les couches et la position de chacun,
    // par paquets de 30 (un paquet reste petit, il passe d'un coup sur le Wi-Fi).
    // Un ballon que l'hôte n'envoie plus (éclaté, sorti de la piste) disparaît chez l'invité au bout de 0,5 s.
    public static class CoopBalloons
    {
        const int PerPacket = 30;
        const float ForgetAfter = 0.5f;   // secondes sans nouvelles avant d'effacer la copie
        const float PoppedMemory = 2f;    // un ballon éclaté chez l'invité n'est pas recréé pendant 2 s

        // Invité : les copies, la dernière fois qu'on les a reçues, et ceux qu'on vient d'éclater (numéro de la demande)
        static readonly Dictionary<int, Balloon> copies = new Dictionary<int, Balloon>();
        static readonly Dictionary<int, float> lastSeen = new Dictionary<int, float>();
        static readonly Dictionary<int, (int request, float time)> popped = new Dictionary<int, (int, float)>();
        static readonly List<int> toForget = new List<int>();

        // --- Hôte ---

        public static List<byte[]> Packets(int doneRequest)
        {
            var packets = new List<byte[]>();
            var all = Balloon.All;
            for (int start = 0; start < all.Count; start += PerPacket)
            {
                int n = Mathf.Min(PerPacket, all.Count - start);
                var w = CoopPacket.Begin(CoopMsg.Balloons);
                w.Write(doneRequest);
                w.Write((byte)n);
                for (int i = start; i < start + n; i++)
                {
                    var b = all[i];
                    w.Write(b.Id);
                    w.Write((byte)b.Kind);
                    w.Write((short)b.Layers);
                    w.Write(b.transform.position);
                    w.Write(b.transform.rotation);
                }
                packets.Add(CoopPacket.Bytes(w));
            }
            return packets;
        }

        // --- Invité ---

        public static void Read(BinaryReader r)
        {
            int doneRequest = r.ReadInt32();
            int n = r.ReadByte();
            float now = Time.unscaledTime;
            for (int i = 0; i < n; i++)
            {
                int id = r.ReadInt32();
                var kind = (BalloonKind)r.ReadByte();
                int layers = r.ReadInt16();
                var pos = r.ReadVector3();
                var rot = r.ReadQuaternion();

                // Éclaté chez nous, mais l'hôte n'a pas encore reçu notre tir : on ne le fait pas réapparaître
                if (popped.TryGetValue(id, out var p) && doneRequest < p.request) continue;

                if (!copies.TryGetValue(id, out var b) || !b)
                {
                    var spawner = WaveSpawner.Instance;
                    if (!spawner) continue;   // la carte n'est pas encore chargée
                    copies[id] = b = spawner.SpawnRemote(kind, layers, id);
                }
                b.ApplyRemote(pos, rot, layers, doneRequest);
                lastSeen[id] = now;
            }
        }

        // Chaque image (invité) : on efface les copies dont l'hôte ne parle plus
        public static void Tick()
        {
            float now = Time.unscaledTime;
            toForget.Clear();
            foreach (var pair in lastSeen)
                if (now - pair.Value > ForgetAfter) toForget.Add(pair.Key);
            foreach (int id in toForget)
            {
                if (copies.TryGetValue(id, out var b) && b) Object.Destroy(b.gameObject);
                copies.Remove(id);
                lastSeen.Remove(id);
            }

            toForget.Clear();
            foreach (var pair in popped)
                if (now - pair.Value.time > PoppedMemory) toForget.Add(pair.Key);
            foreach (int id in toForget) popped.Remove(id);
        }

        // Une copie vient d'éclater sous nos flèches (Balloon.Pop)
        public static void Popped(int id, int request)
        {
            popped[id] = (request, Time.unscaledTime);
            copies.Remove(id);
            lastSeen.Remove(id);
        }

        public static void Clear()
        {
            foreach (var b in copies.Values) if (b) Object.Destroy(b.gameObject);
            copies.Clear();
            lastSeen.Clear();
            popped.Clear();
        }
    }
}
