using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public interface IRenderInstruction
    {
        void ApplyCommandBuffer(CommandBuffer cb);

        void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop);
    }
}
