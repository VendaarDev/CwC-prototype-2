﻿#if IMPOSTORS_UNITY_PIPELINE_URP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Impostors.URP
{
    public class UpdateImpostorsTexturesFeature : ScriptableRendererFeature
    {
        private Dictionary<Camera, UpdateImpostorsTexturesRenderPass> _renderPasses;

        [SerializeField]
        private bool _clearBufferAfterPass = true;

        class UpdateImpostorsTexturesRenderPass : ScriptableRenderPass
        {
            private readonly Func<bool> _clearBufferAfterPass;
            public readonly List<CommandBuffer> CommandBuffers;

            public UpdateImpostorsTexturesRenderPass(Func<bool> clearBufferAfterPass)
            {
                _clearBufferAfterPass = clearBufferAfterPass;
                CommandBuffers = new List<CommandBuffer>();
            }

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                foreach (var commandBuffer in CommandBuffers)
                {
                    context.ExecuteCommandBuffer(commandBuffer);
                }
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (_clearBufferAfterPass.Invoke())
                    CommandBuffers.Clear();
            }
        }

        public override void Create()
        {
            _renderPasses = new Dictionary<Camera, UpdateImpostorsTexturesRenderPass>();
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPasses.TryGetValue(renderingData.cameraData.camera, out var renderPass))
            {
                if (renderPass.CommandBuffers.Count > 0)
                    renderer.EnqueuePass(renderPass);
            }
        }

        public void AddCommandBuffer(Camera camera, CommandBuffer commandBuffer)
        {
            if (!_renderPasses.TryGetValue(camera, out var renderPass))
            {
                renderPass = new UpdateImpostorsTexturesRenderPass(() => _clearBufferAfterPass);
                // Configures where the render pass should be injected.
                renderPass.renderPassEvent = RenderPassEvent.BeforeRendering;
                _renderPasses.Add(camera, renderPass);
            }

            renderPass.CommandBuffers.Add(commandBuffer);
        }

        public void Clear(Camera mainCamera)
        {
            if (_renderPasses.TryGetValue(mainCamera, out var renderPass))
                renderPass.CommandBuffers.Clear();
        }
    }
}
#endif