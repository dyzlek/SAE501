using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SAE
{
    // Où est un joueur : son niveau (hub, carte, menu), sa tête et ses mains. INetworkSerializeByMemcpy : NGO l'envoie
    // tel quel, octet par octet (il n'y a que des nombres dedans).
    public struct CoopPose : INetworkSerializeByMemcpy, IEquatable<CoopPose>
    {
        public Level level;
        public bool vr;
        public Vector3 headPos, leftPos, rightPos;
        public Quaternion headRot, leftRot, rightRot;

        public bool Equals(CoopPose o) =>
            level == o.level && vr == o.vr && headPos == o.headPos && leftPos == o.leftPos && rightPos == o.rightPos
            && headRot == o.headRot && leftRot == o.leftRot && rightRot == o.rightRot;
    }

    // Le joueur réseau (le « Player Prefab » de NGO) : il y en a un par joueur dans la partie, l'hôte compris, chez tout le monde.
    //   - Le nôtre (IsOwner) écrit 20 fois par seconde notre tête et nos mains (Pose) ; chez les autres, il les montre
    //     avec un avatar (CoopAvatar).
    //   - Il porte les demandes de l'invité à l'hôte : des RPC (appels de méthode à distance) [Rpc(SendTo.Server)].
    //     L'invité appelle HitRpc(…) chez lui, la méthode s'exécute chez l'hôte. NGO garantit qu'elles arrivent,
    //     une seule fois et dans l'ordre. L'hôte y fait les mêmes appels que pour son propre joueur, pour que les règles
    //     (prix, placement, fusion) restent écrites à un seul endroit.
    //   - Ses flèches : ShotRpc [SendTo.NotMe], tout le monde sauf lui les voit voler.
    public class CoopPlayer : NetworkBehaviour
    {
        const float PoseInterval = 0.05f;   // 20 fois par seconde

        public static CoopPlayer Local { get; private set; }   // notre joueur réseau
        static readonly List<CoopPlayer> all = new List<CoopPlayer>();

        // Le numéro du joueur (0 = l'hôte, 1 à 3 = les invités) : donné par l'hôte, il choisit la couleur de l'avatar
        public readonly NetworkVariable<byte> Number = new NetworkVariable<byte>();

        // Tête et mains : c'est le joueur lui-même qui les écrit (Owner), les autres les lisent
        readonly NetworkVariable<CoopPose> pose = new NetworkVariable<CoopPose>(writePerm: NetworkVariableWritePermission.Owner);

        CoopAvatar avatar;
        float nextPose;

        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(gameObject);   // survit au rechargement des scènes (nouvelle partie de l'hôte)
            if (IsServer) Number.Value = FreeNumber();
            all.Add(this);
            if (IsOwner)
            {
                Local = this;
                Coop.Joined(Number.Value);
            }
            else avatar = CoopAvatar.Create(Number.Value, transform);   // enfant de cet objet : il disparaît avec lui
        }

        public override void OnNetworkDespawn()
        {
            all.Remove(this);
            if (Local == this) Local = null;
        }

        // Hôte : le plus petit numéro libre (un invité qui part libère le sien)
        byte FreeNumber()
        {
            if (OwnerClientId == NetworkManager.ServerClientId) return 0;
            for (byte n = 1; ; n++)
                if (!all.Exists(p => p != this && p.Number.Value == n)) return n;
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (!IsOwner)
            {
                avatar.SetPose(pose.Value);
                return;
            }
            if (Time.unscaledTime < nextPose) return;   // le menu arrête le jeu (timeScale = 0), pas le réseau
            nextPose = Time.unscaledTime + PoseInterval;
            var rig = PlayerRig.Local;
            if (!rig) return;
            pose.Value = new CoopPose
            {
                level = Levels.Current,
                vr = rig.IsVR,
                headPos = rig.head.position, headRot = rig.head.rotation,
                leftPos = rig.leftHand.position, leftRot = rig.leftHand.rotation,
                rightPos = rig.rightHand.position, rightRot = rig.rightHand.rotation,
            };
        }

        // --- Les demandes de l'invité, faites chez l'hôte. request = leur numéro (voir Coop.Request). ---

        [Rpc(SendTo.Server)]
        public void HitRpc(int request, NetworkBehaviourReference balloon, int pierce)
        {
            if (balloon.TryGet(out CoopBalloon b) && b.Target) b.Target.Pop(pierce);   // déjà éclaté chez l'hôte : rien à faire
            Done(request);
        }

        [Rpc(SendTo.Server)]
        public void SlowRpc(int request, NetworkBehaviourReference balloon, float factor, float seconds)
        {
            if (balloon.TryGet(out CoopBalloon b) && b.Target) b.Target.Slow(factor, seconds);
            Done(request);
        }

        [Rpc(SendTo.Server)]
        public void EarnRpc(int request, int amount, int stakeBack)
        {
            Economy.Earn(amount, null, stakeBack);
            Done(request);
        }

        [Rpc(SendTo.Server)]
        public void SpendRpc(int request, int amount)
        {
            Economy.SpendForPartner(amount);
            Done(request);
        }

        [Rpc(SendTo.Server)]
        public void InventoryRpc(int request, InventoryOp op, MonkeyType type, Rarity level)
        {
            var m = new Monkey(type, level);
            switch (op)
            {
                case InventoryOp.Add: GameState.AddToInventory(m); break;
                case InventoryOp.Take: GameState.TakeForPartner(m); break;
                case InventoryOp.Unlock: GameState.Unlock(m.type); break;
                case InventoryOp.ClearBoard: GameState.ClearBoard(); break;
            }
            Done(request);
        }

        [Rpc(SendTo.Server)]
        public void PlaceRpc(int request, Vector2 pos, bool holds, MonkeyType type, Rarity level)
        {
            // Le placement lit « le singe tenu » : le temps de l'appel, c'est celui de l'invité
            var mine = GameState.Held;
            GameState.Held = holds ? new Monkey(type, level) : (Monkey?)null;
            Placement.Apply(pos);
            GameState.Held = mine;
            Done(request);
        }

        [Rpc(SendTo.Server)]
        public void LaunchRpc(int request)
        {
            WaveSpawner.Launch();
            Done(request);
        }

        // La roulette : l'hôte appuie à la place de l'invité sur SA roulette (une seule table pour tous)
        [Rpc(SendTo.Server)]
        public void RouletteRpc(int request, RouletteOp op, RouletteColor color, float speed)
        {
            var table = RouletteTable.Instance;
            if (table)
                switch (op)
                {
                    case RouletteOp.Select: table.Select(color); break;
                    case RouletteOp.AddOne: table.Add(1); break;
                    case RouletteOp.AddTen: table.Add(10); break;
                    case RouletteOp.AddHundred: table.Add(100); break;
                    case RouletteOp.AllIn: table.AllIn(); break;
                    case RouletteOp.Clear: table.Clear(); break;
                    case RouletteOp.Spin: table.Launch(speed); break;
                }
            Done(request);
        }

        // La demande est faite : l'invité peut de nouveau écouter l'état de l'hôte (voir Coop.LastStateRequest)
        void Done(int request)
        {
            if (CoopGame.Instance) CoopGame.Instance.MarkDone(Number.Value, request);
        }

        // --- Les flèches ---

        [Rpc(SendTo.NotMe)]
        public void ShotRpc(Vector3 from, Vector3 velocity) => Coop.ShowShot(from, velocity);
    }
}
