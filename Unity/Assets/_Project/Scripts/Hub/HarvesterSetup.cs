using UnityEngine;

namespace SAE
{
    // Installe le singe récolteur et son panneau dans le hub, au lancement du jeu.
    // Construit en code (et pas dans la scène) pour ne pas toucher à Jeu.unity, qu'une autre personne modifie.
    // Il faut le bananier et le panier de Maxens dans la scène ; sinon rien n'est créé.
    // Placement sur le cercle du hub (voir PrototypeGenerator) : le singe attend entre la table (-172°) et le panier (-157°),
    // un peu vers le centre ; le panneau est à -118°, entre la caisse (-140°) et la bibliothèque de gauche.
    public static class HarvesterSetup
    {
        const float Ring = 5.0f;              // rayon du cercle du hub, en mètres
        const float MonkeySize = 0.55f;       // taille du singe, en mètres
        const float HomeAngle = -171f;
        const float HomeRadius = 3.4f;
        const float BasketAngle = -156f;      // mêmes valeurs que PrototypeGenerator.BasketAngle / BasketRadius
        const float BasketRadius = 3.7f;
        const float PanelAngle = -118f;
        const float PanelRadius = Ring - 0.45f;
        static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var bananier = Object.FindAnyObjectByType<Bananier>();
            var panier = Object.FindAnyObjectByType<Panier>();
            if (!bananier || !panier || !bananier.versCible) return;

            MoveBasket(panier);
            var monkey = BuildMonkey(bananier, panier);
            BuildPanel(monkey);
        }

        static Vector3 Around(float angleDeg, float radius)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * radius, 0f, Mathf.Cos(a) * radius);
        }

        // Le panier était collé à la table et à la caisse : on l'avance vers le centre, avec son socle.
        // (Le générateur le pose déjà là ; ceci sert pour une scène générée avant le changement.)
        static void MoveBasket(Panier panier)
        {
            var socle = GameObject.Find("Socle du panier");
            var basket = panier.transform;
            while (basket.parent && basket.name != "Panier") basket = basket.parent;
            if (!socle || basket.name != "Panier") return;

            var offset = Around(BasketAngle, BasketRadius) - socle.transform.position;
            offset.y = 0f;
            socle.transform.position += offset;
            basket.position += offset;
        }

        static HarvesterMonkey BuildMonkey(Bananier bananier, Panier panier)
        {
            var root = new GameObject("Singe récolteur");
            var home = Around(HomeAngle, HomeRadius);
            root.transform.SetPositionAndRotation(home, Quaternion.LookRotation(-home));   // tourné vers le centre

            // Le singe classique, posé au sol. Le modèle regarde vers -Z : on le retourne pour qu'il marche vers l'avant (+Z).
            var piece = Visuals.MonkeyPiece(new Monkey(MonkeyType.Classique, Rarity.Gris), root.transform,
                                            new Vector3(0f, MonkeySize / 2f, 0f), MonkeySize, withLabel: false);
            piece.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var view = piece.GetComponent<MonkeyView>();
            if (view.aura) Object.Destroy(view.aura.gameObject);   // c'est un employé, pas un singe de la collection

            var monkey = root.AddComponent<HarvesterMonkey>();
            monkey.bananier = bananier;
            monkey.panier = panier;
            monkey.table = bananier.versCible;
            monkey.home = home;
            monkey.Height = MonkeySize;

            if (view.model)
            {
                var animator = root.AddComponent<HarvesterAnimator>();
                animator.model = view.model.transform;
            }
            root.SetActive(false);   // il apparaît quand on l'achète
            return monkey;
        }

        // Pupitre avec 4 boutons : acheter, vitesse, cadence, rendement (même style que le panneau du bananier)
        static void BuildPanel(HarvesterMonkey monkey)
        {
            var root = new GameObject("Panneau récolteur").transform;
            root.SetPositionAndRotation(Around(PanelAngle, PanelRadius), Quaternion.Euler(0f, PanelAngle, 0f));   // +Z local = vers l'extérieur

            Visuals.Solid("Pupitre", root, new Vector3(0, 0.45f, 0.05f), new Vector3(2.0f, 0.9f, 0.3f), Wood);
            Visuals.Solid("Fronton", root, new Vector3(0, 1.5f, 0.18f), new Vector3(2.0f, 0.7f, 0.04f), Wood);
            var title = Visuals.Label(root, "RÉCOLTEUR", new Vector3(0, 1.75f, 0.14f), 0.09f, new Color(1f, 0.9f, 0.4f));
            Object.Destroy(title.GetComponent<Billboard>());

            MakeButton(root, monkey, -0.75f, true, HarvesterStat.Vitesse);
            MakeButton(root, monkey, -0.25f, false, HarvesterStat.Vitesse);
            MakeButton(root, monkey, 0.25f, false, HarvesterStat.Cadence);
            MakeButton(root, monkey, 0.75f, false, HarvesterStat.Rendement);
        }

        static void MakeButton(Transform panel, HarvesterMonkey monkey, float x, bool buy, HarvesterStat stat)
        {
            var button = new GameObject(buy ? "Bouton acheter" : $"Bouton {stat}").transform;
            button.SetParent(panel, false);
            button.localPosition = new Vector3(x, 0.9f, 0.05f);
            button.gameObject.tag = Tags.Bouton;
            var col = button.gameObject.AddComponent<BoxCollider>();   // avant RayPress : l'interactable récupère le collider
            col.size = new Vector3(0.24f, 0.14f, 0.24f);
            col.center = new Vector3(0, 0.05f, 0);

            var socle = Visuals.Box("Socle", button, new Vector3(0, 0.02f, 0), new Vector3(0.22f, 0.02f, 0.22f), new Color(0.15f, 0.15f, 0.17f));
            socle.GetComponent<MeshFilter>().sharedMesh = CylinderMesh;
            var cap = Visuals.Box("Bouton", button, new Vector3(0, 0.07f, 0), new Vector3(0.16f, 0.035f, 0.16f), new Color(0.25f, 0.85f, 0.35f));
            cap.GetComponent<MeshFilter>().sharedMesh = CylinderMesh;

            var label = Visuals.Label(panel, "", new Vector3(x, 1.42f, 0.14f), 0.06f, Color.white);
            Object.Destroy(label.GetComponent<Billboard>());

            var hb = button.gameObject.AddComponent<HarvesterButton>();
            hb.monkey = monkey;
            hb.isBuyButton = buy;
            hb.stat = stat;
            hb.cap = cap.transform;
            hb.label = label;
            button.gameObject.AddComponent<RayPress>();
        }

        // Le maillage du cylindre de Unity, pour des boutons ronds (Visuals.Box fait des cubes)
        static Mesh cylinder;
        static Mesh CylinderMesh
        {
            get
            {
                if (cylinder) return cylinder;
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(tmp);
                return cylinder;
            }
        }
    }
}
