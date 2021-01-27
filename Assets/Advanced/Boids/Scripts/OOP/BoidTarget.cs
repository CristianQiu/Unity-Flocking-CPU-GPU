using UnityEngine;

namespace BoidsOOP
{
    /// <summary>
    /// Need this for multithreading
    /// </summary>
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
            pos = transform.position;
        }
    }

    public class BoidTarget : BoidInterestPos
    {
    }
}