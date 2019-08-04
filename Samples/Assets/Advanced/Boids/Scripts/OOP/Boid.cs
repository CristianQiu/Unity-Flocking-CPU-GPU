using UnityEngine;
using Unity.Mathematics;

namespace BoidsOOP
{
    public class Boid : IHasheable
    {
        #region Private attributes

        private static float CellRadius = 8.0f;

        private int outerBatchesIndex = -1;
        private int innerBatchIndex = -1;

        private Vector3 pos;
        private Vector3 fwd;

        #endregion

        #region Properties

        public int OuterBatchesIndex
        {
            get { return outerBatchesIndex; }
            set { outerBatchesIndex = value; }
        }
        public int InnerBatchesIndex
        {
            get { return innerBatchIndex; }
            set { innerBatchIndex = value; }
        }
        public Vector3 Pos
        {
            get { return pos; }
            set { pos = value; }
        }

        public Vector3 Fwd
        {
            get { return fwd; }
            set { fwd = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="fwd"></param>
        /// <param name="outerBatchesIndex"></param>
        /// <param name="innerBatchIndex"></param>
        public Boid(Vector3 pos, Vector3 fwd, int outerBatchesIndex, int innerBatchIndex)
        {
            this.pos = pos;
            this.fwd = fwd;
            this.outerBatchesIndex = outerBatchesIndex;
            this.innerBatchIndex = innerBatchIndex;
        }

        /// <summary>
        /// Interface method to provide a hash
        /// </summary>
        /// <returns></returns>
        public int Hash()
        {
            Vector3 pos = this.pos / CellRadius;
            float3 floatPos = new float3(pos.x, pos.y, pos.z);

            // https://github.com/Unity-Technologies/Unity.Mathematics/blob/master/src/Unity.Mathematics/int3.gen.cs
            // implementation at line 1478
            // found this too, older? https://forum.unity.com/threads/question-about-mike-actons-boids-example.586963/

            // this is from the mathematics unity library, I do not know how they do the hash atm so just let's use the same
            int hash = (int) math.hash(new int3(math.floor(floatPos)));

            return hash;
        }

        #endregion
    }
}