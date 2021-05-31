using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    [System.Serializable]
    public class RenderInstructionsBuffer
    {
        private readonly List<IRenderInstruction> _renderInstructions = new List<IRenderInstruction>();
        public readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

        public void Add(IRenderInstruction instruction)
        {
            Assert.IsNotNull(instruction);
            _renderInstructions.Add(instruction);
        }

        public void Apply(CommandBuffer cb)
        {
            for (int i = 0; i < _renderInstructions.Count; i++)
            {
                _renderInstructions[i].ApplyCommandBuffer(cb);
            }
        }

        public void RegenerateMaterialPropertyBlock()
        {
            PropertyBlock.Clear();
            for (int i = 0; i < _renderInstructions.Count; i++)
            {
                _renderInstructions[i].ApplyMaterialPropertyBlock(PropertyBlock);
            }
        }
    }
}