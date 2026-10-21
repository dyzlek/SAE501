using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAE
{
    // Un singe récolteur (un singe classique) : on l'achète au comptoir « RÉCOLTEUR » (voir HarvesterCrew, qui garde
    // le nombre de singes et leurs améliorations, communes à toute l'équipe).
    // Il va à l'arbre qui a le plus de bananes en attente (2 singes au plus par arbre), se met au bord de son étal,
    // saute pour attraper la banane, en prend une 2e si une autre attend sur le même étal, porte le tout jusqu'au panier
    // et le jette dedans. Puis il souffle un peu et recommence. Une banane dorée passe avant toutes les autres.
    // Pour rire, le lancer n'est pas toujours le même : parfois il DUNK (saute au-dessus du panier, l'y écrase et fête ça),
    // parfois il RATE (la banane tombe à côté, il boude, la ramasse et recommence).
    // Il garde une commission : seule une part de la banane (Rendement) est payée.
    // Les animations (marche, porter, lancer) sont faites par HarvesterAnimator, qui lit les états publics d'ici.
    // Pour ne pas se gêner (avant, ils passaient tous par le milieu de la terrasse et s'y tassaient) :
    //   - devant chaque étal, une ZONE invisible (côté terrasse) : on y attend quand il n'y a rien à faire, on y passe en
    //     allant à l'étal ou en revenant ; les singes sans travail se répartissent entre les arbres ;
    //   - au bord de l'étal, deux singes d'un même arbre se tiennent chacun de leur côté ;
    //   - autour du panier, 4 places en arc : chacun prend une place libre (HarvesterCrew).
    // Entre les zones des étals et les places du panier, la terrasse est libre : on y marche en ligne droite.
    public class HarvesterMonkey : MonoBehaviour
    {
        const float ThrowDistance = 0.6f;       // en mètres, du centre du panier : devant son tabouret, pas dedans
        const float DunkDistance = 0.45f;
        const float ZoneDistance = 1.15f;       // en mètres : la zone devant l'étal, depuis son milieu, côté terrasse
        const float StallEdge = 0.7f;           // en mètres : le bord de l'étal (1,4 × 1 m), côté terrasse, où l'on se tient
        const float StallHalfWidth = 0.6f;      // en mètres : on se tient au plus à 0,6 m du milieu de l'étal (pas au-delà des pieds)
        const float Elbow = 0.4f;               // en mètres : deux singes au même étal gardent cet écart
        public const int MaxPerTree = 2;

        public HarvesterCrew crew;
        public BananaOrchard orchard;           // les bananiers : il ramasse sous tous ceux qui sont plantés
        public Panier panier;
        public Vector3 home;                    // sa place au tout début (au sol), avant d'aller vers les arbres
        public Vector3 areaCenter;              // le milieu de la terrasse de la bananeraie (en coordonnées du monde)
        public float areaRadius = 3f;           // la terrasse, vue comme un disque : on y repose le singe lancé (HarvesterGrab)

        public float jumpDuration = 0.5f;       // en secondes, le saut pour attraper la banane
        public float throwDuration = 0.45f;     // en secondes, le vol de la banane jusqu'au panier
        public float turnSpeed = 540f;          // en degrés par seconde
        [Range(0f, 1f)] public float dunkChance = 0.2f;   // chance de dunker au lieu de lancer
        [Range(0f, 1f)] public float missChance = 0.25f;  // chance de rater un lancer normal

        public bool Walking { get; private set; }
        public Banane Carried { get; private set; }     // la banane qu'il porte
        public Banane Extra { get; private set; }       // la 2e banane, portée par-dessus la première (null : une seule)
        public float ThrowPhase { get; private set; }   // 0 = rien, monte à 1 pendant le geste du lancer
        public float height = 0.55f;                    // taille du singe, en mètres
        public bool Cheering { get; private set; }      // bras levés : il fête son dunk
        public bool Sulking { get; private set; }       // il secoue la tête : il a raté (ou il est sonné après un lancer)
        public bool Held { get; private set; }          // le joueur l'a pris dans sa main (voir HarvesterGrab) : il gigote

        public Bananier Tree { get; private set; }      // l'arbre où il travaille (ou attend)
        public bool Busy { get; private set; }          // parti chercher des bananes : compte dans les 2 singes au plus par arbre
        public float StandX { get; private set; }       // où il se tient le long de l'étal (repère de l'étal, -0,6 à 0,6 m)

        Banane claimed, claimedExtra;                   // les bananes qu'il est parti chercher (les autres singes les laissent)
        int basketSlot = -1;                            // sa place autour du panier (HarvesterCrew), -1 : aucune

        // Appelé par l'équipe quand on achète ce singe : il apparaît et se met au travail
        public void StartWork()
        {
            gameObject.SetActive(true);
            StartCoroutine(Work());
        }

        // Le joueur le prend dans sa main : il arrête tout, lâche ses bananes et libère celles qu'il visait
        public void PickedUp()
        {
            StopAllCoroutines();
            Held = true;
            Walking = Cheering = Sulking = Busy = false;
            ThrowPhase = 0f;
            if (claimed) crew.Unclaim(claimed);
            if (claimedExtra) crew.Unclaim(claimedExtra);
            claimed = claimedExtra = null;
            LeaveBasket();
            Drop(Carried);
            Drop(Extra);
            Carried = Extra = null;
        }

        // Une banane qu'il tenait retombe : le joueur ou un autre singe pourra la reprendre
        static void Drop(Banane banana)
        {
            if (!banana) return;
            foreach (var c in banana.GetComponentsInChildren<Collider>()) c.enabled = true;
            banana.Lachee();
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

        // La boucle de travail : choisir un arbre et une banane, aller, attraper (une ou deux), porter, lancer, souffler.
        IEnumerator Work()
        {
            while (true)
            {
                var banana = FindBanana(out var tree);
                if (!banana)
                {
                    // Rien à ramasser : il attend dans la zone d'un arbre, là où il y a le moins de singes
                    Busy = false;
                    Tree = QuietestTree();
                    yield return WalkTo(IdleSpot(Tree), null);
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                Tree = tree;
                Busy = true;
                crew.Claim(banana);   // les autres singes la laissent : c'est la sienne
                claimed = banana;
                yield return Fetch(banana);
                crew.Unclaim(banana);
                claimed = null;
                Busy = false;
                yield return new WaitForSeconds(crew.Pause);
            }
        }

        IEnumerator Fetch(Banane banana)
        {
            // Le joueur peut la prendre avant nous : on abandonne et on en cherche une autre
            Func<bool> lost = () => !banana || banana.EnMain || banana.Deposee;
            var spot = PickSpot(banana);
            if (OnTable(banana)) yield return WalkTo(ZoneSpot(Tree, StandX), lost);   // d'abord la zone devant l'étal, puis le bord
            yield return WalkTo(spot, lost);
            if (lost()) yield break;
            yield return JumpAndGrab(banana);

            // Une autre attend sur le même étal : il la prend aussi (deux bananes par trajet)
            var second = FindBananaOn(Tree, banana);
            if (second && OnTable(second))
            {
                crew.Claim(second);
                claimedExtra = second;
                Func<bool> lost2 = () => !second || second.EnMain || second.Deposee;
                yield return WalkTo(PickSpot(second), lost2);
                if (!lost2()) yield return JumpAndGrab(second);
                crew.Unclaim(second);
                claimedExtra = null;
            }

            // Retour par la zone de l'étal (on s'écarte de la table avant de traverser la terrasse)
            yield return WalkTo(ZoneSpot(Tree, StandX), null);
            yield return Deliver();
        }

        // --- Choisir ---

        // L'arbre où aller et sa banane : d'abord un arbre avec une banane dorée, sinon celui qui a le plus de bananes
        // en attente (à égalité, le plus proche). On ne va pas à un arbre où travaillent déjà 2 autres singes.
        Banane FindBanana(out Bananier bestTree)
        {
            Banane best = null;
            bestTree = null;
            int bestWaiting = 0;
            bool bestGolden = false;
            float bestDistance = float.MaxValue;
            foreach (var tree in orchard.Active)
            {
                if (crew.WorkingAt(tree, this) >= MaxPerTree) continue;
                var banana = FindBananaOn(tree, null);
                if (!banana) continue;
                int waiting = Waiting(tree);
                bool golden = banana.EstDoree;
                float distance = (tree.transform.position - transform.position).sqrMagnitude;
                bool better = !best
                    || (golden && !bestGolden)
                    || (golden == bestGolden && (waiting > bestWaiting || (waiting == bestWaiting && distance < bestDistance)));
                if (!better) continue;
                best = banana;
                bestTree = tree;
                bestWaiting = waiting;
                bestGolden = golden;
                bestDistance = distance;
            }
            return best;
        }

        // Sur cet arbre : une banane dorée s'il y en a, sinon celle posée depuis le plus longtemps (la plus proche de pourrir).
        // 'except' : une banane à ne pas prendre (celle qu'il tient déjà).
        Banane FindBananaOn(Bananier tree, Banane except)
        {
            Banane best = null;
            foreach (var b in tree.BananesAuSol)
            {
                if (b == except || !Available(b)) continue;
                if (!best || (b.EstDoree && !best.EstDoree) || (b.EstDoree == best.EstDoree && b.Progression > best.Progression)) best = b;
            }
            return best;
        }

        int Waiting(Bananier tree)
        {
            int n = 0;
            foreach (var b in tree.BananesAuSol) if (Available(b)) n++;
            return n;
        }

        bool Available(Banane b) => b && !b.EnMain && !b.Deposee && !b.EstPourrie && !crew.IsClaimed(b) && AtRest(b);

        // L'arbre où il y a le moins de singes (pour attendre sans se tasser), en restant au sien si c'est pareil
        Bananier QuietestTree()
        {
            Bananier best = Tree;
            int bestCount = best ? crew.At(best, this) : int.MaxValue;
            foreach (var tree in orchard.Active)
            {
                int count = crew.At(tree, this);
                if (count < bestCount) { best = tree; bestCount = count; }
            }
            return best ? best : orchard.First;
        }

        // Immobile : finie sa chute depuis l'arbre, ou retombée (par terre, sur un meuble…) après un lâcher du joueur
        static bool AtRest(Banane b)
        {
            var body = b.GetComponent<Rigidbody>();
            return body.isKinematic ? b.Posee : body.linearVelocity.sqrMagnitude < 0.01f;
        }

        // --- Où se mettre ---

        // L'étal d'un arbre, et de quel côté est la terrasse (+1 ou -1 sur son axe Z)
        static Transform Stall(Bananier tree) => tree ? tree.versCible : null;
        float TerraceSide(Transform stall) => Mathf.Sign(stall.InverseTransformPoint(areaCenter).z);

        // La banane est-elle sur l'étal de son arbre (1,4 × 1 m) ?
        static bool OnTable(Banane banana)
        {
            var stall = banana.Source ? Stall(banana.Source) : null;
            if (!stall) return false;
            var local = stall.InverseTransformPoint(banana.transform.position);
            return Mathf.Abs(local.x) < 0.75f && Mathf.Abs(local.z) < 0.55f;
        }

        // Où se mettre pour la prendre : au bord de l'étal, sous la banane, si elle est dessus ; sinon juste devant elle
        Vector3 PickSpot(Banane banana)
        {
            var bananaPos = banana.transform.position;
            var stall = banana.Source ? Stall(banana.Source) : null;
            if (!stall || !OnTable(banana))
            {
                var toMonkey = transform.position - bananaPos;
                toMonkey.y = 0f;
                var spot = bananaPos + toMonkey.normalized * 0.2f;
                spot.y = home.y;
                return spot;
            }
            // Au bord de l'étal, côté terrasse ; si l'autre singe de cet arbre est déjà là, on se décale de son côté libre
            float x = Mathf.Clamp(stall.InverseTransformPoint(bananaPos).x, -StallHalfWidth, StallHalfWidth);
            var other = crew.OtherAt(banana.Source, this);
            if (other && Mathf.Abs(other.StandX - x) < Elbow)
            {
                float shifted = other.StandX + (x >= other.StandX ? Elbow : -Elbow);
                if (Mathf.Abs(shifted) > StallHalfWidth) shifted = other.StandX - Mathf.Sign(shifted - other.StandX) * Elbow;
                x = Mathf.Clamp(shifted, -StallHalfWidth, StallHalfWidth);
            }
            StandX = x;
            var edge = stall.TransformPoint(new Vector3(x, 0f, TerraceSide(stall) * StallEdge));
            edge.y = home.y;
            return edge;
        }

        // La zone invisible devant l'étal, côté terrasse, au niveau de 'x' le long de l'étal
        Vector3 ZoneSpot(Bananier tree, float x)
        {
            var stall = Stall(tree);
            if (!stall) return home;
            var spot = stall.TransformPoint(new Vector3(x, 0f, TerraceSide(stall) * ZoneDistance));
            spot.y = home.y;
            return spot;
        }

        // Où attendre dans la zone d'un arbre : à gauche ou à droite du milieu, selon qu'un autre singe y attend déjà
        Vector3 IdleSpot(Bananier tree)
        {
            var other = crew.OtherAt(tree, this);
            float x = other && other.StandX <= 0f ? Elbow / 2f + 0.1f : -(Elbow / 2f + 0.1f);
            StandX = x;
            return ZoneSpot(tree, x);
        }

        // Sa place autour du panier, à « distance » de son centre : 4 places en arc, côté terrasse
        Vector3 BasketSpot(float distance)
        {
            var basket = panier.transform.position;
            var dir = areaCenter - basket;
            dir.y = 0f;
            dir = Quaternion.Euler(0f, HarvesterCrew.BasketAngles[Mathf.Max(0, basketSlot)], 0f) * dir.normalized;
            var spot = basket + dir * distance;
            spot.y = home.y;
            return spot;
        }

        void TakeBasket() { if (basketSlot < 0) basketSlot = crew.TakeBasketSlot(); }
        void LeaveBasket() { crew.FreeBasketSlot(basketSlot); basketSlot = -1; }

        // --- Marcher ---

        // Marcher jusqu'à target, en ligne droite : la terrasse est libre entre les zones des étals et les places du panier.
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
                transform.position = Vector3.MoveTowards(transform.position, target, crew.Speed * Time.deltaTime);
                yield return null;
            }
            Walking = false;
        }

        // --- Attraper ---

        // Il se tourne vers la banane, saute, l'attrape en haut du saut et retombe avec.
        IEnumerator JumpAndGrab(Banane banana)
        {
            var look = banana.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);

            // Assez haut pour que ses mains levées atteignent la banane
            float jumpHeight = Mathf.Max(0.1f, banana.transform.position.y - (home.y + height * 1.1f));
            var ground = transform.position;
            bool got = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / jumpDuration)
            {
                transform.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * jumpHeight);
                if (!got && t >= 0.5f) { Grab(banana); got = true; }
                yield return null;
            }
            transform.position = ground;
            if (!got) Grab(banana);
        }

        // Dans ses mains : la première banane, ou la 2e par-dessus
        void Grab(Banane banana)
        {
            if (!banana) return;
            if (!Carried) Carried = banana;
            else Extra = banana;
            banana.PriseParSinge();   // elle ne pourrit plus en disparaissant, le panier ne la compte pas toute seule
            var body = banana.GetComponent<Rigidbody>();
            body.isKinematic = true;   // lâchée par le joueur, elle tombait : dans ses mains, plus de gravité
            // Le joueur ne peut plus la lui prendre des mains
            foreach (var c in banana.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        // --- Livrer ---

        // Porter les bananes jusqu'au panier et les y mettre : dunk, lancer réussi, ou lancer raté (et on recommence).
        IEnumerator Deliver()
        {
            TakeBasket();
            while (Carried)
            {
                if (Random.value < dunkChance)
                {
                    yield return WalkTo(BasketSpot(DunkDistance), null);
                    yield return Dunk();
                    break;
                }
                yield return WalkTo(BasketSpot(ThrowDistance), null);
                bool miss = Random.value < missChance;
                yield return Throw(miss);
                if (!miss) break;

                // Raté : il boude, puis va ramasser la première par terre, et retente (la 2e reste pour qui veut)
                var banana = lastThrown;
                yield return Sulk();
                if (!banana) break;
                yield return WalkTo(banana.transform.position - (banana.transform.position - transform.position).normalized * 0.25f, null);
                Carried = banana;
                banana.PriseParSinge();   // de nouveau en main : elle ne pourrit plus
            }
            LeaveBasket();
        }

        Banane lastThrown;

        // Le geste : les bras partent en arrière puis vers l'avant ; les bananes partent en cloche
        // jusqu'au panier, ou à côté si c'est raté (elles restent alors par terre).
        IEnumerator Throw(bool miss)
        {
            FaceBasket();
            for (ThrowPhase = 0f; ThrowPhase < 1f; ThrowPhase += Time.deltaTime / 0.35f) yield return null;

            var banana = Carried;
            var extra = Extra;
            Carried = Extra = null;
            lastThrown = banana;
            if (banana)
            {
                var to = panier.transform.position + Vector3.up * 0.05f;
                var extraTo = to;
                if (miss)
                {
                    // À côté du panier, par terre : trop court ou trop à droite / à gauche
                    var aside = Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * (transform.position - panier.transform.position);
                    to = panier.transform.position + aside.normalized * 0.45f;
                    to.y = home.y + 0.05f;
                    extraTo = to + transform.right * 0.2f;
                }
                if (extra) StartCoroutine(Fly(extra, extraTo, throwDuration * 1.1f));   // la 2e part juste avec la première
                yield return Fly(banana, to, throwDuration);
                if (extra) yield return new WaitForSeconds(throwDuration * 0.1f);
                if (!miss)
                {
                    if (banana) panier.RecevoirPart(banana, crew.Share);
                    if (extra) panier.RecevoirPart(extra, crew.Share);
                }
                else
                {
                    if (banana) banana.PoseeParTerre();   // par terre, elles se remettent à pourrir
                    if (extra) { Drop(extra); extra.PoseeParTerre(); }   // la 2e : un autre singe (ou le joueur) la ramassera
                }
            }
            ThrowPhase = 0f;
        }

        // Le dunk : grand saut au-dessus du panier, il y écrase les bananes, retombe et fait un tour sur lui-même, bras levés.
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
                    StartCoroutine(SlamInto(Carried));
                    if (Extra) StartCoroutine(SlamInto(Extra));
                    Carried = Extra = null;
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
