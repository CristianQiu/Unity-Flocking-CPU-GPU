using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BoidsOOP
{
    public class BoidManager : MonoBehaviour
    {
        #region Defs

        public enum RunMode
        {
            SingleThread,
            MultiThread,
        }

        #endregion

        #region Public Attributes

        public static int ConcurrencyLevel = 8;

        [Header("Spawn params")]
        public Transform spawnPoint = null;
        public int numBoidsSpawned;
        public float radius = 75.0f;
        public Mesh boidMesh = null;
        public Material boidMat = null;

        [Header("Boid params")]
        public float separationWeight = 1.0f;
        public float alignmentWeight = 1.0f;
        public float targetWeight = 2.0f;
        public float obstacleAversionDistance = 30.0f;
        public float moveSpeed = 25.0f;

        [Header("Boid interests")]
        public BoidObstacle[] obstacles = null;
        public BoidTarget[] targets = null;

        #endregion

        #region Private Attributes

        private const int MaxBatchSize = 1023;

        private RunMode runMode = RunMode.SingleThread;

        private List<Boid> boidsList = new List<Boid>();
        private DictionaryOfLists<Boid> boidsDict = new DictionaryOfLists<Boid>();

        private List<Matrix4x4[]> matrices = null;
        private ConcurrentDictOfLists<Boid> boidsDictConcurrent = null;
        private ParallelOptions parallelOpts = new ParallelOptions();

        private Action<Boid> parallelAddToDictFunc = null;
        private Action<KeyValuePair<int, List<Boid>>> parallelSteeringFunc = null;

        private float dt = 0.0f;

        #endregion

        #region MonoBehaviour Methods

        private void Start()
        {
            if (BenchmarkSystem.Instance.IsBenchmarkRunning)
            {
                numBoidsSpawned = BenchmarkSystem.Instance.numberOfBoids;
                SceneBenchmarkType sbt = BenchmarkSystem.Instance.CurrRunningBenchmark.SceneBenchType;
                runMode = sbt == SceneBenchmarkType.OOPMT ? RunMode.MultiThread : RunMode.SingleThread;
            }

            CreateMatrices();
            SpawnBoids();
            parallelAddToDictFunc = ParallelAddToDict;
            parallelSteeringFunc = ParallelSteering;

            ConcurrencyLevel = SystemInfo.processorCount;
            boidsDictConcurrent = new ConcurrentDictOfLists<Boid>(ConcurrencyLevel);
            parallelOpts.MaxDegreeOfParallelism = ConcurrencyLevel;
            Debug.Log("System has " + SystemInfo.processorCount + " hardware threads, setting concurrency level...");
        }

        private void Update()
        {
            dt = Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.F1))
                runMode = runMode == RunMode.SingleThread ? RunMode.MultiThread : RunMode.SingleThread;

            switch (runMode)
            {
                case RunMode.SingleThread:
                    UpdateBoidsSingleThread();
                    break;
                case RunMode.MultiThread:
                    UpdateBoidsMultithread();
                    break;
                default:
                    break;
            }

            for (int i = 0; i < matrices.Count; i++)
                Graphics.DrawMeshInstanced(boidMesh, 0, boidMat, matrices[i], matrices[i].Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }

        private void OnGUI()
        {
            if (BenchmarkSystem.Instance.IsBenchmarkRunning)
                return;

            switch (runMode)
            {
                case RunMode.SingleThread:
                    GUI.Label(new Rect(20.0f, 25.0f, 200.0f, 120.0f), "SingleThread - F1 to switch");
                    break;
                case RunMode.MultiThread:
                    GUI.Label(new Rect(20.0f, 25.0f, 200.0f, 120.0f), "MultiThread - F1 to switch");
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Methods

        /*

        Other approach idea, but a bit too different to compare it directly to the ECS version IMO.
        Current implementation is as close as I can get to replicate the algorithms in OOP.

        First frame:
        Have a dictionary for the hashed cells (could even implement my own like in Compute).
        Iterate over the boids: calculate the hash and start accumulating the cell information in the dictionary (pos, fwd, number of boids).

        Other frames:
        Iterate over the boids: get the hash again, read the dictionary cell data and compute flocking.
        Hash again each boid at the same time (after) the new position is computed, accumulating the cell data.
        Use a second dictionary and alternate each frame to use one or another.
        Clear the one no longer needed at the end of the frame.

        If I implemented my own dictionary I could clear it on the fly (still need two): each time a boid reads a cell,
        decrease a replica of the number of boids inside (other boids may still be reading)
        when it reaches 0 no other boid needs it, so just reset pos and fwd.

        I could add a list with the indices that are occuppied, but this is probably not worth too much or even at all.
        If reading linearly is (say x3 x4) faster then I can have 3 or 4 spots empty and still be as fast as directly accessing occuppied indices.

        I'm almost 100% sure this would be faster in single thread, but the question is how multithreading would perform.

        */

        /// <summary>
        /// Create the TRS matrices
        /// </summary>
        private void CreateMatrices()
        {
            matrices = new List<Matrix4x4[]>(64);

            int numBatches = Mathf.FloorToInt((float) numBoidsSpawned / (float) MaxBatchSize);
            int rest = numBoidsSpawned - (numBatches * MaxBatchSize);

            for (int i = 0; i < numBatches; i++)
            {
                Matrix4x4[] batch = new Matrix4x4[MaxBatchSize];

                for (int j = 0; j < MaxBatchSize; j++)
                {
                    Matrix4x4 m = new Matrix4x4();
                    batch[j] = m;
                }

                matrices.Add(batch);
            }

            Matrix4x4[] batchRest = new Matrix4x4[rest];

            for (int i = 0; i < rest; i++)
            {
                Matrix4x4 m = new Matrix4x4();
                batchRest[i] = m;
            }

            matrices.Add(batchRest);
        }

        /// <summary>
        /// Spawn the boids
        /// </summary>
        private void SpawnBoids()
        {
            for (int i = 0; i < numBoidsSpawned; i++)
            {
                int outerIndex = Mathf.FloorToInt((float) i / (float) MaxBatchSize);
                int innerIndex = i % MaxBatchSize;

                Vector3 pos = UnityEngine.Random.insideUnitSphere * radius;
                pos += spawnPoint.transform.position;
                Quaternion rot = Quaternion.LookRotation(pos, Vector3.up);

                Boid b = new Boid(pos, Vector3.forward, outerIndex, innerIndex);
                boidsList.Add(b);

                matrices[outerIndex][innerIndex].SetTRS(pos, rot, Vector3.one);
            }
        }

        /// <summary>
        /// Find the nearest position of interest of the boids, from a given pos
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="fromPos"></param>
        /// <param name="nearest"></param>
        /// <param name="distance"></param>
        private void FindNearest(BoidInterestPos[] positions, Vector3 fromPos, out BoidInterestPos nearest, out float distance)
        {
            nearest = positions[0];
            distance = (nearest.Pos - fromPos).sqrMagnitude;

            for (int i = 1; i < positions.Length; i++)
            {
                float newDist = (positions[i].Pos - fromPos).sqrMagnitude;
                if (newDist < distance)
                {
                    distance = newDist;
                    nearest = positions[i];
                }
            }

            distance = Mathf.Sqrt(distance);
        }

        #endregion

        #region Single Thread Methods

        /// <summary>
        /// Update the boids system
        /// </summary>
        private void UpdateBoidsSingleThread()
        {
            StoreHashedBoidsSingleThread();
            DoSteeringSingleThread();
            boidsDict.Clear();
        }

        /// <summary>
        /// Hash the boids and store them in the dictionary of lists
        /// </summary>
        private void StoreHashedBoidsSingleThread()
        {
            for (int i = 0; i < boidsList.Count; i++)
            {
                Boid b = boidsList[i];
                boidsDict.Add(b);
            }
        }

        /// <summary>
        /// Do the boids steering
        /// </summary>
        private void DoSteeringSingleThread()
        {
            foreach (KeyValuePair<int, List<Boid>> item in boidsDict.DictLists)
            {
                List<Boid> list = item.Value;

                if (list == null || list.Count <= 0)
                    continue;

                int numBoidsInCell = list.Count;

                Vector3 cellAlignment = Vector3.zero;
                Vector3 cellSeparation = Vector3.zero;

                // each list has the boids that are within the same cell
                // Unity is using a different approach from what I previously did in my first test
                // avoiding to compute local avoidance between boids themselves is probably boosting performance significantly
                // they probable went for that to have a more "impressive" numbers on their demo...
                for (int i = 0; i < numBoidsInCell; i++)
                {
                    Boid b = list[i];
                    cellAlignment += b.Fwd;
                    cellSeparation += b.Pos;
                }

                // it seems Unity does this just finding the nearest to the first element found in a cell, if I understood correctly
                // however, I need to iterate twice over the list of boids to aproximate the code to the ECS version, I guess i can take the average pos
                Vector3 avgCellPos = cellSeparation / numBoidsInCell;

                FindNearest(obstacles, avgCellPos, out BoidInterestPos nearestObstacle, out float nearestObstacleDist);
                FindNearest(targets, avgCellPos, out BoidInterestPos nearestTarget, out float nearestTargetDist);

                for (int i = numBoidsInCell - 1; i >= 0; i--)
                {
                    Boid b = list[i];
                    Vector3 fwd = b.Fwd;
                    Vector3 pos = b.Pos;

                    Vector3 obstacleSteering = pos - nearestObstacle.Pos;
                    Vector3 avoidObstacleHeading = (nearestObstacle.Pos + Vector3.Normalize(obstacleSteering) * obstacleAversionDistance) - pos;
                    Vector3 targetHeading = targetWeight * Vector3.Normalize(nearestTarget.Pos - pos);
                    float nearestObstacleDistanceFromRadius = nearestObstacleDist - obstacleAversionDistance;
                    Vector3 alignmentResult = alignmentWeight * Vector3.Normalize((cellAlignment / numBoidsInCell) - fwd);
                    Vector3 separationResult = separationWeight * Vector3.Normalize((pos * numBoidsInCell) - cellSeparation);

                    Vector3 normalHeading = Vector3.Normalize(alignmentResult + separationResult + targetHeading);
                    Vector3 targetForward = nearestObstacleDistanceFromRadius < 0.0 ? avoidObstacleHeading : normalHeading;
                    Vector3 nextHeading = Vector3.Normalize(fwd + dt * (targetForward - fwd));

                    Vector3 nextPos = pos + (nextHeading * (moveSpeed * dt));

                    b.Pos = nextPos;
                    b.Fwd = nextHeading;

                    Quaternion rot = Quaternion.LookRotation(nextHeading, Vector3.up);
                    matrices[b.OuterBatchesIndex][b.InnerBatchesIndex].SetTRS(nextPos, rot, Vector3.one);

                    list.RemoveAt(i);
                }

                boidsDict.PushList(list);
            }
        }

        #endregion

        #region Multithread Methods

        /// <summary>
        /// Update the boids using multithreading
        /// </summary>
        private void UpdateBoidsMultithread()
        {
            StoreHashedBoidsMultithread();
            DoSteeringMultithread();
            boidsDictConcurrent.Clear();
        }

        /// <summary>
        /// Function that will be executed in parallel to add the boids to the dictionary
        /// </summary>
        /// <param name="b"></param>
        private void ParallelAddToDict(Boid b)
        {
            boidsDictConcurrent.Add(b);
        }

        /// <summary>
        /// Hash the boids and store them in the dictionary of lists
        /// </summary>
        private void StoreHashedBoidsMultithread()
        {
            Parallel.ForEach(boidsList, parallelOpts, parallelAddToDictFunc);
        }

        /// <summary>
        /// Function that will be executed in parallel to calculate steerings
        /// </summary>
        /// <param name="item"></param>
        private void ParallelSteering(KeyValuePair<int, List<Boid>> item)
        {
            List<Boid> list = item.Value;

            if (list == null || list.Count <= 0)
                return;

            int numBoidsInCell = list.Count;

            Vector3 cellAlignment = Vector3.zero;
            Vector3 cellSeparation = Vector3.zero;

            for (int i = 0; i < numBoidsInCell; i++)
            {
                Boid b = list[i];
                cellAlignment += b.Fwd;
                cellSeparation += b.Pos;
            }

            Vector3 avgCellPos = cellSeparation / numBoidsInCell;

            FindNearest(obstacles, avgCellPos, out BoidInterestPos nearestObstacle, out float nearestObstacleDist);
            FindNearest(targets, avgCellPos, out BoidInterestPos nearestTarget, out float nearestTargetDist);

            for (int i = numBoidsInCell - 1; i >= 0; i--)
            {
                Boid b = list[i];
                Vector3 fwd = b.Fwd;
                Vector3 pos = b.Pos;

                Vector3 obstacleSteering = pos - nearestObstacle.Pos;
                Vector3 avoidObstacleHeading = (nearestObstacle.Pos + Vector3.Normalize(obstacleSteering) * obstacleAversionDistance) - pos;
                Vector3 targetHeading = targetWeight * Vector3.Normalize(nearestTarget.Pos - pos);
                float nearestObstacleDistanceFromRadius = nearestObstacleDist - obstacleAversionDistance;
                Vector3 alignmentResult = alignmentWeight * Vector3.Normalize((cellAlignment / numBoidsInCell) - fwd);
                Vector3 separationResult = separationWeight * Vector3.Normalize((pos * numBoidsInCell) - cellSeparation);

                Vector3 normalHeading = Vector3.Normalize(alignmentResult + separationResult + targetHeading);
                Vector3 targetForward = nearestObstacleDistanceFromRadius < 0.0 ? avoidObstacleHeading : normalHeading;

                Vector3 nextHeading = Vector3.Normalize(fwd + dt * (targetForward - fwd));
                Vector3 nextPos = pos + (nextHeading * (moveSpeed * dt));

                b.Fwd = nextHeading;
                b.Pos = nextPos;

                Quaternion rot = Quaternion.LookRotation(nextHeading, Vector3.up);
                matrices[b.OuterBatchesIndex][b.InnerBatchesIndex].SetTRS(nextPos, rot, Vector3.one);

                list.RemoveAt(i);
            }

            boidsDictConcurrent.PushList(list);
        }

        /// <summary>
        /// Calculate steerings using multithreading
        /// </summary>
        private void DoSteeringMultithread()
        {
            Parallel.ForEach(boidsDictConcurrent.DictLists, parallelOpts, parallelSteeringFunc);
        }

        #endregion
    }
}