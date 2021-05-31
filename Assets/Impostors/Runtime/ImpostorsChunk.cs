using System;
using Impostors.ImpostorsChunkMesh;
using Impostors.MemoryUsage;
using Impostors.ObjectPools;
using Impostors.Structs;
using Impostors.TimeProvider;
using Impostors.Unsafe;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Impostors
{
    [Serializable]
    public class ImpostorsChunk : IDisposable, IMemoryConsumer
    {
        private readonly CompositeRenderTexturePool _renderTexturePool;
        private readonly MaterialObjectPool _materialObjectPool;
        private readonly bool _useMipMap;

        [HideInInspector]
        [SerializeField]
        private string name;

        private NativeStack<int> _emptyPlaces;

        [SerializeField]
        private RenderTexture _renderTexture;

        [SerializeField]
        private Material _material;

        [SerializeField]
        private int _size;

        [SerializeField]
        private bool _needToRebuildMeshFlag;

        [SerializeField]
        private bool _needToClearTexture;

        private NativeArray<Impostor> _impostors;
        private NativeList<int> _instanceIdsToRemove;
        private NativeArray<bool> _needToRebuildMeshNativeArray;


        public int TextureResolution { get; }
        public int Id { get; }
        public int Capacity { get; }
        public int Count => Capacity - _emptyPlaces.Count;
        public bool IsFull => _emptyPlaces.Count == 0;
        public bool HasPlace => !IsFull;
        public bool IsEmpty => _emptyPlaces.Count == Capacity;
        public int EmptyPlacesCount => _emptyPlaces.Count;

        private static int ImpostorsChunkIdCounter;
        private ITimeProvider TimeProvider { get; }
        private IImpostorsChunkMesh ImpostorsChunkMesh { get; }

        internal bool NeedToRebuildMesh => _needToRebuildMeshFlag || _needToRebuildMeshNativeArray[0];

        private ImpostorsChunk()
        {
        }

        public ImpostorsChunk(int atlasResolution, int textureResolution, ITimeProvider timeProvider,
            CompositeRenderTexturePool renderTexturePool, MaterialObjectPool materialObjectPool)
        {
            _renderTexturePool = renderTexturePool;
            _materialObjectPool = materialObjectPool;
            TimeProvider = timeProvider;
            _size = atlasResolution / textureResolution;
            Capacity = _size * _size;

            Assert.AreEqual(math.ceilpow2(Capacity), Capacity);
            _impostors = new NativeArray<Impostor>(Capacity, Allocator.Persistent);
            _instanceIdsToRemove = new NativeList<int>(Allocator.Persistent);
            _emptyPlaces = new NativeStack<int>(Capacity, Allocator.Persistent);
            _needToRebuildMeshNativeArray = new NativeArray<bool>(1, Allocator.Persistent);
            for (int i = 0; i < Capacity; i++)
            {
                _emptyPlaces.Push(i);
            }

            TextureResolution = textureResolution;
            Id = ++ImpostorsChunkIdCounter;

            _renderTexture = renderTexturePool.Get(atlasResolution);
            _useMipMap = _renderTexture.useMipMap;
            _needToClearTexture = true;
            _material = materialObjectPool.Get();
            _material.mainTexture = _renderTexture;
            // setting render queue to minimize overdraw effect
            _material.renderQueue = 2470 - InversePowerOfTwo(textureResolution);

            //ImpostorsChunkMesh = new DefaultImpostorsChunkMesh(_impostors, this);
            ImpostorsChunkMesh = new BufferedImpostorsChunkMesh(Capacity, _impostors);
            var mesh = ImpostorsChunkMesh.GetMesh();
            string tName = $"Chunk#{Id} {textureResolution}";
            mesh.name = tName;
            name = tName;
        }


        public int GetPlace(ImpostorableObject impostorableObject)
        {
            if (!HasPlace)
                throw new Exception("No place in chunk.");

            int index = _emptyPlaces.Pop();

            var impostor = _impostors[index];
            impostor.isRelevant = true;
            impostor.impostorLODGroupInstanceId = impostorableObject.impostorLODGroupInstanceId;

            impostor.position = impostorableObject.data.position;
            impostor.direction = impostorableObject.nowDirection;
            impostor.quadSize = impostorableObject.data.quadSize;
            impostor.zOffset = impostorableObject.data.zOffset;

            var requiredAction = impostorableObject.requiredAction;
            switch (requiredAction)
            {
                case ImpostorableObject.RequiredAction.UpdateImpostorTexture:
                    impostor.fadeTime = impostorableObject.settings.fadeTransitionTime;
                    break;
                case ImpostorableObject.RequiredAction.GoToImpostorMode:
                    impostor.fadeTime = impostorableObject.settings.fadeInTime;
                    break;
                default:
                    impostor.fadeTime = impostorableObject.settings.fadeInTime;
                    Debug.LogError($"Unexpected required action: '{requiredAction.ToString()}'");
                    break;
            }

            impostor.uv = GetUV(index);
            impostor.time = TimeProvider.Time + impostor.fadeTime - TimeProvider.DeltaTime;

            _impostors[index] = impostor;

            _needToRebuildMeshFlag = true;
            return index;
        }

        public void MarkPlaceAsNotRelevant(int place, float fadeTime, bool isLettingFadeInFirst)
        {
            var impostor = _impostors[place];
            impostor.isRelevant = false;
            if (!isLettingFadeInFirst)
                impostor.time = -(TimeProvider.Time + fadeTime);
            impostor.fadeTime = fadeTime;
            _impostors[place] = impostor;
            _needToRebuildMeshFlag = true;
        }

        public JobHandle ScheduleUpdateImpostors(JobHandle jobHandle)
        {
            int impostorsLength = _impostors.Length;
            if (_instanceIdsToRemove.Length > 0)
            {
                var jobRemoveImpostors = new RemoveAllImpostorsWithImpostorLODGroupInstanceIdJob()
                {
                    impostors = _impostors,
                    queue = _emptyPlaces.AsParallelWriter(),
                    impostorLodGroupInstanceIds = _instanceIdsToRemove
                };
                jobHandle = jobRemoveImpostors.Schedule(impostorsLength, 32, jobHandle);

                var jobClearInstanceIdsList = new ClearInstanceIdsJob() {list = _instanceIdsToRemove};
                jobHandle = jobClearInstanceIdsList.Schedule(jobHandle);
            }
            var jobUpdateImpostors = new UpdateImpostorsJob()
            {
                impostors = _impostors,
                queue = _emptyPlaces.AsParallelWriter(),
                time = TimeProvider.Time,
                needRebuildMesh = _needToRebuildMeshNativeArray
            };
            jobHandle = jobUpdateImpostors.Schedule(impostorsLength, Capacity, jobHandle);
            return jobHandle;
        }

        public JobHandle ScheduleMeshCreation(JobHandle dependsOn)
        {
            if (!NeedToRebuildMesh)
                throw new Exception("Wrong state");
            _needToRebuildMeshFlag = false;
            _needToRebuildMeshNativeArray[0] = false;
            var jobHandle = ImpostorsChunkMesh.ScheduleMeshCreation(dependsOn);
            return jobHandle;
        }

        public Mesh GetMesh()
        {
            var mesh = ImpostorsChunkMesh.GetMesh();
            return mesh;
        }

        public void RemoveAllImpostorsWithImpostorLODGroupInstanceId(int impostorLodGroupInstanceId)
        {
            if (impostorLodGroupInstanceId == 0)
                throw new ArgumentOutOfRangeException(nameof(impostorLodGroupInstanceId), "Value must be not zero.");
            _instanceIdsToRemove.Add(impostorLodGroupInstanceId);
            _needToRebuildMeshFlag = true;
        }

        public void BeginRendering(CommandBuffer cb)
        {
            cb.SetRenderTarget(_renderTexture);
            if (_needToClearTexture)
            {
                cb.SetViewport(new Rect(0, 0, _renderTexture.width, _renderTexture.height));
                cb.ClearRenderTarget(true, true, Color.clear);
                _needToClearTexture = false;
            }
        }

        public void AddCommandBufferCommands(int placeInChunk, CommandBuffer cb)
        {
            cb.SetViewport(GetPixelRect(placeInChunk));
        }

        public void EndRendering(CommandBuffer cb)
        {
            if (_useMipMap)
                cb.GenerateMips(_renderTexture);
        }

        private Rect GetPixelRect(int placeInChunk)
        {
            int x = placeInChunk / _size * TextureResolution;
            int y = placeInChunk % _size * TextureResolution;
            return new Rect(x, y, TextureResolution, TextureResolution);
        }

        private Vector4 GetUV(int placeInChunk)
        {
            int x = placeInChunk / _size;
            int y = placeInChunk % _size;
            return new Vector4(x, y, x + 1, y + 1) / _size;
        }

        public Material GetMaterial()
        {
            return _material;
        }

        [BurstCompile]
        private struct UpdateImpostorsJob : IJobParallelFor
        {
            public NativeArray<Impostor> impostors;

            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeStack<int>.ParallelWriter queue;

            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<bool> needRebuildMesh;

            public float time;

            public void Execute(int index)
            {
                var impostor = impostors[index];
                if (!impostor.Exists || impostor.isRelevant)
                    return;

                // if impostor is only fading-in and is not ready to fade-out
                if (impostor.time > time)
                {
                    return;
                }

                // when impostor is fully faded-in -> start fade-out
                if (impostor.time > 0)
                {
                    impostor.time = -(time + impostor.fadeTime);
                    impostors[index] = impostor;
                    needRebuildMesh[0] = true;
                    return;
                }

                // when impostor is fully faded-out -> delete it
                if (impostor.time > -time)
                {
                    impostor.impostorLODGroupInstanceId = 0;
                    impostors[index] = impostor;
                    queue.Push(index);
                    needRebuildMesh[0] = true;
                }
            }
        }

        [BurstCompile]
        private struct RemoveAllImpostorsWithImpostorLODGroupInstanceIdJob : IJobParallelFor
        {
            public NativeArray<Impostor> impostors;

            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeStack<int>.ParallelWriter queue;

            [ReadOnly]
            public NativeList<int> impostorLodGroupInstanceIds;

            public void Execute(int index)
            {
                var impostor = impostors[index];
                for (int i = 0; i < impostorLodGroupInstanceIds.Length; i++)
                {
                    if (impostor.impostorLODGroupInstanceId == impostorLodGroupInstanceIds[i])
                    {
                        impostor.impostorLODGroupInstanceId = 0;
                        impostors[index] = impostor;
                        queue.Push(index);
                        break;
                    }
                }
            }
        }

        [BurstCompile]
        private struct ClearInstanceIdsJob : IJob
        {
            public NativeList<int> list;

            public void Execute()
            {
                list.Clear();
            }
        }

        public void Dispose()
        {
            _impostors.Dispose();
            _instanceIdsToRemove.Dispose();
            _emptyPlaces.Dispose();
            _needToRebuildMeshNativeArray.Dispose();
            _renderTexturePool.Return(_renderTexture);
            _materialObjectPool.Return(_material);
            _renderTexture = null;
            _material = null;
            ImpostorsChunkMesh.Dispose();
        }

        public int GetUsedBytes()
        {
            var res = 0;

            res += MemoryUsageUtility.GetMemoryUsage(_impostors);
            res += MemoryUsageUtility.GetMemoryUsage(_emptyPlaces);
            res += MemoryUsageUtility.GetMemoryUsage(_instanceIdsToRemove);

            return res;
        }

        // returns the power of desired value. 32 -> 5, 512 -> 9
        private static int InversePowerOfTwo(int value)
        {
            int power = 0;
            while (value > 1)
            {
                power++;
                value = value >> 1;
            }

            return power;
        }
    }
}