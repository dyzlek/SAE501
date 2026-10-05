using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un objet de la carte qui doit apparaître en miniature sur le plateau du hub
    // (ballons, singes, joueurs). À placer sur l'objet qui porte le mesh.
    public class Mirrored : MonoBehaviour
    {
        public static readonly List<Mirrored> All = new List<Mirrored>();

        public string label;

        public ColorTint Tint { get; private set; }

        void OnEnable()
        {
            Tint = GetComponent<ColorTint>();
            All.Add(this);
        }

        void OnDisable() => All.Remove(this);
    }
}
