using System;
using System.Collections;
using UnityEngine;

namespace SAE
{
    public enum HarvesterStat { Vitesse, Cadence, Rendement }

    // Le singe récolteur (un singe classique) : un service qu'on achète au panneau « RÉCOLTEUR ».
    // Il marche jusqu'à une banane posée sur la table, saute pour l'attraper, la porte au-dessus de sa tête,
    // marche jusqu'au panier et la jette dedans. Puis il souffle un peu et recommence.
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

        public bool Bought { get; private set; }
        public bool Walking { get; private set; }
        public Banane Carried { get; private set; }
        public float ThrowPhase { get; private set; }   // 0 = rien, monte à 1 pendant le geste du lancer
        public float Height { get; set; }               // taille du singe en mètres (posée par HarvesterSetup)

        readonly int[] levels = { 1, 1, 1 };

        public int Level(HarvesterStat s) => levels[(int)s];
        public bool IsMax(HarvesterStat s) => Level(s) >= MaxLevel;
        public int Price(HarvesterStat s) => Free ? 0 : Mathf.RoundToInt(100 * Mathf.Pow(1.6f, Level(s) - 1));
        public int PriceToBuy => Free ? 0 : BuyPrice;

        // Les trois améliorations, niveau 1 à 5
        public float Speed => 0.6f + 0.3f * (Level(HarvesterStat.Vitesse) - 1);        // m/s : 0,6 → 1,8
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
                yield return WalkTo(TableSpot(banana.transform.position), lost);
                if (lost()) continue;

                yield return JumpAndGrab(banana);
                yield return WalkTo(BasketSpot(), null);
                yield return Throw();
                yield return new WaitForSeconds(Pause);
            }
        }

        // La banane posée sur la table depuis le plus longtemps (la plus proche de pourrir), que personne ne tient
        Banane FindBanana()
        {
            Banane best = null;
            foreach (var b in bananier.BananesAuSol)
            {
                if (!b || !b.Posee || b.EnMain || b.Deposee || b.EstPourrie) continue;
                if (!best || b.Progression > best.Progression) best = b;
            }
            return best;
        }

        // Au bord de la table, juste sous la banane, du côté où le singe attend (il ne passe pas sous la table)
        Vector3 TableSpot(Vector3 bananaPos)
        {
            var local = table.InverseTransformPoint(bananaPos);
            float side = Mathf.Sign(table.InverseTransformPoint(home).z);
            var spot = table.TransformPoint(new Vector3(Mathf.Clamp(local.x, -0.7f, 0.7f), 0f, side * 0.7f));
            spot.y = home.y;
            return spot;
        }

        // Devant le socle du panier, du côté où le singe attend
        Vector3 BasketSpot()
        {
            var basket = panier.transform.position;
            var dir = home - basket;
            dir.y = 0f;
            var spot = basket + dir.normalized * 0.55f;
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
            // Le joueur ne peut plus la lui prendre des mains
            foreach (var c in banana.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        // Le geste : les bras partent en arrière puis vers l'avant ; la banane part en cloche jusqu'au panier.
        IEnumerator Throw()
        {
            var look = panier.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);

            for (ThrowPhase = 0f; ThrowPhase < 1f; ThrowPhase += Time.deltaTime / 0.35f) yield return null;

            var banana = Carried;
            Carried = null;
            if (banana)
            {
                var from = banana.transform.position;
                var to = panier.transform.position + Vector3.up * 0.05f;
                for (float t = 0f; t < 1f; t += Time.deltaTime / throwDuration)
                {
                    if (!banana) break;
                    banana.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.4f);
                    banana.transform.Rotate(0f, 0f, 720f * Time.deltaTime);   // elle tourne en l'air
                    yield return null;
                }
                if (banana) panier.RecevoirPart(banana, Share);
            }
            ThrowPhase = 0f;
        }
    }
}
