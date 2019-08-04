using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class SpawnRandomInSphereSystem : ComponentSystem
    {
        struct SpawnRandomInSphereInstance
        {
            public int spawnerIndex;
            public Entity sourceEntity;
            public float3 position;
        }

        ComponentGroup m_MainGroup;

        protected override void OnCreateManager()
        {
            m_MainGroup = GetComponentGroup(
                ComponentType.ReadOnly<SpawnRandomInSphere>(),
                ComponentType.ReadOnly<LocalToWorld>());
        }

        // This is (most times) a trigger: see https://forum.unity.com/threads/onupdate-method-in-componentsystems.541647/ for when it's called
        protected override void OnUpdate()
        {
            var uniqueTypes = new List<SpawnRandomInSphere>(10);

            // We have (by default) 2 BoidFishSpawners in the scene (if not modified). These have 3 attributes. If both spawners have EXACTLY the same values (whether its reference or value type)
            // they will be "fused" in uniqueTypes.Count, so uniqueTypes.Count will be 2, otherwise it'll be 3. The reason is that the first seems to be a default one (see link below)
            EntityManager.GetAllUniqueSharedComponentData(uniqueTypes);

            int spawnInstanceCount = 0;

            // https://forum.unity.com/threads/question-about-getalluniquesharedcomponentdata.545945/
            // Since the 0 is the default, why not start at 1, does not make any difference since when filtering by the 0 uniqueType, it's being ignored
            for (int sharedIndex = 0 /* 1 */ ; sharedIndex != uniqueTypes.Count; sharedIndex++)
            {
                var spawner = uniqueTypes[sharedIndex];

                // this is filtering the "groups of instances" that have the same values
                m_MainGroup.SetFilter(spawner);

                // we're counting them
                var entityCount = m_MainGroup.CalculateLength();

                // so this is the overall number of instances wether or not they have the same values
                spawnInstanceCount += entityCount;
            }

            if (spawnInstanceCount == 0)
                return;

            var spawnInstances = new NativeArray<SpawnRandomInSphereInstance>(spawnInstanceCount, Allocator.Temp);
            {
                int spawnIndex = 0;
                for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
                {
                    var spawner = uniqueTypes[sharedIndex];
                    m_MainGroup.SetFilter(spawner);

                    // this would never be 0 if the previous loop started at 1 I guess
                    if (m_MainGroup.CalculateLength() == 0)
                        continue;

                    // 1+1 entities (if any value differs, and it is the default scene, 2 spawners overall)
                    var entities = m_MainGroup.ToEntityArray(Allocator.TempJob);

                    var localToWorld = m_MainGroup.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

                    // convenient way of storing the 2 (if default) spawners info
                    for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                    {
                        var spawnInstance = new SpawnRandomInSphereInstance();

                        spawnInstance.sourceEntity = entities[entityIndex];
                        spawnInstance.spawnerIndex = sharedIndex;
                        spawnInstance.position = localToWorld[entityIndex].Position;

                        spawnInstances[spawnIndex] = spawnInstance;
                        spawnIndex++;
                    }

                    entities.Dispose();
                    localToWorld.Dispose();
                }

                // for more info about ISharedComponentData see: https://docs.unity3d.com/Packages/com.unity.entities@0.0/manual/shared_component_data.html
            }

            // now for every spawner
            for (int spawnIndex = 0; spawnIndex < spawnInstances.Length; spawnIndex++)
            {
                int spawnerIndex = spawnInstances[spawnIndex].spawnerIndex;
                var spawner = uniqueTypes[spawnerIndex];
                int count = spawner.count;
                var entities = new NativeArray<Entity>(count, Allocator.Temp);
                var prefab = spawner.prefab;
                float radius = spawner.radius;
                var spawnPositions = new NativeArray<float3>(count, Allocator.TempJob);
                float3 center = spawnInstances[spawnIndex].position;
                var sourceEntity = spawnInstances[spawnIndex].sourceEntity;

                // prepare the positions to spawn the fishes
                GeneratePoints.RandomPointsInUnitSphere(spawnPositions);

                EntityManager.Instantiate(prefab, entities);

                for (int i = 0; i < count; i++)
                {
                    // set the fishes entities data
                    EntityManager.SetComponentData(entities[i], new LocalToWorld
                    {
                        Value = float4x4.TRS(
                            center + (spawnPositions[i] * radius),
                            quaternion.LookRotationSafe(spawnPositions[i], math.up()),
                            new float3(1.0f, 1.0f, 1.0f))
                    });
                }

                // guess since the system does not need it anymore, we can just get rid of it
                EntityManager.RemoveComponent<SpawnRandomInSphere>(sourceEntity);

                spawnPositions.Dispose();
                entities.Dispose();
            }
            spawnInstances.Dispose();
        }
    }
}