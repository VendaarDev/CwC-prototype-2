using UnityEngine;
using UnityEngine.Rendering;

namespace Impostors.RenderInstructions
{
    public struct DrawMeshInstruction : IRenderInstruction
    {
        public Mesh Mesh { get; private set; }
        public Matrix4x4 Matrix { get; private set; }
        public Material Material { get; private set; }
        public int SubmeshIndex { get; private set; }
        public int ShaderPass { get; private set; }
        private MaterialPropertyBlock _propertyBlock;

        public DrawMeshInstruction(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass)
        {
            Mesh = mesh;
            Matrix = matrix;
            Material = material;
            SubmeshIndex = submeshIndex;
            ShaderPass = shaderPass;
            _propertyBlock = null;
        }

        public void ApplyCommandBuffer(CommandBuffer cb)
        {
            if (_propertyBlock == null)
                Debug.LogError("Property block is null. WTF");
            cb.DrawMesh(Mesh, Matrix, Material, SubmeshIndex, ShaderPass, _propertyBlock);
        }

        public void ApplyMaterialPropertyBlock(MaterialPropertyBlock prop)
        {
            _propertyBlock = prop;
        }
    }
}