using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SAE
{
    // Ce qu'un invité demande au comptoir de la bananeraie. Les 4 premiers suivent BananaStat (même ordre),
    // les 3 derniers suivent HarvesterStat : on passe de l'un à l'autre par un simple nombre.
    public enum OrchardOp : byte { Frequence, Pourriture, Valeur, Arbres, Singe, Vitesse, Cadence, Rendement }

    // Une banane, telle qu'elle passe sur le réseau
    public struct CoopBanana : INetworkSerializeByMemcpy, IEquatable<CoopBanana>
    {
        public int id;
        public byte tree;          // l'arbre d'où elle est tombée (sa place dans BananaOrchard.trees)
        public byte holder;        // qui la tient : un numéro de joueur, CoopOrchard.Monkey, ou CoopOrchard.Nobody
        public byte rot;           // pourriture, de 0 (fraîche) à CoopOrchard.RotSteps (pourrie)
        public bool golden;
        public Vector3 pos;
        public Quaternion rotation;

        public bool Equals(CoopBanana o) =>
            id == o.id && tree == o.tree && holder == o.holder && rot == o.rot && golden == o.golden && pos == o.pos && rotation == o.rotation;
    }

    // Un singe récolteur, tel qu'il passe sur le réseau
    public struct CoopHarvester : INetworkSerializeByMemcpy, IEquatable<CoopHarvester>
    {
        public const byte Active = 1, Walking = 2, Carrying = 4, Cheering = 8, Sulking = 16, Held = 32;

        public Vector3 pos;
        public Quaternion rotation;
        public byte flags;         // ce qu'il fait (les constantes ci-dessus, additionnées)
        public byte throwPhase;    // le geste du lancer, de 0 à 255

        public bool Has(byte flag) => (flags & flag) != 0;

        public bool Equals(CoopHarvester o) => pos == o.pos && rotation == o.rotation && flags == o.flags && throwPhase == o.throwPhase;
    }

    // Les niveaux du comptoir : arbres plantés et améliorations, singes récolteurs et leurs améliorations
    public struct CoopOrchardLevels : INetworkSerializeByMemcpy, IEquatable<CoopOrchardLevels>
    {
        public int trees, frequence, pourriture, valeur;
        public int harvesters, vitesse, cadence, rendement;

        public bool Equals(CoopOrchardLevels o) =>
            trees == o.trees && frequence == o.frequence && pourriture == o.pourriture && valeur == o.valeur
            && harvesters == o.harvesters && vitesse == o.vitesse && cadence == o.cadence && rendement == o.rendement;
    }

    // La bananeraie partagée du coop : tout le monde voit les mêmes bananes, les mêmes singes récolteurs, les mêmes arbres.
    // C'est l'hôte qui la fait tourner (chute des bananes, pourriture, singes, achats au comptoir), comme les ballons.
    //   - Chez l'hôte, on recopie à chaque image la bananeraie dans des NetworkList : NGO n'envoie que ce qui a changé
    //     (une banane posée qui ne bouge pas ne coûte rien).
    //   - Chez l'invité, son bananier ne produit plus rien : il crée une copie de chaque banane de l'hôte (Banane.Copie),
    //     qui glisse vers la position reçue. Ses singes récolteurs deviennent des « marionnettes » qui imitent ceux de l'hôte.
    //   - Un invité prend une banane : il la tient tout de suite chez lui, et le dit à l'hôte (TakeRpc). L'hôte la lui donne
    //     si personne ne l'a déjà (sinon l'invité la lâche, voir Read). Tant qu'il la tient ou qu'elle vole après son lancer,
    //     c'est l'invité qui la fait bouger et qui envoie sa position (MoveRpc). Posée par terre, il la rend à l'hôte
    //     (ReleaseRpc) ; dans le panier, il la vend chez lui (l'argent passe déjà par Economy) et prévient l'hôte (GoneRpc).
    //   - Au comptoir, l'invité ne paie pas chez lui : il demande à l'hôte (OrchardRpc), qui fait l'achat avec la bourse commune.
    // Un invité qui quitte le coop recharge sa partie (SaveGame.ReloadFromDisk) : il retrouve sa propre bananeraie.
    // Ce composant est sur le prefab réseau CoopPartie, à côté de CoopGame.
    public class CoopOrchard : NetworkBehaviour
    {
        public const byte Nobody = 255;       // personne ne tient la banane
        public const byte Monkey = 254;       // un singe récolteur la porte
        public const int RotSteps = 32;       // la pourriture est envoyée en 32 crans : assez fin pour la couleur, peu de messages
        const float SendInterval = 0.05f;     // invité : 20 positions par seconde pour la banane qu'il tient
        const float SettleTime = 0.3f;        // en secondes après le lâcher : avant, une banane ne « se pose » pas
        const float MaxFlight = 6f;           // en secondes : au-delà, on la rend à l'hôte là où elle est

        public static CoopOrchard Instance { get; private set; }

        // Invité connecté : sa bananeraie imite celle de l'hôte
        public static bool Mirroring => Coop.IsGuest && Coop.Connected;

        readonly NetworkVariable<CoopOrchardLevels> levels = new NetworkVariable<CoopOrchardLevels>();
        NetworkList<CoopBanana> bananas;
        NetworkList<CoopHarvester> harvesters;

        // Invité : il y a du nouveau à recopier ; ses propres bananes ont-elles déjà été retirées ?
        bool levelsChanged, bananasChanged, harvestersChanged, purged;
        readonly HashSet<int> seen = new HashSet<int>();

        // Invité : les bananes qu'il tient ou qu'il vient de lancer, et quand il les a lâchées (-1 : encore en main)
        class Thrown { public Banane banana; public float releasedAt = -1f; }
        readonly List<Thrown> mine = new List<Thrown>();
        float nextSend;

        void Awake()
        {
            bananas = new NetworkList<CoopBanana>();
            harvesters = new NetworkList<CoopHarvester>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer) return;
            levels.OnValueChanged += (_, _) => levelsChanged = true;
            bananas.OnListChanged += _ => bananasChanged = true;
            harvesters.OnListChanged += _ => harvestersChanged = true;
            levelsChanged = bananasChanged = harvestersChanged = true;   // l'état de départ, reçu avec l'objet
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            // L'hôte ferme le coop : les bananes que tenaient les invités retombent chez lui
            var orchard = BananaOrchard.Instance;
            if (!orchard) return;
            foreach (var tree in orchard.trees)
                for (int i = tree.BananesAuSol.Count - 1; i >= 0; i--)
                {
                    var b = tree.BananesAuSol[i];
                    if (b && b.TenuePar != Nobody) { b.FinPriseInvite(); b.Lachee(); }
                }
        }

        public override void OnDestroy()
        {
            bananas?.Dispose();
            harvesters?.Dispose();
            base.OnDestroy();
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            var orchard = BananaOrchard.Instance;
            var crew = HarvesterCrew.Instance;
            if (!orchard || !crew) return;   // la scène Hub n'est pas encore chargée
            if (IsServer) Write(orchard, crew);
            else Read(orchard, crew);
        }

        // --- Hôte : la bananeraie → le réseau ---

        void Write(BananaOrchard orchard, HarvesterCrew crew)
        {
            levels.Value = new CoopOrchardLevels
            {
                trees = orchard.Count,
                frequence = orchard.First.frequence.niveau,
                pourriture = orchard.First.pourriture.niveau,
                valeur = orchard.First.valeur.niveau,
                harvesters = crew.Count,
                vitesse = crew.Level(HarvesterStat.Vitesse),
                cadence = crew.Level(HarvesterStat.Cadence),
                rendement = crew.Level(HarvesterStat.Rendement),
            };

            int n = 0;
            for (int t = 0; t < orchard.trees.Length; t++)
                foreach (var b in orchard.trees[t].BananesAuSol)
                {
                    if (!b || b.Deposee) continue;
                    // Un invité qui la tenait est parti : elle tombe de sa main
                    if (b.TenuePar != Nobody && !CoopPlayer.Exists(b.TenuePar)) { b.FinPriseInvite(); b.Lachee(); }
                    Set(bananas, n++, new CoopBanana
                    {
                        id = b.Id,
                        tree = (byte)t,
                        holder = Holder(b),
                        rot = (byte)Mathf.RoundToInt(b.Progression * RotSteps),
                        golden = b.EstDoree,
                        pos = b.transform.position,
                        rotation = b.transform.rotation,
                    });
                }
            Trim(bananas, n);

            for (int i = 0; i < crew.monkeys.Length; i++)
            {
                var m = crew.monkeys[i];
                byte flags = 0;
                if (m.gameObject.activeSelf) flags |= CoopHarvester.Active;
                if (m.Walking) flags |= CoopHarvester.Walking;
                if (m.Carrying) flags |= CoopHarvester.Carrying;
                if (m.Cheering) flags |= CoopHarvester.Cheering;
                if (m.Sulking) flags |= CoopHarvester.Sulking;
                if (m.Held) flags |= CoopHarvester.Held;
                Set(harvesters, i, new CoopHarvester
                {
                    pos = m.transform.position,
                    rotation = m.transform.rotation,
                    flags = flags,
                    throwPhase = (byte)Mathf.RoundToInt(m.ThrowPhase * 255f),
                });
            }
            Trim(harvesters, crew.monkeys.Length);
        }

        // Qui tient la banane, vu de l'hôte : un invité, l'hôte lui-même (joueur 0), un singe, ou personne
        static byte Holder(Banane b)
        {
            if (b.TenuePar != Nobody) return b.TenuePar;
            if (!b.EnMain) return Nobody;
            return b.PriseParJoueur ? (byte)0 : Monkey;
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

        // --- Invité : le réseau → la bananeraie ---

        void Read(BananaOrchard orchard, HarvesterCrew crew)
        {
            if (!Coop.Connected) return;

            // D'abord les niveaux : une copie de banane prend la valeur de l'arbre au moment où elle apparaît
            if (levelsChanged)
            {
                levelsChanged = false;
                var l = levels.Value;
                orchard.ApplyFromHost(l.trees, l.frequence, l.pourriture, l.valeur);
                crew.ApplyFromHost(l.harvesters, l.vitesse, l.cadence, l.rendement);
            }

            // Une seule fois : les bananes que notre bananier avait fait tomber avant de rejoindre disparaissent,
            // et les boutons du comptoir réapparaissent (un bouton au maximum chez nous se cache, mais l'hôte en est peut-être
            // moins loin : il se recachera tout seul s'il est aussi au maximum chez l'hôte)
            if (!purged)
            {
                purged = true;
                foreach (var button in FindObjectsByType<UpgradeButton>(FindObjectsInactive.Include))
                    button.gameObject.SetActive(true);
                foreach (var button in FindObjectsByType<HarvesterButton>(FindObjectsInactive.Include))
                    button.gameObject.SetActive(true);
                foreach (var tree in orchard.trees)
                    for (int i = tree.BananesAuSol.Count - 1; i >= 0; i--)
                    {
                        var b = tree.BananesAuSol[i];
                        if (b && !b.Copie) b.Effacer();
                    }
            }

            if (bananasChanged)
            {
                bananasChanged = false;
                ReadBananas(orchard);
            }

            if (harvestersChanged)
            {
                harvestersChanged = false;
                for (int i = 0; i < harvesters.Count && i < crew.monkeys.Length; i++) crew.monkeys[i].Mirror(harvesters[i]);
            }
        }

        void ReadBananas(BananaOrchard orchard)
        {
            seen.Clear();
            foreach (var e in bananas)
            {
                seen.Add(e.id);
                var b = Find(orchard, e.id);
                if (!b)
                {
                    if (e.tree >= orchard.trees.Length) continue;
                    b = orchard.trees[e.tree].CreerCopie(e.id, e.golden, e.pos, e.rotation, e.rot == 0);
                    if (!b) continue;
                }

                bool free = e.holder == Nobody;
                if (b.Distante) b.SuivreHote(e.pos, e.rotation, (float)e.rot / RotSteps, free);
                // On la tient, mais l'hôte l'a donnée à un autre (il l'avait prise juste avant nous) : on la lâche
                else if (!free && e.holder != Coop.LocalId)
                {
                    Forget(b);
                    b.Reprendre();
                }
            }

            // Les copies que l'hôte n'a plus : vendues, pourries, ou ramassées par un singe jusqu'au panier
            foreach (var tree in orchard.trees)
                for (int i = tree.BananesAuSol.Count - 1; i >= 0; i--)
                {
                    var b = tree.BananesAuSol[i];
                    if (b && b.Copie && !seen.Contains(b.Id)) { Forget(b); b.Effacer(); }
                }
        }

        // La banane de ce numéro chez nous : chez l'invité, seulement parmi les copies (ses anciens numéros ne comptent plus)
        Banane Find(BananaOrchard orchard, int id)
        {
            foreach (var tree in orchard.trees)
                foreach (var b in tree.BananesAuSol)
                    if (b && b.Id == id && (IsServer || b.Copie)) return b;
            return null;
        }

        void Forget(Banane b) => mine.RemoveAll(t => t.banana == b);

        // Invité : la position des bananes qu'il tient ou qu'il a lancées, 20 fois par seconde, jusqu'à ce qu'elles se posent
        void Update()
        {
            if (!IsSpawned || IsServer || mine.Count == 0) return;
            if (Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + SendInterval;

            for (int i = mine.Count - 1; i >= 0; i--)
            {
                var t = mine[i];
                var b = t.banana;
                if (!b || b.Distante) { mine.RemoveAt(i); continue; }   // vendue, ou reprise par l'hôte
                var pos = b.transform.position;
                var rotation = b.transform.rotation;
                if (!b.EnMain)
                {
                    if (t.releasedAt < 0f) t.releasedAt = Time.time;
                    float flight = Time.time - t.releasedAt;
                    bool still = flight > SettleTime && b.Corps.linearVelocity.sqrMagnitude < 0.01f;
                    if (still || flight > MaxFlight)
                    {
                        // Posée : on la rend à l'hôte, qui la refait vivre (pourriture, singes) là où elle est
                        ReleaseRpc(b.Id, pos, rotation);
                        b.RendreDistante();
                        mine.RemoveAt(i);
                        continue;
                    }
                }
                else t.releasedAt = -1f;   // reprise en main avant de toucher le sol
                MoveRpc(b.Id, pos, rotation);
            }
        }

        // --- Appelé par Banane et les boutons du comptoir ---

        // Un joueur vient de prendre cette banane : chez l'invité, on la demande à l'hôte
        public static void Taken(Banane b)
        {
            if (!Mirroring || !Instance || !b.Copie) return;
            if (!Instance.mine.Exists(t => t.banana == b)) Instance.mine.Add(new Thrown { banana = b });
            Instance.TakeRpc(b.Id, Coop.LocalId);
        }

        // Une banane vient d'être vendue dans un panier (Banane.Deposer)
        public static void Sold(Banane b)
        {
            if (!Instance || !Instance.IsSpawned) return;
            byte kind = SoldKind(b);
            if (Mirroring)
            {
                // Chez l'invité, elle est vendue (et payée) chez lui : on prévient l'hôte, qui la retire pour tout le monde
                if (!b.Copie) return;
                Instance.Forget(b);
                Instance.GoneRpc(b.Id, Coop.LocalId);
                return;
            }
            if (Coop.IsHost) Instance.SoldRpc(b.transform.position, kind, b.PriseParJoueur ? (byte)0 : Monkey);
        }

        // 0 : pourrie (rien), 1 : banane, 2 : dorée
        static byte SoldKind(Banane b) => b.EstPourrie ? (byte)0 : b.EstDoree ? (byte)2 : (byte)1;

        // Invité : un bouton du comptoir de la bananeraie. L'hôte fait l'achat avec la bourse commune.
        public static void Ask(OrchardOp op)
        {
            if (Mirroring && Instance) Instance.OrchardRpc(op);
        }

        // --- Les demandes de l'invité, faites chez l'hôte ---

        [Rpc(SendTo.Server)]
        void TakeRpc(int id, byte player)
        {
            var orchard = BananaOrchard.Instance;
            var b = orchard ? Find(orchard, id) : null;
            // Déjà prise (par l'hôte, un singe, un autre invité) ou vendue : refusé, l'invité le verra dans la liste
            if (!b || b.EnMain || b.Deposee || b.TenuePar != Nobody) return;
            b.PriseParInvite(player);
        }

        [Rpc(SendTo.Server)]
        void MoveRpc(int id, Vector3 pos, Quaternion rotation)
        {
            var orchard = BananaOrchard.Instance;
            var b = orchard ? Find(orchard, id) : null;
            if (b && b.TenuePar != Nobody) b.SuivreInvite(pos, rotation);
        }

        [Rpc(SendTo.Server)]
        void ReleaseRpc(int id, Vector3 pos, Quaternion rotation)
        {
            var orchard = BananaOrchard.Instance;
            var b = orchard ? Find(orchard, id) : null;
            if (b && b.TenuePar != Nobody) b.PoseeParInvite(pos, rotation);
        }

        [Rpc(SendTo.Server)]
        void GoneRpc(int id, byte player)
        {
            var orchard = BananaOrchard.Instance;
            var b = orchard ? Find(orchard, id) : null;
            if (!b) return;
            byte kind = SoldKind(b);
            var pos = b.transform.position;
            b.Effacer();   // sans passer par le panier : l'invité a déjà été payé
            PlaySold(pos, kind);
            SoldRpc(pos, kind, player);
        }

        [Rpc(SendTo.Server)]
        void OrchardRpc(OrchardOp op)
        {
            var orchard = BananaOrchard.Instance;
            var crew = HarvesterCrew.Instance;
            if (!orchard || !crew) return;
            if (op <= OrchardOp.Arbres) orchard.Upgrade((BananaStat)op, orchard.transform.position);
            else if (op == OrchardOp.Singe) crew.Buy();
            else crew.Upgrade((HarvesterStat)(op - OrchardOp.Vitesse));
        }

        // Hôte → invités : une banane vient d'être vendue, on entend le son au panier (sauf celui qui l'a vendue : il l'a déjà)
        [Rpc(SendTo.NotServer)]
        void SoldRpc(Vector3 pos, byte kind, byte seller)
        {
            if (seller != Coop.LocalId) PlaySold(pos, kind);
        }

        // Le même son que le panier (Panier.RecevoirPart) : plus aigu et plus fort pour une dorée
        static void PlaySold(Vector3 near, byte kind)
        {
            if (kind == 0) return;
            var basket = Panier.LePlusProche(near);
            var pos = basket ? basket.transform.position : near;
            bool golden = kind == 2;
            Sounds.Play(Sounds.BananeVendue, pos, golden ? 1f : 0.7f, golden ? 1.35f : 1f);
        }
    }
}
