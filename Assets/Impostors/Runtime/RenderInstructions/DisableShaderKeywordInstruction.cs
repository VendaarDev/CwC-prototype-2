using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public struct DisableShaderKeywordInstruction: IRenderInstruction
    {
        public string Keyword { get; }

        public DisableShaderKeywordInstruction(string keyword)
        {
            Keyword = keyword;
        }

        public void ApplyCommandBuffer(CommandBuffer cb)
        {
            cb.DisableShaderKeyword(Keyword);
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
            
        }
    }
}