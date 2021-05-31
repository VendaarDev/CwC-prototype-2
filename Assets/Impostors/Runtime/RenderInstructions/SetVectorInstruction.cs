using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public readonly struct SetVectorInstruction : IRenderInstruction
    {
        public int PropertyId { get; }
        private Vector4 Value { get; }

        public SetVectorInstruction(int propertyId, Vector4 value)
        {
            PropertyId = propertyId;
            Value = value;
        }

        public void ApplyCommandBuffer(CommandBuffer cb)
        {
            
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
            prop.SetVector(PropertyId, Value);
        }
    }
}