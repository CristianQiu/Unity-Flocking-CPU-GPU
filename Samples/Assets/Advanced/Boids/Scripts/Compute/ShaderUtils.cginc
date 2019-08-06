#define MAX_FLOAT 3.402823466e+38

#define LARGE_PRIME1 73856093
#define LARGE_PRIME2 19349663
#define LARGE_PRIME3 83492791

float FindDistToNearestFromPos(StructuredBuffer<float3> interestPoints, uint totalInterestPoints, float3 fromPos, out float3 interestPointPos)
{
    float dist = MAX_FLOAT;

    for (uint i = 0; i < totalInterestPoints; i++)
    {
        float3 interestPoint = interestPoints[i];
        float newDist = distance(fromPos, interestPoint);

        if (newDist < dist)
        {
            interestPointPos = interestPoint;
            dist = newDist;
        }
    }

    return dist;
}

uint cellHash(float3 pos, uint cellRadius, uint numEntities)
{
    int3 floored = (int3) (floor(pos / cellRadius));

    // what about negative numbers, what's their representation and how all these mults and xors affect them
    uint hash = LARGE_PRIME1 * floored.x ^ LARGE_PRIME2 * floored.y ^ LARGE_PRIME3 * floored.z;
    hash %= numEntities;

    return hash;
}