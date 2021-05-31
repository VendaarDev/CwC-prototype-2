using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public readonly struct SetTextureInstruction : IRenderInstruction
    {
        public int PropertyId { get; }
        public Texture Texture { get; }

        public SetTextureInstruction(int propertyId, Texture texture)
        {
            PropertyId = propertyId;
            Texture = texture;
        }

        public void ApplyCommandBuffer(CommandBuffer cb)
        {
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
            prop.SetTexture(PropertyId, Texture);
        }
    }
}