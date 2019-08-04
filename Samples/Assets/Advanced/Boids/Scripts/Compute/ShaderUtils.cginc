#define MAX_FLOAT 3.402823466e+38

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