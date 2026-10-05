using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le plateau du hub = la carte en miniature, en direct.
    // - Il affiche une copie réduite de tout ce qui porte Mirrored (ballons, singes, joueurs).
    // - On y pose les singes librement (hors piste, dans la carte) ; poser sur un singe identique = fusion.
    public class Board : MonoBehaviour, IClickable
    {
        public Transform mapRoot;          // le centre de la vraie carte
        public float scale = 0.0733f;      // taille du plateau / taille de la carte

        // Rayon d'un singe sur la carte (m) : sert à l'espacement et à viser un singe déjà posé.
        const float MonkeyRadius = TowerManager.TowerSize / 2f;

        readonly Dictionary<Mirrored, GameObject> proxies = new Dictionary<Mirrored, GameObject>();
        readonly List<Mirrored> toRemove = new List<Mirrored>();
        GameObject ghost;
        Monkey? ghostMonkey;

        // ---------- Conversions plateau <-> carte ----------
        Vector2 BoardPointToMap(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint) / scale;
            return new Vector2(local.x, local.z);
        }

        Vector3 MapToBoard(Vector3 worldOnMap) =>
            transform.TransformPoint(mapRoot.InverseTransformPoint(worldOnMap) * scale);

        // ---------- Interaction ----------
        public string GetHint(Vector3 point)
        {
            var pos = BoardPointToMap(point);
            var held = GameState.Held;
            var near = GameState.Nearest(pos, MonkeyRadius * 2f);

            if (held == null) return near == null ? "Plateau : prends un singe dans la bibliothèque" : $"Reprendre : {near.monkey}";
            if (near != null)
                return near.monkey.CanFuseWith(held.Value) ? $"FUSION → {near.monkey.Upgraded()}" : $"Échanger avec : {near.monkey}";
            return MapLayout.CanPlace(pos, MonkeyRadius) ? $"Poser : {held}" : "Impossible : sur la piste ou hors de la carte";
        }

        public void OnClick(PlayerController player, Vector3 point)
        {
            var pos = BoardPointToMap(point);
            var held = GameState.Held;
            var near = GameState.Nearest(pos, MonkeyRadius * 2f);

            if (held == null)
            {
                if (near == null) return;
                GameState.Placed.Remove(near);                        // reprendre
                GameState.Held = near.monkey;
            }
            else if (near != null && near.monkey.CanFuseWith(held.Value))
            {
                GameState.Placed.Remove(near);                        // fusion 2 → 1, au même endroit
                GameState.Placed.Add(new PlacedMonkey(near.monkey.Upgraded(), near.pos));
                GameState.Held = null;
            }
            else if (near != null)
            {
                GameState.Placed.Remove(near);                        // échange
                GameState.Placed.Add(new PlacedMonkey(held.Value, near.pos));
                GameState.Held = near.monkey;
            }
            else if (MapLayout.CanPlace(pos, MonkeyRadius))
            {
                GameState.Placed.Add(new PlacedMonkey(held.Value, pos)); // poser
                GameState.Held = null;
            }
            else return;

            GameState.NotifyChanged();
        }

        // ---------- Miroir de la carte ----------
        void LateUpdate()
        {
            if (!mapRoot) return;

            foreach (var m in Mirrored.All)
            {
                if (!proxies.TryGetValue(m, out var proxy))
                {
                    proxy = CreateProxy(m);
                    proxies.Add(m, proxy);
                }

                // On ne montre que ce qui est sur la carte (pas le joueur quand il est au hub)
                var local = mapRoot.InverseTransformPoint(m.transform.position);
                bool onMap = Mathf.Abs(local.x) <= MapLayout.HalfExtent + 1f && Mathf.Abs(local.z) <= MapLayout.HalfExtent + 1f;
                proxy.SetActive(onMap);
                if (!onMap) continue;

                proxy.transform.SetPositionAndRotation(MapToBoard(m.transform.position), transform.rotation * m.transform.rotation);
                proxy.transform.localScale = m.transform.lossyScale * scale;
                if (m.Tint) proxy.GetComponent<ColorTint>().Set(m.Tint.color, m.Tint.rainbow);
            }

            // Supprimer les copies des objets disparus (ballon éclaté, singe repris…)
            toRemove.Clear();
            foreach (var pair in proxies)
                if (!pair.Key) toRemove.Add(pair.Key);
            foreach (var m in toRemove)
            {
                Destroy(proxies[m]);
                proxies.Remove(m);
            }

            UpdateGhost();
        }

        GameObject CreateProxy(Mirrored m)
        {
            var proxy = new GameObject($"Miniature {m.name}");
            proxy.AddComponent<MeshFilter>().sharedMesh = m.GetComponent<MeshFilter>().sharedMesh;
            proxy.AddComponent<MeshRenderer>().sharedMaterial = m.GetComponent<MeshRenderer>().sharedMaterial;
            proxy.AddComponent<ColorTint>();
            if (!string.IsNullOrEmpty(m.label))
            {
                // Label posé au-dessus, à taille fixe (on compense l'échelle du proxy chaque frame)
                var label = Visuals.Label(proxy.transform, m.label, Vector3.up * 0.9f, 0.05f, Color.black);
                label.gameObject.AddComponent<KeepWorldScale>();
            }
            return proxy;
        }

        // Aperçu du singe tenu, là où on vise sur le plateau (vert = possible, rouge = impossible).
        void UpdateGhost()
        {
            var player = PlayerController.Local;
            var held = GameState.Held;
            bool show = player && held != null && ReferenceEquals(player.Target, this);

            if (!show || !held.Equals(ghostMonkey))
            {
                if (ghost) Destroy(ghost);
                ghost = null;
                ghostMonkey = null;
            }
            if (!show) return;

            if (!ghost)
            {
                ghostMonkey = held;
                ghost = Visuals.MonkeyPiece(held.Value, transform, Vector3.zero, TowerManager.TowerSize * scale);
                ghost.name = "Apercu";
            }

            var pos = BoardPointToMap(player.AimPoint);
            var near = GameState.Nearest(pos, MonkeyRadius * 2f);
            bool ok = near != null ? true : MapLayout.CanPlace(pos, MonkeyRadius);
            var target = near != null ? near.pos : pos;
            ghost.transform.localPosition = new Vector3(target.x, TowerManager.TowerSize, target.y) * scale;
            ghost.transform.Find("Corps").GetComponent<ColorTint>().Set(ok ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f));
        }
    }
}
