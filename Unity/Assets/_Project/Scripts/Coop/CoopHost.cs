using System.IO;
using UnityEngine;

namespace SAE
{
    // L'hôte fait ce qu'un invité lui demande (Coop) : les mêmes appels que si c'était son propre joueur,
    // pour que les règles (prix, placement, fusion) restent écrites à un seul endroit.
    public static class CoopHost
    {
        public static void Handle(CoopRequest kind, BinaryReader r)
        {
            switch (kind)
            {
                case CoopRequest.Hit:
                {
                    var b = Balloon.Find(r.ReadInt32());
                    int pierce = r.ReadInt32();
                    if (b) b.Pop(pierce);   // déjà éclaté chez l'hôte : rien à faire
                    break;
                }
                case CoopRequest.Slow:
                {
                    var b = Balloon.Find(r.ReadInt32());
                    float factor = r.ReadSingle(), seconds = r.ReadSingle();
                    if (b) b.Slow(factor, seconds);
                    break;
                }
                case CoopRequest.Earn:
                {
                    int amount = r.ReadInt32(), stakeBack = r.ReadInt32();
                    Economy.Earn(amount, null, stakeBack);
                    break;
                }
                case CoopRequest.Spend:
                    Economy.SpendForPartner(r.ReadInt32());
                    break;
                case CoopRequest.Inventory:
                {
                    var op = (InventoryOp)r.ReadByte();
                    var m = new Monkey((MonkeyType)r.ReadByte(), (Rarity)r.ReadByte());
                    switch (op)
                    {
                        case InventoryOp.Add: GameState.AddToInventory(m); break;
                        case InventoryOp.Take: GameState.TakeForPartner(m); break;
                        case InventoryOp.Unlock: GameState.Unlock(m.type); break;
                        case InventoryOp.ClearBoard: GameState.ClearBoard(); break;
                    }
                    break;
                }
                case CoopRequest.Place:
                {
                    var pos = new Vector2(r.ReadSingle(), r.ReadSingle());
                    bool holds = r.ReadBoolean();
                    var held = new Monkey((MonkeyType)r.ReadByte(), (Rarity)r.ReadByte());
                    // Le placement lit « le singe tenu » : le temps de l'appel, c'est celui de l'invité
                    var mine = GameState.Held;
                    GameState.Held = holds ? held : (Monkey?)null;
                    Placement.Apply(pos);
                    GameState.Held = mine;
                    break;
                }
                case CoopRequest.Launch:
                    WaveSpawner.Launch();
                    break;
            }
        }
    }
}
