using UnityEngine;
using UnityEngine.Rendering;

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
        public int numBoids = 8192;
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
        private const int ThreadGroupX = 256;
        private static readonly Vector3 GraphicsIndirectBounds = new Vector3(1000.0f, 1000.0f, 1000.0f);

        private static readonly int DtId = Shader.PropertyToID("dt");
        private static readonly int SeparationWeightId = Shader.PropertyToID("separationWeight");
        private static readonly int AlignmentWeightId = Shader.PropertyToID("alignmentWeight");
        private static readonly int TargetWeightId = Shader.PropertyToID("targetWeight");
        private static readonly int ObstacleAversionDistanceId = Shader.PropertyToID("obstacleAversionDistance");
        private static readonly int MoveSpeedId = Shader.PropertyToID("moveSpeed");

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
            if (numBoids <= 0)
                return;

            SpawnBoids();
            CreateCells();
            SetupCompute();
        }

        private void Update()
        {
            if (numBoids <= 0)
                return;

            cellsBuffer.SetData(cells);
            BufferUpdateObstaclesAndTargetsNewPos();
            UpdateCBufferParams();

            int threadsX = numBoids / ThreadGroupX;
            int rest = numBoids % ThreadGroupX;

            threadsX = rest == 0 ? threadsX : threadsX + 1;

            computeShader.Dispatch(computeCellsKernel, threadsX, 1, 1);
            computeShader.Dispatch(computeBoidsKernel, threadsX, 1, 1);

            // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html
            Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMat, new Bounds(transform.position, GraphicsIndirectBounds), argsBuffer, 0, null, ShadowCastingMode.Off, false);
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
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)numBoids;
            args[2] = (uint)boidMesh.GetIndexStart(0);
            args[3] = (uint)boidMesh.GetBaseVertex(0);
            args[4] = (uint)0;

            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            cellsBuffer = new ComputeBuffer(numBoids, CellStructSize);
            cellsBuffer.SetData(cells);

            boidBuffer = new ComputeBuffer(numBoids, BoidStructSize);
            boidBuffer.SetData(boids);

            targetsBuffer = new ComputeBuffer(targets.Length, Vector3StructSize);
            obstaclesBuffer = new ComputeBuffer(obstacles.Length, Vector3StructSize);
            BufferUpdateObstaclesAndTargetsNewPos();

            computeBoidsKernel = computeShader.FindKernel("ComputeBoids");
            computeCellsKernel = computeShader.FindKernel("ComputeCells");

            computeShader.SetBuffer(computeCellsKernel, "boidBuffer", boidBuffer);
            computeShader.SetBuffer(computeCellsKernel, "cellsBuffer", cellsBuffer);

            computeShader.SetBuffer(computeBoidsKernel, "boidBuffer", boidBuffer);
            computeShader.SetBuffer(computeBoidsKernel, "cellsBuffer", cellsBuffer);
            computeShader.SetBuffer(computeBoidsKernel, "targetsBuffer", targetsBuffer);
            computeShader.SetBuffer(computeBoidsKernel, "obstaclesBuffer", obstaclesBuffer);

            boidMat.SetBuffer("boidBuffer", boidBuffer);

            computeShader.SetInt("totalBoids", numBoids);
            computeShader.SetInt("totalTargets", targets.Length);
            computeShader.SetInt("totalObstacles", obstacles.Length);

            UpdateCBufferParams();
        }

        /// <summary>
        /// Update the position of the targets and obstacles so that the buffer in the GPU uses the
        /// correct data
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

        /// <summary>
        /// Update the constant buffer parameters from the compute shader
        /// </summary>
        private void UpdateCBufferParams()
        {
            computeShader.SetFloat(DtId, Time.deltaTime);
            computeShader.SetFloat(SeparationWeightId, separationWeight);
            computeShader.SetFloat(AlignmentWeightId, alignmentWeight);
            computeShader.SetFloat(TargetWeightId, targetWeight);
            computeShader.SetFloat(ObstacleAversionDistanceId, obstacleAversionDistance);
            computeShader.SetFloat(MoveSpeedId, moveSpeed);
        }

        #endregion
    }
}