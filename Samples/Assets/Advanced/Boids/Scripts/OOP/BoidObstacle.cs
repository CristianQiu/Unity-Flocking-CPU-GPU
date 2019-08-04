using UnityEngine;

namespace BoidsOOP
{
    public class BoidObstacle : BoidInterestPos
    {
        public override Vector3 Pos
        {
            get { return pos; }
            set { pos = value; }
        }
    }
}