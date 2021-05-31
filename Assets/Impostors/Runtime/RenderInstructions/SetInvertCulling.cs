using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public class SetInvertCulling : IRenderInstruction
    {
        private readonly bool _invert;

        public SetInvertCulling(bool invert)
        {
            _invert = invert;
        }
        public void ApplyCommandBuffer(CommandBuffer cb)
        {
            cb.SetInvertCulling(_invert);
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
        }
    }
}