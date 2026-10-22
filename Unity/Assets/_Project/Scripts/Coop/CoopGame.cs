using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SAE
{
    // Un singe posé, tel qu'il passe sur le réseau
    public struct CoopPlaced : INetworkSerializeByMemcpy, IEquatable<CoopPlaced>
    {
        public MonkeyType type;
        public Rarity level;
        public Vector2 pos;

        public bool Equals(CoopPlaced o) => type == o.type && level == o.level && pos == o.pos;
    }

    // L'état partagé du coop, en NetworkVariable et NetworkList : la bourse, la bibliothèque, les types débloqués,
    // les singes posés et la vague en cours. Un seul objet dans la partie, créé par l'hôte (Coop.SpawnShared).
    // Chez l'hôte, on recopie l'état du jeu dedans à chaque image ; NGO n'envoie que ce qui a changé.
    // Chez l'invité, dès que quelque chose change, on le recopie dans le jeu (GameState.ApplyFromHost) : l'hôte fait foi.
    //
    // done : pour chaque joueur, le numéro de sa dernière demande faite chez l'hôte. Il change dans la même image que
    // l'état (même message réseau) : l'invité sait si l'état qu'il reçoit contient déjà ses dernières demandes.
    public class CoopGame : NetworkBehaviour
    {
        public static CoopGame Instance { get; private set; }

        readonly NetworkVariable<int> money = new NetworkVariable<int>();
        readonly NetworkVariable<int> wavesWon = new NetworkVariable<int>();
        readonly NetworkVariable<int> bananasEarned = new NetworkVariable<int>();
        NetworkList<int> owned;           // le nombre de chaque singe dans la bibliothèque (type × rareté, à plat)
        NetworkList<bool> unlocked;       // les types débloqués
        NetworkList<CoopPlaced> placed;   // les singes posés sur la carte
        NetworkList<int> done;            // par numéro de joueur (0 à 3)

        // La vague (seulement si la carte est chargée chez l'hôte)
        readonly NetworkVariable<bool> hasWave = new NetworkVariable<bool>();
        readonly NetworkVariable<int> wave = new NetworkVariable<int>();
        readonly NetworkVariable<int> lives = new NetworkVariable<int>();
        readonly NetworkVariable<bool> running = new NetworkVariable<bool>();
        readonly NetworkVariable<bool> won = new NetworkVariable<bool>();
        readonly NetworkVariable<FixedString512Bytes> status = new NetworkVariable<FixedString512Bytes>();

        bool stateChanged, waveChanged;   // invité : il y a du nouveau à recopier dans le jeu

        // Les NetworkList se créent dans Awake (conseil de NGO : créées plus tôt, elles fuient de la mémoire dans l'éditeur)
        void Awake()
        {
            owned = new NetworkList<int>();
            unlocked = new NetworkList<bool>();
            placed = new NetworkList<CoopPlaced>();
            done = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            if (IsServer)
            {
                for (int i = 0; i <= Coop.MaxGuests; i++) done.Add(0);
                return;
            }
            // Invité : on note ce qui change, on le recopie dans LateUpdate (une fois, même si dix valeurs ont changé)
            money.OnValueChanged += (_, _) => stateChanged = true;
            wavesWon.OnValueChanged += (_, _) => stateChanged = true;
            bananasEarned.OnValueChanged += (_, _) => stateChanged = true;
            owned.OnListChanged += _ => stateChanged = true;
            unlocked.OnListChanged += _ => stateChanged = true;
            placed.OnListChanged += _ => stateChanged = true;
            done.OnListChanged += _ => stateChanged = true;
            hasWave.OnValueChanged += (_, _) => waveChanged = true;
            wave.OnValueChanged += (_, _) => waveChanged = true;
            lives.OnValueChanged += (_, _) => waveChanged = true;
            running.OnValueChanged += (_, _) => waveChanged = true;
            won.OnValueChanged += (_, _) => waveChanged = true;
            status.OnValueChanged += (_, _) => waveChanged = true;
            stateChanged = waveChanged = true;   // l'état de départ, reçu avec l'objet
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        public override void OnDestroy()
        {
            owned?.Dispose();
            unlocked?.Dispose();
            placed?.Dispose();
            done?.Dispose();
            base.OnDestroy();
        }

        public int DoneFor(byte number) => number < done.Count ? done[number] : 0;

        // Hôte : la demande 'request' du joueur 'number' est faite
        public void MarkDone(byte number, int request)
        {
            if (number < done.Count && request > done[number]) done[number] = request;
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer) Write();
            else Read();
        }

        // --- Hôte : le jeu → le réseau (une NetworkVariable ne part que si sa valeur a changé) ---

        void Write()
        {
            money.Value = GameState.Money;
            wavesWon.Value = GameState.WavesWon;
            bananasEarned.Value = GameState.BananasEarned;

            int i = 0;
            for (int t = 0; t < MonkeyData.TypeCount; t++)
                for (int l = 0; l < MonkeyData.LevelCount; l++)
                    Set(owned, i++, GameState.Count(new Monkey((MonkeyType)t, (Rarity)l)));
            Trim(owned, i);

            for (int t = 0; t < MonkeyData.TypeCount; t++) Set(unlocked, t, GameState.IsUnlocked((MonkeyType)t));
            Trim(unlocked, MonkeyData.TypeCount);

            for (int p = 0; p < GameState.Placed.Count; p++)
            {
                var m = GameState.Placed[p];
                Set(placed, p, new CoopPlaced { type = m.monkey.type, level = m.monkey.level, pos = m.pos });
            }
            Trim(placed, GameState.Placed.Count);

            var spawner = WaveSpawner.Instance;
            hasWave.Value = spawner;
            if (!spawner) return;
            wave.Value = spawner.Wave;
            lives.Value = spawner.Lives;
            running.Value = spawner.Running;
            won.Value = spawner.Won;
            status.Value = spawner.Status;
        }

        static void Set<T>(NetworkList<T> list, int i, T value) where T : unmanaged, IEquatable<T>
        {
            if (i >= list.Count) list.Add(value);
            else if (!list[i].Equals(value)) list[i] = value;
        }

        static void Trim<T>(NetworkList<T> list, int count) where T : unmanaged, IEquatable<T>
        {
            while (list.Count > count) list.RemoveAt(list.Count - 1);
        }

        // --- Invité : le réseau → le jeu ---

        void Read()
        {
            // Une de nos demandes n'est pas encore faite chez l'hôte : son état est en retard sur le nôtre, on attend
            if (stateChanged && DoneFor(Coop.LocalId) >= Coop.LastStateRequest)
            {
                stateChanged = false;
                var ownedFlat = new int[owned.Count];
                for (int i = 0; i < ownedFlat.Length; i++) ownedFlat[i] = owned[i];
                var unlockedTypes = new bool[unlocked.Count];
                for (int i = 0; i < unlockedTypes.Length; i++) unlockedTypes[i] = unlocked[i];
                var monkeys = new List<PlacedMonkey>(placed.Count);
                foreach (var p in placed) monkeys.Add(new PlacedMonkey(new Monkey(p.type, p.level), p.pos));
                GameState.ApplyFromHost(money.Value, wavesWon.Value, bananasEarned.Value, ownedFlat, unlockedTypes, monkeys);
                Coop.StateApplied();
            }

            // La vague : la carte n'est peut-être pas encore chargée chez nous, on réessaiera
            var spawner = WaveSpawner.Instance;
            if (waveChanged && hasWave.Value && spawner)
            {
                waveChanged = false;
                spawner.ApplyRemote(wave.Value, lives.Value, running.Value, won.Value, status.Value.ToString());
            }
        }
    }
}
