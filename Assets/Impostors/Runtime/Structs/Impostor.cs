using UnityEngine;

namespace Impostors.Structs
{
    [System.Serializable]
    public struct Impostor
    {
        public bool Exists => impostorLODGroupInstanceId != 0;

        public int impostorLODGroupInstanceId;
        public bool isRelevant;

        public Vector3 position;
        public Vector3 direction;
        public Vector3 sizeAndOffset;
        public float quadSize;
        public float zOffset;
        public float fadeTime;
        public Vector4 uv;
        public float time;
    }
}