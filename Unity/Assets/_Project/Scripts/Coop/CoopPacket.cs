using System.IO;
using UnityEngine;

namespace SAE
{
    // Les sortes de messages du coop (le 1er octet utile de chaque paquet).
    public enum CoopMsg : byte
    {
        Hello,      // invité → tout le réseau : « la partie 4821, mot de passe … ? » (chaque seconde tant qu'on n'a pas de réponse)
        Welcome,    // hôte → invité : « oui, tu es le joueur n »
        Refused,    // hôte → invité : « non » (mauvais mot de passe, partie pleine)
        Bye,        // on quitte la partie
        Pose,       // la tête et les mains d'un joueur (et son niveau : hub, carte, menu)
        State,      // hôte → invité : la bourse, la bibliothèque, les singes posés, la vague
        Balloons,   // hôte → invité : les ballons (30 par paquet)
        Shot,       // une flèche tirée, pour la voir chez les autres
        Request,    // invité → hôte : une demande numérotée (voir CoopRequest), renvoyée tant qu'elle n'est pas reçue
        Ack,        // hôte → invité : « j'ai fait tes demandes jusqu'au numéro n »
    }

    // Ce qu'un invité demande à l'hôte de faire à sa place
    public enum CoopRequest : byte { Hit, Slow, Earn, Spend, Inventory, Place, Launch }

    // Les changements de la bibliothèque (GameState)
    public enum InventoryOp : byte { Add, Take, Unlock, ClearBoard }

    // Écrire et lire un paquet : un en-tête (Magic, pour ignorer les paquets d'autres jeux), le type, puis les données.
    // BinaryWriter sait écrire les nombres et les textes ; on lui apprend les vecteurs et les rotations.
    public static class CoopPacket
    {
        const ushort Magic = 0x5AE1;   // « SAE 1 » : change si le format des paquets change

        public static BinaryWriter Begin(CoopMsg type)
        {
            var w = new BinaryWriter(new MemoryStream());
            w.Write(Magic);
            w.Write((byte)type);
            return w;
        }

        public static byte[] Bytes(BinaryWriter w) => ((MemoryStream)w.BaseStream).ToArray();

        // Lit l'en-tête : false si ce n'est pas un paquet du jeu
        public static bool Open(byte[] data, out BinaryReader r, out CoopMsg type)
        {
            r = new BinaryReader(new MemoryStream(data));
            type = default;
            if (data.Length < 3 || r.ReadUInt16() != Magic) return false;
            type = (CoopMsg)r.ReadByte();
            return true;
        }

        public static void Write(this BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        public static void Write(this BinaryWriter w, Quaternion q) { w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w); }
        public static Vector3 ReadVector3(this BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        public static Quaternion ReadQuaternion(this BinaryReader r) => new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }
}
