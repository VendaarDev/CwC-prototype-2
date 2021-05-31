using Impostors.Structs;
using Unity.Collections;
using Unity.Jobs;

namespace Impostors.Managers.QueueSortingMethods
{
    public interface IQueueSortingMethod
    {
        /// <summary>
        /// In your job '<paramref name="impostorableObjects"/>' array must be with [<see cref="ReadOnlyAttribute"/>] attribute
        /// </summary>
        /// <param name="impostorableObjects"></param>
        /// <param name="queue"></param>
        /// <param name="maxUpdates"></param>
        /// <param name="maxBackgroundUpdates"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        JobHandle Sort(
            NativeArray<ImpostorableObject> impostorableObjects, 
            NativeQueue<int> queue, 
            int maxUpdates,
            int maxBackgroundUpdates,
            JobHandle jobHandle);
    }
}