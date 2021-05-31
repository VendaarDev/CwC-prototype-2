﻿using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderPipelineProxy
{
    public abstract class RenderPipelineProxyBase : MonoBehaviour
    {
        public event Action<Camera> PreCullCalled;

        public abstract void ScheduleImpostorTextureRendering(CommandBuffer commandBuffer);

        public abstract void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material,
            int layer, Camera camera, int submeshIndex, MaterialPropertyBlock materialPropertyBlock, bool castShadows,
            bool receiveShadows, bool useLightProbes);
        
        

        protected void OnPreCullCalled(Camera camera)
        {
            PreCullCalled?.Invoke(camera);
        }

        public virtual void SetFogEnabled(bool value, CommandBuffer commandBuffer)
        {
            ImpostorsUtility.SetFogShaderKeywordsEnabled(value, commandBuffer);
        }
    }
}