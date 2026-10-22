using System;
using System.Collections;
using UnityEngine;

namespace SAE
{
    // La roue de la roulette. On la lance À LA MAIN, comme un croupier : on attrape le plateau (WheelGrab : grip en VR,
    // clic maintenu en mode PC), on le fait tourner, on lâche. La roue garde l'élan du geste ; s'il est trop mou,
    // le code la relance plus vite (il y a toujours du spectacle), puis elle ralentit et la bille s'arrête
    // dans la case tirée, juste sous le repère doré (côté joueur, -Z local de la roue).
    // Le tirage est fait par la table AU LÂCHER (RouletteTable.Launch) : le geste ne décide que du spectacle, pas du résultat.
    // Sans pari valide, la roue tourne pour rien, puis s'arrête doucement (frottement).
    // Un « tic » à chaque case qui passe : sous le repère quand on la fait tourner, sous la bille pendant un lancer.
    public class RouletteWheel : MonoBehaviour
    {
        public RouletteTable table;
        public Transform rotor;             // le plateau à cases qui tourne (case i à l'angle i × 360/37, voir RouletteRules.WheelOrder)
        public Transform ball;              // la bille, enfant de la roue (elle ne tourne pas avec le plateau pendant un lancer)
        public float pocketRadius = 0.2f;   // rayon où la bille se pose, en mètres
        public float rimRadius = 0.27f;     // rayon où la bille roule au début, contre le bord
        public float pocketHeight = 0.03f;  // hauteur du centre de la bille posée dans une case
        public float rimHeight = 0.03f;     // hauteur du centre de la bille sur la piste (plus haute : la cuvette remonte vers le bord)
        public int ballTurns = 5;           // tours de la bille pendant un lancer, dans l'autre sens que le plateau

        public float launchSpeed = 90f;     // °/s : en dessous, le geste n'est pas un lancer (la roue roule un peu, sans pari)
        public float minSpeed = 300f;       // °/s : un lancer trop mou est relancé à cette vitesse
        public float maxSpeed = 900f;       // °/s : un lancer trop fort est ramené à cette vitesse
        public float friction = 180f;       // °/s² : une roue lâchée sans pari ralentit puis s'arrête

        const float PointerAngle = 180f;    // le repère, côté joueur

        public bool Spinning { get; private set; }   // un lancer avec pari est en cours
        public bool Dragging { get; private set; }   // une main (ou la souris) fait tourner la roue

        float rotorAngle;
        float coastSpeed;                   // °/s : la roue lâchée sans pari, qui ralentit
        float dragSpeed;                    // °/s : la vitesse de la main, lissée sur les dernières images
        float ballAngle;                    // la bille, en degrés autour de la roue (repère = PointerAngle)
        bool ballInPocket;                  // après un lancer, la bille est dans sa case : elle tourne avec le plateau
        AudioSource tickSource;
        int lastPocket;

        void Start()
        {
            ballAngle = PointerAngle;
            PlaceBall(ballAngle, rimRadius);
            tickSource = gameObject.AddComponent<AudioSource>();
            tickSource.playOnAwake = false;
            tickSource.spatialBlend = 1f;   // le son vient de la roue
            tickSource.minDistance = 1.5f;
            tickSource.volume = 0.5f;
        }

        // La roue lâchée sans pari continue sur son élan et ralentit
        void Update()
        {
            if (Spinning || Dragging || coastSpeed == 0f) return;
            coastSpeed = Mathf.MoveTowards(coastSpeed, 0f, friction * Time.deltaTime);
            TurnRotor(coastSpeed * Time.deltaTime);
        }

        // --- Le geste (WheelGrab) ---

        public bool BeginDrag()
        {
            if (Spinning) return false;   // pendant un lancer, on ne touche plus à la roue
            Dragging = true;
            coastSpeed = 0f;
            dragSpeed = 0f;
            return true;
        }

        // La main a fait tourner le plateau de 'degrees' depuis l'image d'avant
        public void Drag(float degrees)
        {
            if (!Dragging) return;
            TurnRotor(degrees);
            float instant = degrees / Mathf.Max(Time.deltaTime, 0.001f);
            dragSpeed = Mathf.Lerp(dragSpeed, instant, 0.35f);   // lissé : un à-coup d'une image ne compte pas tout seul
        }

