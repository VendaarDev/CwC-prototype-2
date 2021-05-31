using Impostors.ImpostorsChunkMesh;
using Impostors.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Impostors.Jobs
{
    [BurstCompile]
    public struct CalculateCurrentImpostorsSettings : IJobParallelFor
    {
        public NativeArray<ImpostorableObject> impostors;
        public float3 cameraPosition;
        public float multiplier;

        public void Execute(int index)
        {
            ImpostorableObject impostorableObject = impostors[index];
            impostorableObject.nowDirection = impostorableObject.data.position - cameraPosition;
            impostorableObject.nowDistance = math.length(impostorableObject.nowDirection);
            impostorableObject.nowScreenSize = impostorableObject.data.quadSize / (impostorableObject.nowDistance * multiplier);
            impostors[index] = impostorableObject;
        }
    }
}