using Impostors.Structs;
using Impostors.TimeProvider;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Impostors.Managers.QueueSortingMethods
{
    public class ByScreenSizeAndTimeFromLastUpdate : IQueueSortingMethod
    {
        [BurstCompile]
        private struct Job : IJob
        {
            [ReadOnly]
            public NativeArray<ImpostorableObject> impostorableObjects;
            public NativeList<IndexAndValue> buffer;
            public NativeQueue<int> queue;
            public int maxUpdates;
            public int maxBackgroundUpdates;
            public float time;

            public struct IndexAndValue
            {
                public int index;
                public float value;

                public IndexAndValue(int index, float value)
                {
                    this.index = index;
                    this.value = value;
                }
            }
            
            public void Execute()
            {
                // adding immediate actions into queue buffer
                for (int i = 0; i < impostorableObjects.Length; i++)
                {
                    var reqAction = impostorableObjects[i].requiredAction;
                    if (reqAction == ImpostorableObject.RequiredAction.Cull ||
                        reqAction == ImpostorableObject.RequiredAction.GoToImpostorMode ||
                        reqAction == ImpostorableObject.RequiredAction.GoToNormalMode)
                    {
                        buffer.Add(new IndexAndValue(i, float.MaxValue));
                    }
                }

                // if buffer has place then add visible objects into queue
                if (buffer.Length < maxUpdates)
                {
                    Do(1, maxUpdates - buffer.Length, buffer.Length);
                }

                // if buffer has place then add invisible objects into queue
                if (buffer.Length < maxUpdates)
                {
                    int backgroundUpdates = math.min(maxBackgroundUpdates, maxUpdates - buffer.Length);
                    Do(0, backgroundUpdates, buffer.Length);
                }

                // filling queue with data from temp buffer
                for (int i = 0; i < buffer.Length; i++)
                {
                    queue.Enqueue(buffer[i].index);
                }
            }

            private void Do(int isVisible, int count, int startBufferIndex)
            {

                int smallestBuffIndex = -1;
                float smallestBuffValue = float.MinValue;
                
                // "cool" sorting algorithm that respects max capacity of result 
                for (int i = 0; i < impostorableObjects.Length; i++)
                {
                    if (impostorableObjects[i].isVisible != isVisible || impostorableObjects[i].requiredAction != ImpostorableObject.RequiredAction.UpdateImpostorTexture)
                        continue;
                    
                    float value = impostorableObjects[i].nowScreenSize * (time - impostorableObjects[i].lastUpdate.time);
                    if (buffer.Length - startBufferIndex < count)
                    {
                        buffer.Add(new IndexAndValue(i, value));
                    }
                    else
                    {
                        // if there is no smallest value
                        if (smallestBuffIndex == -1)
                        {
                            // find new smallest value
                            smallestBuffIndex = startBufferIndex;
                            smallestBuffValue = float.MaxValue;
                            for (int j = startBufferIndex; j < buffer.Length; j++)
                            {
                                if (buffer[j].value < smallestBuffValue)
                                {
                                    smallestBuffValue = buffer[j].value;
                                    smallestBuffIndex = j;
                                }
                            }
                        }

                        if (value > smallestBuffValue)
                        {
                            // replace smallest value
                            buffer[smallestBuffIndex] = new IndexAndValue(i, value);
                            smallestBuffIndex = -1;
                        }
                    }
                }
                
            }
        }

        private ITimeProvider TimeProvider { get; }

        public ByScreenSizeAndTimeFromLastUpdate(ITimeProvider timeProvider)
        {
            TimeProvider = timeProvider;
        }
        
        public JobHandle Sort(NativeArray<ImpostorableObject> impostorableObjects, NativeQueue<int> queue, int maxUpdates,
            int maxBackgroundUpdates,
            JobHandle jobHandle)
        {
            NativeList<Job.IndexAndValue> buffer = new NativeList<Job.IndexAndValue>(maxUpdates, Allocator.TempJob);
            var job = new Job()
            {
                impostorableObjects = impostorableObjects,
                buffer = buffer,
                queue = queue,
                maxUpdates = maxUpdates,
                maxBackgroundUpdates = maxBackgroundUpdates,
                time = TimeProvider.Time
            };
            jobHandle = job.Schedule(jobHandle);
            buffer.Dispose(jobHandle);
            return jobHandle;
        }
    }
}