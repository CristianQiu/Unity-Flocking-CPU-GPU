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

        private struct Cell
        {
            public int boidsInside;
            public Vector3Int pos;
            public Vector3Int fwd;
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
        private const int CellStructSize = 28;
        private const int Vector3StructSize = 12;

        private static readonly int dtId = Shader.PropertyToID("dt");

        private int computeBoidsKernel = -1;
        private int computeCellsKernel = -1;

        private ComputeBuffer argsBuffer = null;
        private ComputeBuffer boidBuffer = null;
        private ComputeBuffer cellsBuffer = null;
        private ComputeBuffer targetsBuffer = null;
        private ComputeBuffer obstaclesBuffer = null;

        private Boid[] boids = null;
        private Cell[] cells = null;
        private Vector3[] targetsPos = null;
        private Vector3[] obstaclesPos = null;

        #endregion

        #region MonoBehaviour Methods

        private void Start()
        {
            SpawnBoids();
            CreateCells();
            SetupCompute();
        }

        private void Update()
        {
            BufferUpdateObstaclesAndTargetsNewPos();
            computeShader.SetFloat(dtId, Time.deltaTime);
            cellsBuffer.SetData(cells);

            computeShader.SetFloat("separationWeight", separationWeight);
            computeShader.SetFloat("alignmentWeight", alignmentWeight);
            computeShader.SetFloat("targetWeight", targetWeight);
            computeShader.SetFloat("obstacleAversionDistance", obstacleAversionDistance);
            computeShader.SetFloat("moveSpeed", moveSpeed);

            // dispatches are executed sequentially and serve as a global synchronization point, which cannot be done inside a single kernel
            // only a barrier for one thread group is allowed
            computeShader.Dispatch(computeCellsKernel, (numBoids / 256) + 1, 1, 1);
            computeShader.Dispatch(computeBoidsKernel, (numBoids / 256) + 1, 1, 1);

            // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html
            Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMat, new Bounds(transform.position, Vector3.one * 150.0f), argsBuffer,
                0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }

        private void OnDestroy()
        {
            argsBuffer?.Release();
            boidBuffer?.Release();
            cellsBuffer?.Release();
            targetsBuffer?.Release();
            obstaclesBuffer?.Release();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Spawn the boids
        /// </summary>
        private void SpawnBoids()
        {
            boids = new Boid[numBoids];

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
        /// Create the cells array
        /// </summary>
        private void CreateCells()
        {
            cells = new Cell[numBoids];

            for (int i = 0; i < numBoids; i++)
            {
                cells[i] = new Cell
                {
                    boidsInside = 0,
                    pos = Vector3Int.zero,
                    fwd = Vector3Int.zero,
                };
            }
        }

        /// <summary>
        /// Set up compute shader related stuff
        /// </summary>
        private void SetupCompute()
        {
            obstaclesPos = new Vector3[obstacles.Length];
            targetsPos = new Vector3[targets.Length];

            uint[] args = new uint[5];
            args[0] = (uint) boidMesh.GetIndexCount(0); //  number of triangles in the mesh multiplied by 3
            args[1] = (uint) numBoids;
            args[2] = (uint) boidMesh.GetIndexStart(0); // last 3 are offsets
            args[3] = (uint) boidMesh.GetBaseVertex(0);
            args[4] = (uint) 0;

            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            cellsBuffer = new ComputeBuffer(numBoids, CellStructSize);
            cellsBuffer.SetData(cells);

            boidBuffer = new ComputeBuffer(numBoids, BoidStructSize);
            boidBuffer.SetData(boids);

            targetsBuffer = new ComputeBuffer(targets.Length, Vector3StructSize);
            obstaclesBuffer = new ComputeBuffer(obstacles.Length, Vector3StructSize);
            BufferUpdateObstaclesAndTargetsNewPos();

            computeCellsKernel = computeShader.FindKernel("ComputeCells");
            computeBoidsKernel = computeShader.FindKernel("ComputeBoids");

            computeShader.SetBuffer(computeCellsKernel, "boidBuffer", boidBuffer);
            computeShader.SetBuffer(computeCellsKernel, "cellsBuffer", cellsBuffer);

            computeShader.SetBuffer(computeBoidsKernel, "boidBuffer", boidBuffer);
            computeShader.SetBuffer(computeBoidsKernel, "cellsBuffer", cellsBuffer);
            computeShader.SetBuffer(computeBoidsKernel, "targetsBuffer", targetsBuffer);
            computeShader.SetBuffer(computeBoidsKernel, "obstaclesBuffer", obstaclesBuffer);

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