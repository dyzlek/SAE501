using UnityEngine;

namespace SAE
{
    // Fait tourner un morceau sur lui-même (les hélices du MOAB et du BFB), autour d'un axe
    // exprimé dans le repère de axisRef (le modèle du dirigeable, pour suivre son grand axe).
    public class Spinner : MonoBehaviour
    {
        public float speed = 720f;          // degrés par seconde
        public Transform axisRef;
        public Vector3 localAxis = Vector3.forward;

        Vector3 localCenter;

        void Start()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) localCenter = mf.sharedMesh.bounds.center;
        }

        void Update()
        {
            var axis = axisRef ? axisRef.TransformDirection(localAxis) : transform.forward;
            transform.RotateAround(transform.TransformPoint(localCenter), axis, speed * Time.deltaTime);
        }
    }
}
