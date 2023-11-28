using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct UpdateBlockPositionsJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<Vector3> trackSplinePoints;
    [ReadOnly] public NativeArray<BlockData> blocksData;
    [ReadOnly] public float currentPercentage;

    public void Execute(int index, TransformAccess transform)
    {
        float finalPercentage = blocksData[index].endPercentage;
        float currentBlockPercentage = Mathf.InverseLerp(finalPercentage - 0.5f, finalPercentage, currentPercentage);

        float currentBlockPositionPercentage =
            Mathf.Lerp(finalPercentage - 0.075f, finalPercentage, currentBlockPercentage);

        float lerp = math.lerp(0, trackSplinePoints.Length - 4, currentBlockPositionPercentage);
        int u = (int)lerp;
        float inter = lerp % 1;

        Vector3 position = Vector3.Lerp(trackSplinePoints[u], trackSplinePoints[u + 1], inter) + Vector3.forward * blocksData[index].zPosition;
        Quaternion rotation = Quaternion.LookRotation(trackSplinePoints[u + 1] - trackSplinePoints[u], Vector3.up);

        transform.position = position;
        transform.rotation = rotation;
    }
}
