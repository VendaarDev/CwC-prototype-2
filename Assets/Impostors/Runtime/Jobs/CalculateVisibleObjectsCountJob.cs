using Impostors.Structs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Impostors.Jobs
{
    public struct CalculateVisibleObjectsCountJob : IJob
    {
        public NativeArray<ImpostorableObject> impostors;

        public void Execute()
        {
            int visibleCount = 0;
            int needUpdateCount = 0;
            for (int i = 0; i < impostors.Length; i++)
            {
                if (impostors[i].isVisible != 0)
                    visibleCount++;
                if (impostors[i].requiredAction == ImpostorableObject.RequiredAction.UpdateImpostorTexture)
                    needUpdateCount++;
            }

            Debug.Log($"Visible count: {visibleCount}, Need update count: {needUpdateCount}");
        }
    }
}