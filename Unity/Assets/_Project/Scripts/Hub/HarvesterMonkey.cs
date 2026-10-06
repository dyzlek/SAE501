using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAE
{
    public enum HarvesterStat { Vitesse, Cadence, Rendement }

    // Le singe récolteur (un singe classique) : un service qu'on achète au panneau « RÉCOLTEUR ».
    // Il marche jusqu'à une banane (sur la table, ou n'importe où si le joueur l'a lâchée ailleurs), saute pour l'attraper, la porte au-dessus de sa tête,
    // marche jusqu'au panier et la jette dedans. Puis il souffle un peu et recommence.
    // Pour rire, le lancer n'est pas toujours le même : parfois il DUNK (saute au-dessus du panier, l'y écrase et fête ça),
    // parfois il RATE (la banane tombe à côté, il boude, la ramasse et recommence).
    // Il garde une commission : seule une part de la banane (Rendement) est payée.
    // Les animations (marche, porter, lancer) sont faites par HarvesterAnimator, qui lit les états publics d'ici.
    // Pour l'instant tout est gratuit (Free) : les prix sont calculés mais pas demandés.
    public class HarvesterMonkey : MonoBehaviour
    {
        public const bool Free = true;          // à passer à false quand on voudra faire payer
        public const int BuyPrice = 200;        // prix du service (en bananes), si Free = false
        public const int MaxLevel = 5;

        public Bananier bananier;
        public Panier panier;
        public Transform table;                 // la table des bananes (Bananier.versCible)
        public Vector3 home;                    // où il attend quand il n'y a rien à ramasser (au sol)

        public float jumpDuration = 0.5f;       // en secondes, le saut pour attraper la banane
        public float throwDuration = 0.45f;     // en secondes, le vol de la banane jusqu'au panier
        public float turnSpeed = 540f;          // en degrés par seconde
        [Range(0f, 1f)] public float dunkChance = 0.2f;   // chance de dunker au lieu de lancer
        [Range(0f, 1f)] public float missChance = 0.25f;  // chance de rater un lancer normal

        public bool Bought { get; private set; }
        public bool Walking { get; private set; }
        public Banane Carried { get; private set; }
        public float ThrowPhase { get; private set; }   // 0 = rien, monte à 1 pendant le geste du lancer
        public float Height { get; set; }               // taille du singe en mètres (posée par HarvesterSetup)
        public bool Cheering { get; private set; }      // bras levés : il fête son dunk
        public bool Sulking { get; private set; }       // il secoue la tête : il a raté

        readonly int[] levels = { 1, 1, 1 };

        public int Level(HarvesterStat s) => levels[(int)s];
        public bool IsMax(HarvesterStat s) => Level(s) >= MaxLevel;
        public int Price(HarvesterStat s) => Free ? 0 : Mathf.RoundToInt(100 * Mathf.Pow(1.6f, Level(s) - 1));
        public int PriceToBuy => Free ? 0 : BuyPrice;

        // Les trois améliorations, niveau 1 à 5
        public float Speed => 0.25f + 0.2f * (Level(HarvesterStat.Vitesse) - 1);       // m/s : 0,25 (il flâne) → 1,05
        public float Pause => 3f - 0.6f * (Level(HarvesterStat.Cadence) - 1);           // s entre deux trajets : 3 → 0,6
        public float Share => 0.6f + 0.1f * (Level(HarvesterStat.Rendement) - 1);       // part payée : 60 % → 100 %

        public bool Buy()
        {
            if (Bought || !Economy.TrySpend(PriceToBuy, transform.position)) return false;
            Bought = true;
            gameObject.SetActive(true);
            StartCoroutine(Work());
            return true;
        }

        public bool Upgrade(HarvesterStat s)
        {
            if (!Bought || IsMax(s) || !Economy.TrySpend(Price(s), transform.position)) return false;
            levels[(int)s]++;
            return true;
        }

        // La boucle de travail : chercher, aller, attraper, porter, lancer, souffler.
        IEnumerator Work()
        {
            while (true)
            {
                var banana = FindBanana();
                if (!banana)
                {
                    yield return WalkTo(home, null);
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Le joueur peut la prendre avant nous : on abandonne et on en cherche une autre
                Func<bool> lost = () => !banana || banana.EnMain || banana.Deposee;
                yield return WalkTo(PickSpot(banana.transform.position), lost);
                if (lost()) continue;

                yield return JumpAndGrab(banana);
                yield return Deliver();
                yield return new WaitForSeconds(Pause);
            }
        }

        // La banane posée sur la table depuis le plus longtemps (la plus proche de pourrir), que personne ne tient
        Banane FindBanana()
        {
            Banane best = null;
            foreach (var b in bananier.BananesAuSol)
            {
                if (!b || b.EnMain || b.Deposee || b.EstPourrie || !AtRest(b)) continue;
                if (!best || b.Progression > best.Progression) best = b;
            }
            return best;
        }

        // Immobile : finie sa chute depuis l'arbre, ou retombée (par terre, sur un meuble…) après un lâcher du joueur
        static bool AtRest(Banane b)
        {
            var body = b.GetComponent<Rigidbody>();
            return body.isKinematic ? b.Posee : body.linearVelocity.sqrMagnitude < 0.01f;
        }

        // Où se mettre pour la prendre : au bord de la table si elle est dessus, sinon juste devant elle
        Vector3 PickSpot(Vector3 bananaPos)
        {
            var local = table.InverseTransformPoint(bananaPos);
            bool onTable = Mathf.Abs(local.x) < 0.85f && Mathf.Abs(local.z) < 0.55f;
            if (!onTable)
            {
                var toMonkey = transform.position - bananaPos;
                toMonkey.y = 0f;
                var spot = bananaPos + toMonkey.normalized * 0.2f;
                spot.y = home.y;
                return spot;
            }
            return TableSpot(local);
        }

        // Au bord de la table, juste sous la banane, du côté où le singe attend (il ne passe pas sous la table)
        Vector3 TableSpot(Vector3 local)
        {
            float side = Mathf.Sign(table.InverseTransformPoint(home).z);
            var spot = table.TransformPoint(new Vector3(Mathf.Clamp(local.x, -0.7f, 0.7f), 0f, side * 0.7f));
            spot.y = home.y;
            return spot;
        }

        // Devant le socle du panier, à « distance » de son centre, du côté où le singe attend
        Vector3 BasketSpot(float distance)
        {
            var basket = panier.transform.position;
            var dir = home - basket;
            dir.y = 0f;
            var spot = basket + dir.normalized * distance;
            spot.y = home.y;
            return spot;
        }

        IEnumerator WalkTo(Vector3 target, Func<bool> abort)
        {
            Walking = true;
            while (true)
            {
                if (abort != null && abort()) break;
                var to = target - transform.position;
                to.y = 0f;
                if (to.magnitude < 0.05f) break;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
                yield return null;
            }
            Walking = false;
        }

        // Il se tourne vers la banane, saute, l'attrape en haut du saut et retombe avec.
        IEnumerator JumpAndGrab(Banane banana)
        {
            var look = banana.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);

            // Assez haut pour que ses mains levées atteignent la banane
            float jumpHeight = Mathf.Max(0.1f, banana.transform.position.y - (home.y + Height * 1.1f));
            var ground = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / jumpDuration)
            {
                transform.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * jumpHeight);
                if (!Carried && t >= 0.5f) Grab(banana);
                yield return null;
            }
            transform.position = ground;
            if (!Carried) Grab(banana);
        }

        void Grab(Banane banana)
        {
            Carried = banana;
            banana.Prise();   // elle ne pourrit plus en disparaissant, le panier ne la compte pas toute seule
            var body = banana.GetComponent<Rigidbody>();
            body.isKinematic = true;   // lâchée par le joueur, elle tombait : dans ses mains, plus de gravité
            // Le joueur ne peut plus la lui prendre des mains
            foreach (var c in banana.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        // Porter la banane jusqu'au panier et l'y mettre : dunk, lancer réussi, ou lancer raté (et on recommence).
        IEnumerator Deliver()
        {
            while (Carried)
            {
                if (Random.value < dunkChance)
                {
                    yield return WalkTo(BasketSpot(0.3f), null);
                    yield return Dunk();
                    yield break;
                }
                yield return WalkTo(BasketSpot(0.55f), null);
                bool miss = Random.value < missChance;
                yield return Throw(miss);
                if (!miss) yield break;

                // Raté : il boude, puis va la ramasser par terre, et retente
                var banana = lastThrown;
                yield return Sulk();
                if (!banana) yield break;
                yield return WalkTo(banana.transform.position - (banana.transform.position - transform.position).normalized * 0.25f, null);
                Carried = banana;
            }
        }

        Banane lastThrown;

        // Le geste : les bras partent en arrière puis vers l'avant ; la banane part en cloche
        // jusqu'au panier, ou à côté si c'est raté (elle reste alors par terre).
        IEnumerator Throw(bool miss)
        {
            FaceBasket();
            for (ThrowPhase = 0f; ThrowPhase < 1f; ThrowPhase += Time.deltaTime / 0.35f) yield return null;

            var banana = Carried;
            Carried = null;
            lastThrown = banana;
            if (banana)
            {
                var to = panier.transform.position + Vector3.up * 0.05f;
                if (miss)
                {
                    // À côté du panier, par terre : trop court ou trop à droite / à gauche
                    var aside = Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * (transform.position - panier.transform.position);
                    to = panier.transform.position + aside.normalized * 0.45f;
                    to.y = home.y + 0.05f;
                }
                yield return Fly(banana, to, throwDuration);
                if (banana && !miss) panier.RecevoirPart(banana, Share);
            }
            ThrowPhase = 0f;
        }

        // Le dunk : grand saut au-dessus du panier, il y écrase la banane, retombe et fait un tour sur lui-même, bras levés.
        IEnumerator Dunk()
        {
            FaceBasket();
            float jumpHeight = Mathf.Max(0.3f, panier.transform.position.y + 0.25f - (home.y + Height * 1.1f));
            var ground = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                transform.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * jumpHeight);
                ThrowPhase = Mathf.Clamp01((t - 0.2f) / 0.5f);   // les bras s'abattent au sommet
                if (Carried && t >= 0.55f)
                {
                    var banana = Carried;
                    Carried = null;
                    StartCoroutine(SlamInto(banana));
                }
                yield return null;
            }
            transform.position = ground;
            ThrowPhase = 0f;

            Cheering = true;
            var start = transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f)
            {
                transform.rotation = start * Quaternion.Euler(0f, 360f * t, 0f);
                yield return null;
            }
            transform.rotation = start;
            Cheering = false;
        }

        IEnumerator SlamInto(Banane banana)
        {
            yield return Fly(banana, panier.transform.position, 0.12f, 0f);
            if (banana) panier.RecevoirPart(banana, Share);
        }

        // Il boude : il secoue la tête et tape deux fois du pied (deux petits sauts)
        IEnumerator Sulk()
        {
            Sulking = true;
            var ground = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
            {
                transform.position = ground + Vector3.up * (Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 0.08f);
                yield return null;
            }
            transform.position = ground;
            Sulking = false;
        }

        // La banane vole en cloche jusqu'à « to » en tournant sur elle-même
        IEnumerator Fly(Banane banana, Vector3 to, float duration, float arc = 0.4f)
        {
            var from = banana.transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (!banana) yield break;
                banana.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc);
                banana.transform.Rotate(0f, 0f, 720f * Time.deltaTime);
                yield return null;
            }
            if (banana) banana.transform.position = to;
        }

        void FaceBasket()
        {
            var look = panier.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);
        }
    }
}
