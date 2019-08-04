using UnityEngine;

namespace BoidsOOP
{
    /// <summary>
    /// Need this for multithreading
    /// </summary>
    public abstract class BoidInterestPos : MonoBehaviour
    {
        protected Vector3 pos = Vector3.zero;

        public abstract Vector3 Pos { get; set; }

        protected void Start()
        {
            pos = transform.position;
        }

        protected void Update()
        {
            pos = transform.position;
        }
    }

    public class BoidTarget : BoidInterestPos
    {
        public override Vector3 Pos
        {
            get { return pos; }
            set { pos = value; }
        }
    }
}