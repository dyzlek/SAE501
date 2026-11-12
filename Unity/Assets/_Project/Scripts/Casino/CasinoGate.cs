using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le casino de la bananeraie (la roulette ET la borne de blackjack) est fermé au début : des barrières rouges et blanches
    // entourent chaque jeu, avec une pancarte « CASINO FERMÉ · ouvre à la vague 5 (2 / 5) ». Tant qu'il est fermé, les boutons,
    // le tapis et la roue refusent (Tutorial.TryPress, WheelGrab) : son d'alerte et pancartes qui rougissent.
    // Pourquoi : au début, le joueur doit apprendre la boucle (arc, singes, bananes) sans parier ses premières bananes ;
    // le casino devient une récompense, et le tutoriel ne s'encombre plus de lui.
    // À la vague UnlockWave, les barrières s'enfoncent dans le sol, et le tutoriel amène le joueur à la roulette
    // (Tutorial.CasinoOpened). Si la partie chargée a déjà passé cette vague, il n'y a pas de barrières du tout.
    // Construit au lancement par la roulette (RouletteTable.Start) : pas besoin de régénérer la scène.
    public class CasinoGate : MonoBehaviour
    {
        public const int UnlockWave = 5;

        const float Margin = 0.45f;        // en mètres : l'écart entre le jeu et la barrière
        const float FenceHeight = 1f;      // en mètres
        const float SinkTime = 1.2f;       // en secondes : les barrières s'enfoncent dans le sol
        const float FlashTime = 0.6f;      // en secondes : la pancarte rougit quand on touche un jeu fermé
        const float SignWidth = 1.2f, SignHeight = 0.55f;   // en mètres
        const float TextHeight = 0.075f;   // en mètres, une ligne : le texte tient dans la pancarte

        static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color Red = new Color(0.85f, 0.15f, 0.12f);
        static readonly Color White = new Color(0.95f, 0.95f, 0.92f);
        static readonly Color Slate = new Color(0.12f, 0.13f, 0.15f);
        static readonly Color SignRed = new Color(0.6f, 0.08f, 0.06f);

        static readonly List<CasinoGate> all = new List<CasinoGate>();   // un enclos par jeu (roulette, blackjack)
        static float refusedAt = -10f;
        static bool announced;             // l'ouverture n'est annoncée qu'une fois, même avec deux enclos

        public static bool Open => GameState.Sandbox || GameState.WavesWon >= UnlockWave;

        // Ce bouton est-il un bouton d'un jeu du casino, et le casino est-il encore fermé ?
        public static bool Blocks(IPressable p) =>
            !Open && p is Component c && c && (c.GetComponentInParent<RouletteTable>() || c.GetComponentInParent<BlackjackTable>());

        // On a touché un jeu fermé : son d'alerte, et les pancartes clignotent
        public static void Refuse()
        {
            Sounds.Play(Sounds.AchatRefuse);
            refusedAt = Time.time;
        }

        TextMesh label;
        ColorTint sign;
        int shownWaves = -1;
        bool opening;

        // Appelé par la roulette au lancement : un enclos autour de ce jeu, seulement si le casino est encore fermé
        public static void CreateAround(Transform game)
        {
            if (Open || !game) return;
            if (all.Count == 0) announced = false;
            var root = new GameObject("Barrières du casino : " + game.name);
            root.transform.SetParent(game.parent, false);
            root.transform.SetPositionAndRotation(game.position, game.rotation);   // même repère que le jeu
            var gate = root.AddComponent<CasinoGate>();
            all.Add(gate);
            gate.Build(game);
        }

        void OnDestroy() => all.Remove(this);

        // Trois côtés de barrière autour du jeu (devant et sur les côtés) ; le quatrième est la rambarde de la terrasse
        void Build(Transform game)
        {
            // La place du jeu dans son propre repère (min et max de tout ce qu'on voit de lui)
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var r in game.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is LineRenderer) continue;   // les auras et lueurs débordent
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var local = game.InverseTransformPoint(corner);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }
            if (float.IsInfinity(min.x)) { min = new Vector3(-1f, 0f, -0.7f); max = new Vector3(1f, 1f, 0.7f); }

            // Le devant : le côté tourné vers le milieu de la bananeraie (là d'où arrive le joueur)
            var orchard = BananaOrchard.Instance;
            float side = orchard ? Mathf.Sign(game.InverseTransformPoint(orchard.transform.position).z) : -1f;
            if (side == 0f) side = -1f;
            float front = side < 0f ? min.z - Margin : max.z + Margin;
            float back = side < 0f ? max.z + 0.1f : min.z - 0.1f;
            float left = min.x - Margin, right = max.x + Margin;

            Fence(new Vector3(left, 0f, front), new Vector3(right, 0f, front));    // devant
            Fence(new Vector3(left, 0f, front), new Vector3(left, 0f, back));      // côté gauche
            Fence(new Vector3(right, 0f, front), new Vector3(right, 0f, back));    // côté droit

            // La pancarte, au milieu du devant, tournée vers le joueur. Le texte est collé devant l'ardoise et ne tourne pas
            // vers la caméra (sans Billboard) : il ne passe plus derrière l'ardoise quand on la regarde de côté.
            float x = (left + right) / 2f;
            var board = Visuals.Box("Pancarte", transform, new Vector3(x, 1.35f, front), new Vector3(SignWidth, SignHeight, 0.04f), Slate);
            sign = board.GetComponent<ColorTint>();
            // Visuals.Text = un texte fixe (sans le Billboard SAE qui le tournait vers la caméra : avant, on retirait celui
            // des coffres, qui n'y était pas, et le texte pivotait à travers l'ardoise, coupé à moitié, 12 nov.)
            label = Visuals.Text(transform, "", new Vector3(x, 1.35f, front + side * 0.03f), TextHeight, White, title: true);
            label.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, 0f, -side));   // lisible depuis le devant
            Refresh();
        }

        // Une barrière de chantier de 'a' à 'b' (au sol) : poteaux en bois, lisse rouge en haut, lisse blanche au milieu.
        // Elle a des colliders : on ne passe pas à travers.
        void Fence(Vector3 a, Vector3 b)
        {
            var along = b - a;
            float length = along.magnitude;
            if (length < 0.01f) return;
            var middle = (a + b) / 2f;
            var rotation = Quaternion.LookRotation(along.normalized);
            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 1.2f) + 1);
            for (int i = 0; i < posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (posts - 1f));
                Visuals.Solid("Poteau", transform, p + Vector3.up * (FenceHeight / 2f), new Vector3(0.08f, FenceHeight, 0.08f), Wood);
            }
            var top = Visuals.Solid("Lisse rouge", transform, middle + Vector3.up * (FenceHeight * 0.85f), new Vector3(0.05f, 0.12f, length), Red);
            top.transform.localRotation = rotation;
            var low = Visuals.Solid("Lisse blanche", transform, middle + Vector3.up * (FenceHeight * 0.5f), new Vector3(0.05f, 0.12f, length), White);
            low.transform.localRotation = rotation;
        }

        void Refresh()
        {
            shownWaves = GameState.WavesWon;
            label.text = $"CASINO FERMÉ\nouvre à la vague {UnlockWave}\n({Mathf.Min(shownWaves, UnlockWave)} / {UnlockWave})";
        }

        void Update()
        {
            if (opening) return;
            if (Open) { StartCoroutine(OpenUp()); return; }
            if (GameState.WavesWon != shownWaves) Refresh();
            float r = (Time.time - refusedAt) / FlashTime;
            sign.Animate(r < 1f ? Color.Lerp(SignRed, Slate, r) : Slate);
        }

        // Le casino ouvre : les pancartes l'annoncent, les barrières s'enfoncent dans le sol, puis le tutoriel y amène le joueur
        IEnumerator OpenUp()
        {
            opening = true;
            label.text = "CASINO OUVERT !";
            if (!announced)
            {
                announced = true;
                Sounds.Play(Sounds.SingeObtenu, label.transform.position);
                Tutorial.CasinoOpened();
            }
            yield return new WaitForSeconds(0.8f);
            var start = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / SinkTime)
            {
                transform.position = start + Vector3.down * (FenceHeight * 1.6f * t * t);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
