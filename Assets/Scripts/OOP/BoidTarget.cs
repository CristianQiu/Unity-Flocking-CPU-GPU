using UnityEngine;

namespace BoidsOOP
{
    public abstract class BoidInterestPos : MonoBehaviour
    {
        protected Vector3 pos = Vector3.zero;

        public Vector3 Pos
        {
            get { return pos; }
            set { pos = value; }
        }

        protected virtual void Start()
        {
            pos = transform.position;
        }

        protected virtual void Update()
        {
            // need this for multithreading
            pos = transform.position;
        }
    }

    public class BoidTarget : BoidInterestPos
    {
    }
}