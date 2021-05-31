using Unity.Mathematics;

namespace Impostors.Structs
{
    [System.Serializable]
    public struct ImpostorableObject
    {
        public int impostorLODGroupInstanceId;

        [System.Serializable]
        public struct ImpostorData
        {
            public float3 position;
            public float3 size;
            public float height;
            public float quadSize;
            public float zOffset;
        }

        public ImpostorData data;

        [System.Serializable]
        public struct ImpostorSettings
        {
            public byte isStatic;
            public float fadeInTime;
            public float fadeOutTime;
            public float fadeTransitionTime;
            public float deltaCameraAngle;
            public byte useUpdateByTime;
            public float timeInterval;
            public byte useDeltaLightAngle;
            public float deltaLightAngle;
            public float deltaDistance;
            public int minTextureResolution;
            public int maxTextureResolution;
            public float screenRelativeTransitionHeight;
            public float screenRelativeTransitionHeightCull;
        }

        public ImpostorSettings settings;

        /// <summary>
        /// Is object currently visible
        /// </summary>
        public byte isVisible;

        /// <summary>
        /// Indicates if imposter has been created and ready to be shown
        /// </summary>
        public bool HasImpostor => lastUpdate.chunkId != 0;

        /// <summary>
        /// Does impostor need to update its texture
        /// '-1' not set.
        ///  '0' do not need update.
        ///  '1' need to update impostor
        ///  '2' need to go in impostor mode
        ///  '3' need to gp in original mode
        /// </summary>
        public RequiredAction requiredAction;

        public enum RequiredAction
        {
            NotSet = -1,
            None = 0,
            UpdateImpostorTexture = 1,
            GoToImpostorMode = 2,
            GoToNormalMode = 3,
            Cull = 4,
        }

        public float nowScreenSize;
        public float nowDistance;
        public float3 nowDirection;

        public int ChunkId => lastUpdate.chunkId;
        public int PlaceInChunk => lastUpdate.placeInChunk;

        public void SetChunk(int chunkId, int placeInChunk)
        {
            lastUpdate.chunkId = chunkId;
            lastUpdate.placeInChunk = placeInChunk;
        }

        [System.Serializable]
        public struct LastUpdate
        {
            public int chunkId;
            public int placeInChunk;
            public float time;
            public float3 lightDirection;

            /// <summary>
            /// Direction form camera to impostor in last update.
            /// Equals to 'impostor.position - camera.position'
            /// </summary>
            public float3 cameraDirection;

            public float3 objectForwardDirection;
            public float screenSize;
            public float distance;
            public int textureResolution;
        }

        public LastUpdate lastUpdate;
    }
}