using UnityEngine;

namespace BoidsCompute
{
    public class ComputeBoidManager : MonoBehaviour
    {
        #region Defs

        private struct Boid
        {
            public Vector3 pos;
            public Vector3 fwd;
        }

        #endregion

        #region Public Attributes

        [Header("Spawn parameters")]
        public Mesh boidMesh = null;
        public Material boidMat = null;
        public int numBoids = 15000;
        public float radius = 75.0f;
        public Transform spawnPoint = null;

        [Header("Targets & Obstacles")]
        public Transform[] targets = null;
        public Transform[] obstacles = null;

        [Header("Boid Configuration")]
        public float separationWeight = 1;
        public float alignmentWeight = 1;
        public float targetWeight = 2;
        public float obstacleAversionDistance = 30;
        public float moveSpeed = 25;

        [Header("Shader")]
        public ComputeShader computeShader = null;

        #endregion

        #region Private Attributes

        private const int BoidStructSize = 24;
        private const int Vector3StructSize = 12;

        private int kernel = -1;
        private ComputeBuffer argsBuffer = null;
        private ComputeBuffer boidBuffer = null;
        private ComputeBuffer targetsBuffer = null;
        private ComputeBuffer obstaclesBuffer = null;

        private Boid[] boids = null;
        private Vector3[] targetsPos = null;
        private Vector3[] obstaclesPos = null;
        private int frame = 0;

        #endregion

        #region MonoBehaviour Methods

        private void Start()
        {
            InitializeArrays();
            SpawnBoids();
            SetupCompute();
        }

        private void Update()
        {
            frame %= 2;
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            BufferUpdateObstaclesAndTargetsNewPos();
            computeShader.SetFloat("dt", Time.deltaTime);
            computeShader.SetInt("frame", frame);

            // sw.Start();
            // boidBuffer.GetData(boids);

            computeShader.Dispatch(kernel, (numBoids / 256) + 1, 1, 1);

            // for (int i = 0; i < 100000; i++)
            // {
            //     // some random maths
            //     Vector3 a = Vector3.up;
            //     Vector3 b = Vector3.forward;
            //     Vector3 cross = Vector3.Cross(a, b);

            //     float dot = Vector3.Dot(a, b);
            //     dot = Vector3.Dot(a, cross);
            // }

            // Debug.Log("mselapsed" + sw.ElapsedMilliseconds);

            // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html
            Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMat, new Bounds(transform.position, Vector3.one * 100.0f), argsBuffer,
                0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);

            frame++;
        }

        private void OnDestroy()
        {
            argsBuffer?.Release();
            boidBuffer?.Release();
            targetsBuffer?.Release();
            obstaclesBuffer?.Release();

            argsBuffer = null;
            boidBuffer = null;
            targetsBuffer = null;
            obstaclesBuffer = null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize arrays
        /// </summary>
        private void InitializeArrays()
        {
            boids = new Boid[numBoids];
            targetsPos = new Vector3[targets.Length];
            obstaclesPos = new Vector3[obstacles.Length];
        }

        /// <summary>
        /// Spawn the boids
        /// </summary>
        private void SpawnBoids()
        {
            for (int i = 0; i < numBoids; i++)
            {
                Vector3 pos = Random.insideUnitSphere * radius;
                pos += spawnPoint.position;

                Boid b = new Boid
                {
                    pos = pos,
                    fwd = pos
                };
                boids[i] = b;
            }
        }

        /// <summary>
        /// Set up compute shader related stuff
        /// </summary>
        private void SetupCompute()
        {
            uint[] args = new uint[5];
            args[0] = (uint) boidMesh.GetIndexCount(0); //  number of triangles in the mesh multiplied by 3
            args[1] = (uint) numBoids;
            args[2] = (uint) boidMesh.GetIndexStart(0); // last 3 are offsets
            args[3] = (uint) boidMesh.GetBaseVertex(0);
            args[4] = (uint) 0;

            // 20: 5 (args[]) x 4 (uint size)
            argsBuffer = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            boidBuffer = new ComputeBuffer(numBoids, BoidStructSize);
            boidBuffer.SetData(boids);

            targetsBuffer = new ComputeBuffer(targets.Length, Vector3StructSize);
            targetsBuffer.SetData(targetsPos);

            obstaclesBuffer = new ComputeBuffer(obstacles.Length, Vector3StructSize);
            obstaclesBuffer.SetData(obstaclesPos);

            kernel = computeShader.FindKernel("ComputeBoids");
            computeShader.SetBuffer(kernel, "boidBuffer", boidBuffer);
            computeShader.SetBuffer(kernel, "targetsBuffer", targetsBuffer);
            computeShader.SetBuffer(kernel, "obstaclesBuffer", obstaclesBuffer);

            computeShader.SetInt("totalBoids", numBoids);
            computeShader.SetInt("totalTargets", targets.Length);
            computeShader.SetInt("totalObstacles", obstacles.Length);

            computeShader.SetFloat("dt", Time.deltaTime);

            computeShader.SetFloat("separationWeight", separationWeight);
            computeShader.SetFloat("alignmentWeight", alignmentWeight);
            computeShader.SetFloat("targetWeight", targetWeight);
            computeShader.SetFloat("obstacleAversionDistance", obstacleAversionDistance);
            computeShader.SetFloat("moveSpeed", moveSpeed);

            boidMat.SetBuffer("boidBuffer", boidBuffer);

            Shader.WarmupAllShaders();
        }

        /// <summary>
        /// Update the position of the targets and obstacles so that the buffer in the GPU uses the correct data
        /// </summary>
        private void BufferUpdateObstaclesAndTargetsNewPos()
        {
            for (int i = 0; i < targets.Length; i++)
                targetsPos[i] = targets[i].position;

            for (int i = 0; i < obstacles.Length; i++)
                obstaclesPos[i] = obstacles[i].position;

            targetsBuffer.SetData(targetsPos);
            obstaclesBuffer.SetData(obstaclesPos);
        }

        #endregion
    }
}