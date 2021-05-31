using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public struct EnableShaderKeywordInstruction : IRenderInstruction
    {
        public string Keyword { get; }

        public EnableShaderKeywordInstruction(string keyword)
        {
            Keyword = keyword;
        }

        public void ApplyCommandBuffer(CommandBuffer cb)
        {
            cb.EnableShaderKeyword(Keyword);
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
            
        }
    }
}