        // On lâche : un vrai lancer si le geste est assez rapide et le pari valide. Renvoie true si c'est un lancer.
        public bool EndDrag()
        {
            if (!Dragging) return false;
            Dragging = false;
            if (Mathf.Abs(dragSpeed) >= launchSpeed && table && table.Launch(dragSpeed)) return true;
            coastSpeed = dragSpeed;   // pas un lancer : elle roule sur son élan, puis s'arrête
            return false;
        }

        // --- Le lancer (appelé par la table, le résultat déjà tiré) ---

        public void Spin(int result, float speed, Action onDone)
        {
            if (!Spinning) StartCoroutine(SpinRoutine(result, speed, onDone));
        }

        IEnumerator SpinRoutine(int result, float speed, Action onDone)
        {
            Spinning = true;
            float dir = speed < 0f ? -1f : 1f;                                  // le sens du geste
            float v = Mathf.Clamp(Mathf.Abs(speed), minSpeed, maxSpeed);

            // Le plateau finit avec la case tirée sous le repère : angle de la case + angle du plateau = repère.
            // On va jusqu'à elle dans le sens du geste, assez loin pour que la roue parte à la vitesse v et ralentisse
            // en douceur (courbe 1 - (1 - t)³ : vitesse de départ = 3 × distance / durée).
            float pocketAngle = Array.IndexOf(RouletteRules.WheelOrder, result) * 360f / RouletteRules.Numbers;
            float start = rotorAngle;
            float distance = Mathf.Repeat(dir * (PointerAngle - pocketAngle - start), 360f);
            float wanted = v * 4.5f / 3f;                                       // ~4,5 s de lancer
            while (distance < wanted) distance += 360f;
            float duration = Mathf.Clamp(3f * distance / v, 3f, 7f);

            // La bille part de là où elle est, à l'envers du plateau, et finit sous le repère
            float ballStart = ballAngle;
            float ballTravel = Mathf.Repeat(-dir * (PointerAngle - ballStart), 360f) + 360f * ballTurns;
            float ballFrom = ballInPocket ? pocketRadius : rimRadius;
            ballInPocket = false;

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float ease = 1f - Mathf.Pow(1f - t, 3f);   // part vite, ralentit doucement
                SetRotor(start + dir * distance * ease);
                ballAngle = ballStart - dir * ballTravel * ease;
                // La bille remonte contre le bord, puis descend vers les cases dans le dernier tiers du lancer
                float radius = t < 0.1f ? Mathf.Lerp(ballFrom, rimRadius, t / 0.1f)
                    : Mathf.Lerp(rimRadius, pocketRadius, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 0.95f, t)));
                PlaceBall(ballAngle, radius);
                TickIfNewPocket(ballAngle);
                yield return null;
            }
            SetRotor(start + dir * distance);
            ballAngle = PointerAngle;
            PlaceBall(ballAngle, pocketRadius);
            ballInPocket = true;
            Spinning = false;
            onDone?.Invoke();
        }

        // --- Interne ---

        // Tourne le plateau à la main ou sur son élan : la bille posée dans sa case tourne avec lui, sinon elle attend au bord
        void TurnRotor(float degrees)
        {
            SetRotor(rotorAngle + degrees);
            if (ballInPocket) { ballAngle += degrees; PlaceBall(ballAngle, pocketRadius); }
            TickIfNewPocket(PointerAngle);   // les cases qui passent sous le repère
        }

        void SetRotor(float angle)
        {
            rotorAngle = Mathf.Repeat(angle, 360f);
            rotor.localRotation = Quaternion.Euler(0f, rotorAngle, 0f);
        }

        // La case du plateau sous 'angle' a changé : un tic (un seul par image, sinon ça bourdonne quand ça va vite)
        void TickIfNewPocket(float angle)
        {
            int pocket = Mathf.FloorToInt(Mathf.Repeat(angle - rotorAngle, 360f) / (360f / RouletteRules.Numbers));
            if (pocket == lastPocket) return;
            lastPocket = pocket;
            if (!tickSource) return;
            tickSource.pitch = UnityEngine.Random.Range(0.8f, 0.9f);   // plus grave que le coffre : une bille sur du bois
            tickSource.PlayOneShot(Sounds.Tick);
        }

        void PlaceBall(float angle, float radius)
        {
            float a = angle * Mathf.Deg2Rad;
            float height = Mathf.Lerp(pocketHeight, rimHeight, Mathf.InverseLerp(pocketRadius, rimRadius, radius));
            ball.localPosition = new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        }
    }
}
