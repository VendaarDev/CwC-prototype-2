using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public struct CopySHCoefficientArraysFromInstruction : IRenderInstruction
    {
        private SphericalHarmonicsL2[] LightProbes { get; }
        private int SourceStart { get; }
        private int DestStart { get; }
        private int Count { get; }

        public CopySHCoefficientArraysFromInstruction(SphericalHarmonicsL2[] lightProbes, int sourceStart,
            int destStart, int count)
        {
            LightProbes = lightProbes;
            SourceStart = sourceStart;
            DestStart = destStart;
            Count = count;
        }

        public void ApplyCommandBuffer(CommandBuffer cb)
        {
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
            prop.CopySHCoefficientArraysFrom(LightProbes, SourceStart, DestStart, Count);
        }
    }
}