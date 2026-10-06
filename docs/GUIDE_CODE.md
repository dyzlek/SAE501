# Guide du code — bonnes pratiques Unity / C# / VR

> Ce guide reprend ce qui est utile pour coder dans le projet, à partir des 7 supports de cours « Dispositifs interactifs » de **Davide Di Pierro**, qui note la **qualité technique** : code, architecture, stabilité, interactions VR et capacité à **expliquer** son code.
> **Claude le relit avant d'écrire ou de modifier du code.** Les règles de projet (journal, prompts, Git, confort) restent dans [CLAUDE.md](../CLAUDE.md).

Les supports :
1. C#, programmation par événements et premiers composants de Unity
2. Première installation et configurations de base (Unity Hub, Meta Quest, SDK Meta)
3. Interaction avec la VR (XR Origin, mains, locomotion, téléportation, saisie d'objets)
4. Interaction avec des objets (arme, tir)
5. Interactions avancées (UI en VR, Ray Interactor, groupes d'interactions et consignes de la SAÉ)
6. Assigner des scores (environnement, score, vie, adversaire)

---

## 0. L'état d'esprit du cours
- L'IA est permise, mais ce qui compte, c'est **la créativité, l'esprit critique et la responsabilité**. On doit comprendre chaque ligne et savoir l'expliquer à l'oral.
- Il faut donc un code **simple et lisible** : pas d'abstraction « au cas où », pas de framework maison. Un script fait une seule chose et son nom dit laquelle.

## 1. Le minimum demandé par la SAÉ (support 5)
À vérifier avant chaque rendu :
- [ ] un **joueur** ;
- [ ] un **personnage Sprite** ;
- [ ] une **interaction** (lancer des objets, tirer…) ;
- [ ] **deux scènes** ;
- [ ] un **score** ;
- [ ] de la **physique de base** (gravité, collisions…).

## 2. Organisation du projet
- **Tout ce qui est à nous va dans `Unity/Assets/_Project/`** : `Scripts/`, `Prefabs/`, `Scenes/`, `Art/`. Rien ne se pose directement à la racine d'`Assets/` (les imports `.glb` vont dans `Art/<Sujet>/`). Les packages importés (Samples XRI, Synty, etc.) gardent leur dossier d'origine.
- Les scripts sont rangés par domaine (`Core/`, `Hub/`, `Map/`, `Player/`, `Runtime/`). Les scripts d'éditeur vont dans `Editor/` : ils ne partent pas dans le build.
- **Dans une scène, on range la hiérarchie** (support 6) : un objet vide `Environment` regroupe tout le décor, et de la même façon `Managers`, `UI`, etc.
- **Les prefabs avant tout** (support 1) : tout objet réutilisé ou modifié par plusieurs personnes devient un prefab. On peut copier-coller entre scènes ce qui marche seul (XR Origin, une arme…).
- **1 unité Unity = 1 m.** Les tailles et distances sont en mètres dans les commentaires.

## 3. Écrire du C# (support 1)
**Nommage et fichiers**
- Une classe par fichier, et **le fichier porte le nom de la classe** : sinon Unity ne trouve pas le composant.
- `PascalCase` pour les classes, méthodes et propriétés ; `camelCase` pour les champs et variables locales.
- Tout le code du projet est dans `namespace SAE { … }`.
- Les commentaires sont en français et expliquent le **pourquoi**, pas le quoi. On met une ligne en tête de classe pour dire son rôle (voir `Core/Aura.cs`).

**Types et constantes**
- On choisit le bon type : `int` pour un compteur, `float` pour les positions et durées (Unity travaille en `float`), `bool` pour un état, `string` seulement pour l'affichage.
- Pas de « nombre magique » : `public const` ou un champ réglable. Les tags passent par la classe `Tags` (`Core/Tags.cs`), jamais une chaîne recopiée.
- Les collections : `List<T>` (taille variable), `Dictionary<K,V>` (accès par clé), `Queue<T>` (file d'attente, ex. des ballons à faire apparaître), `Stack<T>`. Pas d'`ArrayList` non typée.

**Encapsulation et visibilité**
- Ce qu'on règle dans l'Inspector est un champ `public`, avec un commentaire qui donne l'unité ou le sens (c'est ce que font le cours et le code existant).
- L'état interne est `private` (c'est la visibilité par défaut). Si un autre script doit le **lire**, on l'expose par une propriété en lecture seule : `public int Score { get; private set; }`.
- Un autre script ne modifie jamais directement l'état d'un objet : il appelle une méthode (`AddPoint(3)`, `SubtractLife()`), qui peut vérifier la valeur et mettre l'affichage à jour.

**Héritage et polymorphisme**
- On s'en sert seulement quand plusieurs variantes partagent un vrai comportement commun : classe `abstract` avec une méthode `abstract`, puis `override` dans chaque variante (ex. `Animal` / `Cat` dans le cours). Sinon, mieux vaut un seul script avec des champs réglables.
- Les interfaces (`IClickable`, `IMonkeyInfo`) servent quand des objets très différents doivent répondre à la même action.

**Exceptions**
- `try/catch` seulement autour de ce qui peut vraiment échouer (fichiers, sauvegarde). Pour la logique de jeu, on teste plutôt les cas : `if (x != null)`.

## 4. Le cycle de vie Unity (support 1)
Les méthodes « événements » sont appelées par Unity tout seul (voir [l'ordre d'exécution](https://docs.unity3d.com/6000.2/Documentation/Manual/execution-order.html)) :

| Méthode | Quand | On y met |
|---|---|---|
| `Awake()` | à la création de l'objet | ses **propres** références (`GetComponent`) et sa construction |
| `Start()` | avant la 1re frame | l'initialisation de l'état (vie au max, score à 0) et les liens vers **les autres** objets |
| `Update()` | à chaque frame | ce qui doit vraiment changer à chaque image (lecture d'un input, animation légère) |
| `OnCollisionEnter` / `OnTriggerEnter` | deux colliders se touchent | la réaction au contact (dégâts, score) |
| `OnEnable` / `OnDisable` | activation / désactivation | s'abonner / se désabonner des événements |

Règles :
- **Pas de `GetComponent`, `Find…` ni `FindFirstObjectByType` dans `Update`** : on garde la référence une fois pour toutes dans `Awake` ou `Start`. Le cours utilise `FindFirstObjectByType<ScoreManager>()` dans une collision, ce qui passe pour un événement rare, mais une référence gardée en mémoire ou glissée dans l'Inspector est meilleure.
- Un `Update` doit rester court. Pour les minuteries, on préfère `InvokeRepeating(nameof(Shoot), delai, intervalle)` (support 6) ou une coroutine.

## 5. Programmer par événements (supports 1, 3 et 4)
Le principe : **Événement → Listener → Handler**. On ne vérifie pas en boucle « est-ce que c'est arrivé ? », on réagit quand ça arrive.
- **UnityEvent dans l'Inspector** : par exemple, dans *XR Grab Interactable → Interactable Events → Activate*, on ajoute `Pistol.FireBullet()`. Les événements utiles : *Hover Entered/Exited* (l'objet s'éclaire quand la main approche), *Select Entered/Exited* (saisi/lâché), *Activate* (gâchette pendant qu'on tient l'objet).
- **En code**, avec l'Input System :
  ```csharp
  void OnEnable()  { action.action.performed += OnPressed; }
  void OnDisable() { action.action.performed -= OnPressed; } // toujours se désabonner
  void OnPressed(InputAction.CallbackContext ctx) { … }
  ```
  `WasPressedThisFrame()` et `WasReleasedThisFrame()` servent pour « au moment où on appuie / relâche ».
- Un événement = une méthode **publique** au nom explicite (`FireBullet`, `AddPoint`), pour qu'on puisse la brancher depuis l'Inspector.

## 6. Physique et collisions (supports 1, 4 et 6)
- **Collider** = forme physique. **Is Trigger** = il détecte sans bloquer (`OnTriggerEnter`). Sans trigger, l'objet bloque et la collision appelle `OnCollisionEnter`.
- Pour recevoir un événement de collision, **au moins un des deux objets doit avoir un `Rigidbody`**.
- **Colliders simples** : on retire les *Mesh Collider* des sous-parties d'un modèle et on met un seul *Box/Capsule/Sphere Collider* sur la racine (support 4). C'est plus fiable et plus rapide.
- On compare les tags avec `CompareTag(Tags.Ballon)`, jamais avec `tag == "…"`.
- Les **layers** servent à filtrer qui touche qui (Player, Enemy, PlayerBullet…), et le joueur VR est sur *Ignore Raycast* (support 3).
- **Projectile** (support 4) : `Instantiate(prefab, firePoint.position, firePoint.rotation)`, puis `rb.linearVelocity = firePoint.forward * vitesse;` (Unity 6 : `linearVelocity`, plus `velocity`), puis `Destroy(balle, dureeDeVie);` pour ne pas accumuler d'objets.
- **Mieux que le cours** : au lieu d'enchaîner les `if (CompareTag("Target 3")) … else if ("Target 2")…`, on met un petit composant sur la cible avec `public int points`, et la balle lit `GetComponent<Cible>()`. Le code est plus court, et ajouter une cible ne demande pas de toucher au code.

## 7. Mettre en place la VR (supports 2 et 3)
Configuration (une seule fois, déjà faite dans le projet : **XRI 3.6.1 + OpenXR**) :
- Build Profiles → **Meta Quest / Android** ; OpenXR coché ; toutes les interactions sauf *Eye Gaze* ; *Meta → Tools → Project Setup Tool → Fix All / Apply All* ; *Project Settings → XR Interaction Toolkit → Project Validation → Fix All*.
- Importer les Samples XRI : **Starter Assets** (actions, prefabs, interacteurs) et **XR Interaction Simulator** (tester sans casque).

Le rig du joueur :
- **XR Origin (VR)**, avec *Tracking Origin Mode = Floor*. La caméra porte un *Tracked Pose Driver* (Center Eye/HMD).
- Les mains `Left Hand` / `Right Hand` sont sous *Camera Offset*, avec *Tracked Pose Driver (Input System)* branché sur `XRI Left|Right/Position, Rotation, Tracking State`.
- **Character Controller** sur le joueur (*Radius* 0,1 ; *Center Y* 1).
- **Locomotion** : un objet `Locomotion` avec *Locomotion Mediator*, et en enfants les *providers* :
  - *Continuous Move* (stick gauche, *Forward Source* = caméra) ;
  - ***Snap Turn*** (stick droit). On **désactive le Continuous Turn**, qui donne la nausée (règle de confort) ;
  - *Gravity Provider* (*Transformation Priority* 10) ;
  - **Teleportation Provider** + *Teleport Interactor* sous la main droite + *Teleportation Area* (sol, layer d'interaction *Teleport*) ou *Teleportation Anchor* (point précis) ;
  - un *Teleportation Activator* qui n'affiche le rayon que pendant l'appui.
- **Mains animées** : un `AnimateHandOnInput` lit `Activate Value` → paramètre `Trigger` et `Select Value` → paramètre `Grip` de l'Animator.

Lire un input :
```csharp
public InputActionProperty gripValue;          // Use Reference → XRI Left Interaction/Select Value
float grip = gripValue.action.ReadValue<float>();
```
- Les actions XRI sont activées par l'*Input Action Manager* du rig. Une `InputAction` créée à la main dans un script doit être activée avec `Enable()` dans `OnEnable` et désactivée avec `Disable()` dans `OnDisable`.
- Correspondance des boutons : *Select* = **grip** (saisir), *Activate* = **gâchette** (utiliser/tirer), stick gauche = déplacement, stick droit = rotation / téléportation.

## 8. Saisir et utiliser des objets (supports 3 et 4)
- **Direct Interactor** sur chaque main (*Select* et *Activate* branchés sur les actions XRI de la main), avec un *Sphere Collider* **trigger de rayon 0,1** et *Improve Accuracy With Sphere Collider*, sans le layer *Teleport*.
- **XR Grab Interactable** sur l'objet (avec un collider et un Rigidbody) :
  - **Attach Transform** : un objet vide enfant qui indique comment l'objet tombe dans la main (ex. pistolet tourné de -90° en Y) ;
  - *Use Dynamic Attach* : l'objet reste là où on l'a attrapé ;
  - *Movement Type* : *Instantaneous* (aucune physique, le plus stable), *Kinematic*, ou *Velocity Tracking* (garde l'élan, pour **lancer**).
- **Ray Interactor** (pointer de loin) : on le met sur son propre *Interaction Layer* « Ray » pour qu'il ne prenne que ce qui est prévu pour, et on rend invisible le rayon « invalide » (alpha 0).
- **XR Simple Interactable** : un objet qu'on survole ou qu'on active sans le prendre (bouton, cible).
- **XR Interaction Group** sur chaque main (Teleport, Direct, Ray) : **une seule interaction à la fois**, pour qu'on ne téléporte pas en tenant un objet.

## 9. L'interface en VR (supports 5 et 6)
- **Toujours un Canvas en *World Space*** : *Scale* (0,001 ; 0,001 ; 0,001), posé dans le décor à **1-2 m**. On y met *Tracking Device Graphic Raycaster* pour que le rayon le touche.
- Sur l'*EventSystem*, on remplace *Input System UI Input Module* par **XR UI Input Module** (au casque).
- Textes en **TextMeshPro** (`TMP_Text`) et icônes en **Sprite** (*Texture Type → Sprite (2D and UI)*, découpe dans le *Sprite Editor*).
- ⚠️ **Attention** : le support 6 met le score en *Screen Space – Overlay* sous la caméra. **On ne le fait pas** : c'est du texte collé au visage, interdit par nos règles de confort. Le score et la vie s'affichent sur un panneau dans le décor, ou sur la main / le poignet.
- **Un seul script centralise l'affichage** (support 6) : `AddPoint(int)`, `SubtractLife()`, puis une seule méthode `UpdateUI()` qui réécrit les textes. Personne d'autre n'écrit dans les textes.

## 10. Tester (supports 3 et 4)
- **Sans casque** : le prefab *XR Interaction Simulator* glissé dans la scène, puis Play. On peut ajouter des bindings clavier/souris dans *XRI Default Input Actions* (clic gauche sur *Select Value*, Espace sur *Jump*, molette sur *Teleport Mode*). Dans le simulateur, *Activate* = **X**.
- **Déboguer** : `Debug.Log` (à retirer ou garder discret avant le rendu) et *Window → Analysis → XR Interaction Debugger* pour voir les inputs en direct.
- **Au casque** : *File → Build Profiles* → scène ajoutée à la *Scene List* → Android/Meta Quest → *Run Device* = le Quest → **Build and Run**. Câble USB-C conseillé.
- Le simulateur ne remplace pas le casque : **on teste au casque chaque jour** (CLAUDE.md).

## 11. Performance (objectif ≥ 72 fps sur Quest)
- Modèles low poly (le cours utilise Synty Polygon et LowPolyWeapons), peu de lumières en temps réel, des colliders simples.
- Pas de recherche d'objet ni d'allocation dans `Update` : on garde les références en mémoire.
- On détruit ou on recycle ce qu'on fait apparaître (balles, ballons) : `Destroy(obj, t)` au minimum.

## 12. Avant chaque commit (check-list pour Claude)
- [ ] Le code compile, et la console n'a pas d'erreur en Play.
- [ ] Les noms sont clairs, une classe par fichier, `namespace SAE`, et les commentaires expliquent le pourquoi.
- [ ] Aucun `Find`/`GetComponent` dans `Update` ; tags via `Tags` ; aucune chaîne ni nombre magique.
- [ ] Les abonnements aux événements sont retirés dans `OnDisable`.
- [ ] Le confort est respecté : snap turn, téléportation, UI en World Space à 1-2 m, rien de collé à l'écran.
- [ ] Le fichier est dans `Assets/_Project/…`, et rien n'est posé à la racine d'`Assets/`.
- [ ] On sait l'expliquer à l'oral en 2 phrases.
- [ ] Journal et prompts mis à jour dans le même commit (CLAUDE.md, règles 1 et 2).

## Liens utiles (cités dans les supports)
- Installation Meta pour Unity : <https://developers.meta.com/horizon/documentation/unity/unity-project-setup/> et <https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/>
- Tutoriel « premières interactions » : <https://www.youtube.com/watch?v=H8dWVlZKQu4&t=3327s>
- UI Toolkit en World Space pour la VR : <https://www.youtube.com/watch?v=XJRxGHENrjc>
- Cours C# : <https://introprogramming.info/>
- Livre : Borromeo & Gomila Salas, *Hands-On Unity Game Development* (Packt, 2024)
