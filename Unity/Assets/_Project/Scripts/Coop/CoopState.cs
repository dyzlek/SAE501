using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SAE
{
    // L'état partagé que l'hôte envoie aux invités 5 fois par seconde (Coop) : la bourse, la bibliothèque,
    // les types débloqués, les singes posés et la vague en cours. L'invité le recopie tel quel : l'hôte fait foi.
    public static class CoopState
    {
        // doneRequest : la dernière demande de cet invité que l'hôte a faite (voir Coop.LastStateRequest)
        public static byte[] Packet(int doneRequest)
        {
            var w = CoopPacket.Begin(CoopMsg.State);
            w.Write(doneRequest);
            w.Write(GameState.Money);
            w.Write(GameState.WavesWon);
            w.Write(GameState.BananasEarned);

            var owned = GameState.OwnedFlat();
            w.Write(owned.Length);
            foreach (int n in owned) w.Write(n);
            var unlocked = GameState.UnlockedCopy();
            w.Write(unlocked.Length);
            foreach (bool u in unlocked) w.Write(u);

            w.Write(GameState.Placed.Count);
            foreach (var p in GameState.Placed)
            {
                w.Write((byte)p.monkey.type);
                w.Write((byte)p.monkey.level);
                w.Write(p.pos.x);
                w.Write(p.pos.y);
            }

            var spawner = WaveSpawner.Instance;
            w.Write(spawner != null);
            if (spawner)
            {
                w.Write(spawner.Wave);
                w.Write(spawner.Lives);
                w.Write(spawner.Running);
                w.Write(spawner.Won);
                w.Write(spawner.Status);
            }
            return CoopPacket.Bytes(w);
        }

        public static void Read(BinaryReader r)
        {
            int doneRequest = r.ReadInt32();
            int money = r.ReadInt32(), wavesWon = r.ReadInt32(), earned = r.ReadInt32();
            var owned = new int[r.ReadInt32()];
            for (int i = 0; i < owned.Length; i++) owned[i] = r.ReadInt32();
            var unlocked = new bool[r.ReadInt32()];
            for (int i = 0; i < unlocked.Length; i++) unlocked[i] = r.ReadBoolean();
            var placed = new List<PlacedMonkey>();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var m = new Monkey((MonkeyType)r.ReadByte(), (Rarity)r.ReadByte());
                placed.Add(new PlacedMonkey(m, new Vector2(r.ReadSingle(), r.ReadSingle())));
            }

            // Une de nos demandes n'est pas encore faite chez l'hôte : son état est en retard sur le nôtre, on attend le suivant
            if (doneRequest >= Coop.LastStateRequest)
                GameState.ApplyFromHost(money, wavesWon, earned, owned, unlocked, placed);

            if (r.ReadBoolean())
            {
                int wave = r.ReadInt32(), lives = r.ReadInt32();
                bool running = r.ReadBoolean(), won = r.ReadBoolean();
                string status = r.ReadString();
                if (WaveSpawner.Instance) WaveSpawner.Instance.ApplyRemote(wave, lives, running, won, status);
            }
        }
    }
}
