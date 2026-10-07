using UnityEngine;

namespace SAE
{
    // La cible de fléchettes accrochée au mur de la cabane (modèle Blender/cabane.py), rendue jouable (petit bonus caché) :
    // on lance une fléchette (Dart), elle se plante, et les points dépendent de la distance au centre.
    // Une manche = 3 fléchettes ; l'ardoise sous la cible affiche les points de la manche et le meilleur total.
    // +Z local = vers le mur : la face de la cible regarde vers -Z, son centre est à l'origine de l'objet.
    public class DartBoard : MonoBehaviour
    {
        public TextMesh label;

        // Rayons des zones, en mètres (la cible fait 24 cm de rayon) et leurs points, du centre vers le bord
        static readonly float[] ringRadius = { 0.025f, 0.06f, 0.13f, 0.24f };
        static readonly int[] ringPoints = { 50, 25, 10, 5 };
        const int DartsPerRound = 3;

        readonly int[] round = new int[DartsPerRound];
        int thrown;
        int best;

        void Start() => Show();

        // Une fléchette s'est plantée en 'point' : ses points, d'après sa distance au centre dans le plan de la cible
        public void Hit(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);
            float distance = new Vector2(local.x, local.y).magnitude;
            int points = 0;
            for (int i = 0; i < ringRadius.Length; i++)
                if (distance <= ringRadius[i]) { points = ringPoints[i]; break; }

            if (thrown == DartsPerRound) { System.Array.Clear(round, 0, round.Length); thrown = 0; }   // nouvelle manche
            round[thrown++] = points;
            int total = round[0] + round[1] + round[2];
            if (thrown == DartsPerRound && total > best) best = total;

            Show();
        }

        void Show()
        {
            if (!label) return;
            var sb = new System.Text.StringBuilder("<color=#FFD45A>FLÉCHETTES</color>\n");
            for (int i = 0; i < DartsPerRound; i++)
            {
                sb.Append(i < thrown ? round[i].ToString() : "–");
                if (i < DartsPerRound - 1) sb.Append("  +  ");
            }
            sb.Append($"\nmeilleur : {best}");
            label.text = sb.ToString();
        }
    }
}
