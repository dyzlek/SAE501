using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le plateau du hub = la carte en miniature, en direct.
    // Il affiche une copie réduite de tout ce qui porte Mirrored (ballons, singes, joueurs).
    // La pose des singes est gérée par PlacementSurface, sur le même objet.
    public class Board : MonoBehaviour
    {
        public Transform mapRoot;          // le centre de la vraie carte
        public float scale = 0.05f;        // taille du plateau / taille de la carte

        readonly Dictionary<Mirrored, GameObject> proxies = new Dictionary<Mirrored, GameObject>();
        readonly List<Mirrored> toRemove = new List<Mirrored>();

        Vector3 MapToBoard(Vector3 worldOnMap) =>
            transform.TransformPoint(mapRoot.InverseTransformPoint(worldOnMap) * scale);

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
        }

        GameObject CreateProxy(Mirrored m)
        {
            var proxy = new GameObject($"Miniature {m.name}");
            // Un singe avec un modèle 3D : sa miniature est le même singe (modèle + aura), à l'échelle 1 du proxy
            // (le proxy prend la taille du cube du singe). Sinon, on copie simplement le mesh (ballons, joueur).
            var view = m.GetComponentInParent<MonkeyView>();
            if (view && view.model)
                Visuals.MonkeyPiece(view.monkey, proxy.transform, Vector3.zero, 1f, withLabel: false);
            else
            {
                proxy.AddComponent<MeshFilter>().sharedMesh = m.GetComponent<MeshFilter>().sharedMesh;
                proxy.AddComponent<MeshRenderer>().sharedMaterial = m.GetComponent<MeshRenderer>().sharedMaterial;
            }
            proxy.AddComponent<ColorTint>();
            if (!view && !string.IsNullOrEmpty(m.label))   // pas de texte sur les singes posés : le modèle suffit
            {
                // Label posé au-dessus, à taille fixe (on compense l'échelle du proxy chaque frame)
                var label = Visuals.Label(proxy.transform, m.label, Vector3.up * 0.9f, 0.04f, Color.black);
                label.gameObject.AddComponent<KeepWorldScale>();
            }
            return proxy;
        }
    }
}
