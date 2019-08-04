using System;
using UnityEngine;
using Unity.Entities;

[Serializable]
public struct SpawnRandomInSphere : ISharedComponentData
{
    public GameObject prefab;
    public float radius;
    public int count;
}

public class SpawnRandomInSphereProxy : SharedComponentDataProxy<SpawnRandomInSphere> {}