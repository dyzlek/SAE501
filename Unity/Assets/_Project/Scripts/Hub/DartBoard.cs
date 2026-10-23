using UnityEngine;

namespace SAE
{
    // Une cible de fléchettes : on lance une fléchette (Dart), elle se plante, et les points dépendent de la distance au centre.
    // Une manche = 3 fléchettes ; l'ardoise sous la cible affiche les points de la manche et le meilleur total.
    //   - Dans la cabane (modèle Blender/cabane.py), elle est gratuite : un petit bonus caché, sans enjeu.
    //   - Au casino (DartStand), elle est À GAINS (paid) : on paie la manche (JOUER, la mise choisie), puis le total
    //     des 3 fléchettes rapporte : 100 points et plus = mise × 5, 60 = × 3, 35 = × 2, 20 = mise rendue.
    // +Z local = vers le mur : la face de la cible regarde vers -Z, son centre est à l'origine de l'objet.
    public class DartBoard : MonoBehaviour
    {
        public TextMesh label;
        public bool paid;   // au casino : chaque manche se paie et rapporte selon les points

        // Rayons des zones, en mètres (la cible fait 24 cm de rayon) et leurs points, du centre vers le bord
        static readonly float[] ringRadius = { 0.025f, 0.06f, 0.13f, 0.24f };
        static readonly int[] ringPoints = { 50, 25, 10, 5 };
        const int DartsPerRound = 3;

        // Le barème des gains : à partir de ces points (total des 3 fléchettes), la mise est multipliée par…
        static readonly int[] prizePoints = { 100, 60, 35, 20 };
        static readonly int[] prizeFactor = { 5, 3, 2, 1 };

        readonly int[] round = new int[DartsPerRound];
        int thrown;
        int best;
        int stake;           // à gains : la mise de la manche en cours (0 = pas de manche payée)
        string result = "";

        public bool Playing => stake > 0;

        void Start() => Show();

        // À gains : JOUER paie la manche ; les 3 fléchettes suivantes comptent
        public bool StartPaidRound(int bet)
        {
            if (!paid || Playing || bet <= 0 || !Economy.TrySpend(bet, transform.position)) return false;
            stake = bet;
            thrown = 0;
            System.Array.Clear(round, 0, round.Length);
            result = "";
            Show();
            return true;
        }

        // Une fléchette s'est plantée en 'point' : ses points, d'après sa distance au centre dans le plan de la cible
        public void Hit(Vector3 point)
        {
            if (paid && !Playing)   // pas de manche payée : la fléchette ne compte pas
            {
                result = "Appuie d'abord sur JOUER";
                Show();
                return;
            }
            var local = transform.InverseTransformPoint(point);
            float distance = new Vector2(local.x, local.y).magnitude;
            int points = 0;
            for (int i = 0; i < ringRadius.Length; i++)
                if (distance <= ringRadius[i]) { points = ringPoints[i]; break; }

            if (thrown == DartsPerRound) { System.Array.Clear(round, 0, round.Length); thrown = 0; }   // nouvelle manche
            round[thrown++] = points;
            if (points == ringPoints[0]) Sounds.Play(Sounds.FlecheCentre, point);   // en plein dans le mille : le petit bonus caché
            int total = round[0] + round[1] + round[2];
            if (thrown == DartsPerRound)
            {
                if (total > best) best = total;
                if (paid) Pay(total);
            }
            Show();
        }

        // Fin d'une manche à gains : le barème, puis on peut rejouer
        void Pay(int total)
        {
            int factor = 0;
            for (int i = 0; i < prizePoints.Length; i++)
                if (total >= prizePoints[i]) { factor = prizeFactor[i]; break; }
            int gain = stake * factor;
            if (gain > 0)
            {
                Economy.Earn(gain, transform.position + Vector3.back * 0.3f, stakeBack: Mathf.Min(gain, stake));
                Sounds.Play(factor >= 3 ? Sounds.VagueGagnee : Sounds.SingeObtenu, transform.position);
            }
            else Sounds.Play(Sounds.ViePerdue, transform.position, 0.8f, 0.9f);
            result = gain > stake ? $"<color=#8CE08C>{total} points : +{gain} !</color>"
                   : gain == stake ? $"{total} points : mise rendue" : $"<color=#FF7A6A>{total} points : perdu</color>";
            stake = 0;
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
            if (!paid)
            {
                sb.Append($"\nmeilleur : {best}");
                label.text = sb.ToString();
                return;
            }
            sb.Append(Playing ? $"   (mise {stake})\n" : "\n");
            sb.Append(result.Length > 0 ? result : Playing ? "Lance tes 3 fléchettes !" : "Choisis ta mise, puis JOUER");
            sb.Append("\n<size=34>100 pts : ×5 · 60 : ×3 · 35 : ×2 · 20 : mise rendue</size>");
            label.text = sb.ToString();
        }
    }
}
