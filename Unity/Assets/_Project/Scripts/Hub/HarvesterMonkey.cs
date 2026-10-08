using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAE
{
    // Un singe récolteur (un singe classique) : on l'achète au comptoir « RÉCOLTEUR » (voir HarvesterCrew, qui garde
    // le nombre de singes et leurs améliorations, communes à toute l'équipe).
    // Il marche jusqu'à une banane (sur la table, ou n'importe où si le joueur l'a lâchée ailleurs), saute pour l'attraper, la porte devant lui,
    // marche jusqu'au panier et la jette dedans. Puis il souffle un peu et recommence.
    // Pour rire, le lancer n'est pas toujours le même : parfois il DUNK (saute au-dessus du panier, l'y écrase et fête ça),
    // parfois il RATE (la banane tombe à côté, il boude, la ramasse et recommence).
    // Il garde une commission : seule une part de la banane (Rendement) est payée.
    // Les animations (marche, porter, lancer) sont faites par HarvesterAnimator, qui lit les états publics d'ici.
    // Les meubles sont contre les murs : pour ne pas les traverser, il passe par le milieu de la pièce (voir WalkTo).
    public class HarvesterMonkey : MonoBehaviour
    {
        const float FreeRadius = 1.4f;          // en mètres : autour du centre de la terrasse, il n'y a aucun meuble
        const float ThrowDistance = 0.6f;       // en mètres, du centre du panier : devant son tabouret, pas dedans
        const float DunkDistance = 0.45f;
        const float ShortWalk = 1f;             // en mètres : en dessous, pas de crochet par le centre

        public HarvesterCrew crew;
        public Bananier bananier;
        public Panier panier;
        public Transform table;                 // la table des bananes (Bananier.versCible)
        public Vector3 home;                    // où il attend quand il n'y a rien à ramasser (au sol)
        public Vector3 areaCenter;              // le milieu de la terrasse de la bananeraie (en coordonnées du monde)
        public float areaRadius = 3f;           // la terrasse, vue comme un disque : on y repose le singe lancé (HarvesterGrab)

        public float jumpDuration = 0.5f;       // en secondes, le saut pour attraper la banane
        public float throwDuration = 0.45f;     // en secondes, le vol de la banane jusqu'au panier
        public float turnSpeed = 540f;          // en degrés par seconde
        [Range(0f, 1f)] public float dunkChance = 0.2f;   // chance de dunker au lieu de lancer
        [Range(0f, 1f)] public float missChance = 0.25f;  // chance de rater un lancer normal

        public bool Walking { get; private set; }
        public Banane Carried { get; private set; }
        public float ThrowPhase { get; private set; }   // 0 = rien, monte à 1 pendant le geste du lancer
        public float height = 0.55f;                    // taille du singe, en mètres
        public bool Cheering { get; private set; }      // bras levés : il fête son dunk
        public bool Sulking { get; private set; }       // il secoue la tête : il a raté (ou il est sonné après un lancer)
        public bool Held { get; private set; }          // le joueur l'a pris dans sa main (voir HarvesterGrab) : il gigote

        Banane claimed;                                 // la banane qu'il est parti chercher (les autres singes la laissent)

        // Appelé par l'équipe quand on achète ce singe : il apparaît et se met au travail
        public void StartWork()
        {
            gameObject.SetActive(true);
            StartCoroutine(Work());
        }

        // Le joueur le prend dans sa main : il arrête tout, lâche sa banane et libère celle qu'il visait
        public void PickedUp()
        {
            StopAllCoroutines();
            Held = true;
            Walking = Cheering = Sulking = false;
            ThrowPhase = 0f;
            if (claimed) crew.Unclaim(claimed);
            claimed = null;
            if (Carried)
            {
                foreach (var c in Carried.GetComponentsInChildren<Collider>()) c.enabled = true;
                Carried.Lachee();   // elle retombe, le joueur ou un autre singe pourra la reprendre
                Carried = null;
            }
        }

        // Il a atterri après un lancer : il est sonné un instant (il secoue la tête), puis il retourne au travail
        public void PutDown()
        {
            Held = false;
            StartCoroutine(BackToWork());
        }

        IEnumerator BackToWork()
        {
            yield return Sulk();
            yield return Work();
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

                crew.Claim(banana);   // les autres singes la laissent : c'est la sienne
                claimed = banana;
                yield return Fetch(banana);
                crew.Unclaim(banana);
                claimed = null;
                yield return new WaitForSeconds(crew.Pause);
            }
        }

        IEnumerator Fetch(Banane banana)
        {
            // Le joueur peut la prendre avant nous : on abandonne et on en cherche une autre
            Func<bool> lost = () => !banana || banana.EnMain || banana.Deposee;
            yield return WalkTo(PickSpot(banana.transform.position), lost);
            if (lost()) yield break;

            yield return JumpAndGrab(banana);
            yield return Deliver();
        }

        // La banane posée sur la table depuis le plus longtemps (la plus proche de pourrir), que personne ne tient
        Banane FindBanana()
        {
            Banane best = null;
            foreach (var b in bananier.BananesAuSol)
            {
                if (!b || b.EnMain || b.Deposee || b.EstPourrie || crew.IsClaimed(b) || !AtRest(b)) continue;
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
            bool onTable = Mathf.Abs(local.x) < 0.75f && Mathf.Abs(local.z) < 0.55f;   // l'étal fait 1,4 × 1 m
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
            var spot = table.TransformPoint(new Vector3(Mathf.Clamp(local.x, -0.6f, 0.6f), 0f, side * 0.7f));
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

        // Marcher jusqu'à target. Si le trajet est long et que la ligne droite longe les murs (là où sont les meubles), il fait un crochet
        // par le milieu de la terrasse : on prend le point de la ligne le plus proche du centre, ramené à FreeRadius.
        IEnumerator WalkTo(Vector3 target, Func<bool> abort)
        {
            var from = transform.position;
            var center = new Vector2(areaCenter.x, areaCenter.z);
            var closest = ClosestToCenter(new Vector2(from.x, from.z) - center, new Vector2(target.x, target.z) - center);
            bool longWalk = Vector3.Distance(from, target) > ShortWalk;   // un petit pas à côté d'un meuble : tout droit
            if (longWalk && closest.magnitude > FreeRadius)
            {
                var detour = center + closest.normalized * FreeRadius;
                yield return WalkStraight(new Vector3(detour.x, target.y, detour.y), abort);
            }
            yield return WalkStraight(target, abort);
        }

        // Le point du segment [a, b] le plus proche de l'origine (a et b sont pris depuis le milieu de la terrasse, vus de dessus)
        static Vector2 ClosestToCenter(Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(-a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }

        IEnumerator WalkStraight(Vector3 target, Func<bool> abort)
        {
            Walking = true;
            while (true)
            {
                if (abort != null && abort()) break;
                var to = target - transform.position;
                to.y = 0f;
                if (to.magnitude < 0.05f) break;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, target, crew.Speed * Time.deltaTime);
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
            float jumpHeight = Mathf.Max(0.1f, banana.transform.position.y - (home.y + height * 1.1f));
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
                    yield return WalkTo(BasketSpot(DunkDistance), null);
                    yield return Dunk();
                    yield break;
                }
                yield return WalkTo(BasketSpot(ThrowDistance), null);
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
                if (banana && !miss) panier.RecevoirPart(banana, crew.Share);
            }
            ThrowPhase = 0f;
        }

        // Le dunk : grand saut au-dessus du panier, il y écrase la banane, retombe et fait un tour sur lui-même, bras levés.
        IEnumerator Dunk()
        {
            FaceBasket();
            float jumpHeight = Mathf.Max(0.3f, panier.transform.position.y + 0.25f - (home.y + height * 1.1f));
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
            if (banana) panier.RecevoirPart(banana, crew.Share);
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
