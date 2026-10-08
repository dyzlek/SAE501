using System;
using System.Collections;
using UnityEngine;

namespace SAE
{
    // La roue de la roulette : le plateau à cases tourne, la bille tourne dans l'autre sens, puis les deux ralentissent
    // et la bille s'arrête dans la case tirée, juste sous le repère doré (côté joueur, -Z local de la roue).
    // Le tirage est fait AVANT (RouletteTable) : la roue ne fait que le montrer.
    public class RouletteWheel : MonoBehaviour
    {
        public Transform rotor;          // le plateau à cases qui tourne (case i à l'angle i × 360/37, voir RouletteRules.WheelOrder)
        public Transform ball;           // la bille, enfant de la roue (elle ne tourne pas avec le plateau)
        public float pocketRadius = 0.2f;   // rayon où la bille se pose, en mètres
        public float rimRadius = 0.27f;     // rayon où la bille roule au début, contre le bord
        public float duration = 5f;         // durée d'un lancer, en secondes
        public int rotorTurns = 3;          // tours du plateau
        public int ballTurns = 5;           // tours de la bille, dans l'autre sens

        const float PointerAngle = 180f;    // le repère, côté joueur
        const float BallHeight = 0.03f;

        public bool Spinning { get; private set; }

        float rotorAngle;

        void Start() => PlaceBall(PointerAngle, rimRadius);

        public void Spin(int result, Action onDone)
        {
            if (!Spinning) StartCoroutine(SpinRoutine(result, onDone));
        }

        IEnumerator SpinRoutine(int result, Action onDone)
        {
            Spinning = true;
            float pocketAngle = Array.IndexOf(RouletteRules.WheelOrder, result) * 360f / RouletteRules.Numbers;
            // Le plateau finit avec la case tirée sous le repère : angle de la case + angle du plateau = repère
            float start = rotorAngle;
            float end = start + Mathf.Repeat(PointerAngle - pocketAngle - start, 360f) + 360f * rotorTurns;

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float ease = 1f - Mathf.Pow(1f - t, 3f);   // part vite, ralentit doucement
                rotorAngle = Mathf.Lerp(start, end, ease);
                rotor.localRotation = Quaternion.Euler(0f, rotorAngle, 0f);
                // La bille tourne à l'envers et descend vers les cases dans le dernier tiers du lancer
                float drop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 0.95f, t));
                PlaceBall(PointerAngle + 360f * ballTurns * (1f - ease), Mathf.Lerp(rimRadius, pocketRadius, drop));
                yield return null;
            }
            rotorAngle = Mathf.Repeat(end, 360f);
            rotor.localRotation = Quaternion.Euler(0f, rotorAngle, 0f);
            PlaceBall(PointerAngle, pocketRadius);
            Spinning = false;
            onDone?.Invoke();
        }

        void PlaceBall(float angle, float radius)
        {
            float a = angle * Mathf.Deg2Rad;
            ball.localPosition = new Vector3(Mathf.Sin(a) * radius, BallHeight, Mathf.Cos(a) * radius);
        }
    }
}
