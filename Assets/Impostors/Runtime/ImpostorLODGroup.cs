using System.Collections.Generic;
using System.Linq;
using Impostors.Managers;
using Impostors.RenderInstructions;
using Impostors.Structs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Impostors
{
    [RequireComponent(typeof(LODGroup))]
    public class ImpostorLODGroup : MonoBehaviour
    {
        private LODGroup _lodGroup;
        private Transform _transform;

        [HideInInspector]
        public bool isStatic = true; // todo

        [Tooltip("List of Impostor Level Of Details, like in LODGroup component. " +
                 "Each LOD contains renderers and screen size after which renderers will be shown.")]
        [SerializeField]
        private ImpostorLOD[] _lods = new ImpostorLOD[2];

        [Tooltip(
            "GENERATED.\nBounds size of ImpostorLODGroup. This value is used to determine whether ImpostorLODGroup is visible in the camera.")]
        [SerializeField]
        private Vector3 _size;

        [Tooltip("GENERATED.\nSize of quad that will be generated for impostor.")]
        [SerializeField]
        private float _quadSize;

        [Tooltip("Determines how far will be generated impostor quad from center of object. " +
                 "This is useful when impostors intersects with ground. " +
                 "Default value of 0.5f works in most cases")]
        [Range(0f, 1f)]
        public float zOffset = 0.5f;

        [Tooltip("Time in seconds that is needed for impostor to fade in/out when impostor changes texture.")]
        [Range(0f, 1f)]
        public float fadeTransitionTime = 0.2f;

        [Tooltip("Euler angles that determines how much direction from camera should change to cause texture update. " +
                 "The less this value, the more often impostor's texture will be updated.")]
        [Min(0.1f)]
        public float deltaCameraAngle = 1f;

        [Tooltip("Determines relative distance change between camera and object that will cause texture update. " +
                 "The less this value, the more often impostor's texture will be updated.")]
        [Min(0.01f)]
        public float deltaDistance = .1f;

        [Tooltip("Check this to update impostor's texture over time.")]
        public bool useUpdateByTime = false;

        [Tooltip("Time in seconds, after which impostor's texture will be updated. " +
                 "The less this value, the more often impostor's texture will be updated. " +
                 "Check 'useUpdateByTime' to take this setting into account.")]
        [Min(0.01f)]
        public float timeInterval = 1f;

        [Tooltip("Check this to update impostor's texture when main directional light changes direction.")]
        public bool useDeltaLightAngle = true;

        [Tooltip("Determines how much direction of main light should change to cause texture update. " +
                 "The less this value, the more often impostor's texture will be updated.\n" +
                 "In euler angles.")]
        [Min(0.01f)]
        public float deltaLightAngle = 3;

        public TextureResolution minTextureResolution = TextureResolution._32x32;
        public TextureResolution maxTextureResolution = TextureResolution._512x512;

        internal int IndexInImpostorsManager = -1;

        public Vector3 Position => _transform.TransformPoint(_lodGroup.localReferencePoint);

        public float LocalHeight => _lodGroup.size * Mathf.Abs(_transform.lossyScale.y);

        public float ZOffsetWorld => _quadSize * zOffset;

        public float FadeInTime => _lodGroup.fadeMode != LODFadeMode.None ? 0.3f : 0f;

        public float FadeOutTime => _lodGroup.fadeMode != LODFadeMode.None ? 1.5f : 0f;

        public float ScreenRelativeTransitionHeight => _lods[0].screenRelativeTransitionHeight;

        public float ScreenRelativeTransitionHeightCull => _lods[_lods.Length - 1].screenRelativeTransitionHeight;

        public float QuadSize => _quadSize;

        public Vector3 Size => _size;

        /// <summary>
        /// Sets impostor's LODs. In most cases you need to use <see cref="SetLODsAndCache"/> instead.
        /// </summary>
        public ImpostorLOD[] LODs
        {
            get { return _lods; }
            set { _lods = value; }
        }

        private int matFrom => 0;
        private int matTo => 100;

        #region CACHE

        private float[] _lodGroupOriginalScreenRelativeTransitionHeights;
        private Dictionary<Renderer, RenderInstructionsBuffer> _dictRendererToRenderInstructionsBuffer;

        #endregion

        private void Awake()
        {
            _lodGroup = GetComponent<LODGroup>();
            _lodGroup.RecalculateBounds();
            _transform = transform;
            // Cache must be called at OnEnable to get original mesh if static batching enabled (static batching replaces original mesh with combined one)
            Cache();
        }

        private void OnEnable()
        {
            IndexInImpostorsManager = ImpostorLODGroupsManager.Instance.AddImpostorLODGroup(this);
            var lods = _lodGroup.GetLODs();
            _lodGroupOriginalScreenRelativeTransitionHeights = new float[lods.Length];
            float minValue = ScreenRelativeTransitionHeight;
            for (int i = lods.Length - 1; i >= 0; i--)
            {
                _lodGroupOriginalScreenRelativeTransitionHeights[i] = lods[i].screenRelativeTransitionHeight;
                lods[i].screenRelativeTransitionHeight = Mathf.Clamp(lods[i].screenRelativeTransitionHeight,
                    minValue, 1);
                minValue += 0.000001f;
            }

            _lodGroup.SetLODs(lods);
        }

        private void OnDisable()
        {
            ImpostorLODGroupsManager.Instance.RemoveImpostorLODGroup(this);
            IndexInImpostorsManager = -1;
            var lods = _lodGroup.GetLODs();
            float minValue =
                _lodGroupOriginalScreenRelativeTransitionHeights[
                    _lodGroupOriginalScreenRelativeTransitionHeights.Length - 1];
            for (int i = _lodGroupOriginalScreenRelativeTransitionHeights.Length - 1; i >= 0; i--)
            {
                lods[i].screenRelativeTransitionHeight =
                    Mathf.Max(_lodGroupOriginalScreenRelativeTransitionHeights[i], minValue);
                minValue += 0.000001f;
            }

            _lodGroup.SetLODs(lods);
        }

        private void OnValidate()
        {
            var lodGroup = GetComponent<LODGroup>();
            Debug.Assert(lodGroup);
            float lodGroupCullHeight = lodGroup.GetLODs().Last().screenRelativeTransitionHeight;
            if (_lods[0].screenRelativeTransitionHeight < lodGroupCullHeight)
                _lods[0].screenRelativeTransitionHeight = lodGroupCullHeight;
        }

        internal void AddCommandBufferCommands(CommandBuffer cb, Vector3 cameraPosition,
            float screenSize,
            List<SphericalHarmonicsL2> lightProbes, int lightProbeIndex)
        {

            Vector3 locBillPos = Position;
            Vector3 fromCamToCenter = cameraPosition - locBillPos;

            Quaternion renderingCameraRotation = Quaternion.LookRotation(-fromCamToCenter);
            float impostorQuadSize = _quadSize;

            fromCamToCenter = cameraPosition - locBillPos - fromCamToCenter.normalized * ZOffsetWorld;

            float angleForCamera = 2 * Mathf.Atan2(impostorQuadSize * 0.5f, fromCamToCenter.magnitude) * Mathf.Rad2Deg;
            float zFar = fromCamToCenter.magnitude + QuadSize * 1.5f;
            float zNear = Mathf.Max(fromCamToCenter.magnitude - QuadSize * 1.5f, 0.3f);

            Matrix4x4 V = Matrix4x4.TRS(cameraPosition, renderingCameraRotation, new Vector3(1, 1, -1))
                .inverse;
            Matrix4x4 p = Matrix4x4.Perspective(angleForCamera, 1, zNear, zFar);

            Profiler.BeginSample("Config command buffer");
            cb.SetViewProjectionMatrices(V, p);
            cb.SetGlobalVector(ShaderProperty._WorldSpaceCameraPos, cameraPosition);
            cb.SetGlobalVector(ShaderProperty._ProjectionParams, new Vector4(-1, zNear, zFar, 1 / zFar));

            int lodLevel = -1;
            for (int i = 0; i < _lods.Length; i++)
            {
                lodLevel = i;
                if (_lods[i].screenRelativeTransitionHeight < screenSize)
                    break;
            }

            if (lodLevel == 0 || lodLevel == -1)
                Debug.LogError("This must not happen");

            var renderers = _lods[lodLevel].renderers;
            for (int i = 0; i < renderers.Length; i++)
            {
                var rend = renderers[i];
                var buff = _dictRendererToRenderInstructionsBuffer[rend];
                if (buff == null)
                {
                    Debug.LogError($"There is no RenderInstructionBuffer for {rend.name}", this);
                    continue;
                }

                buff.PropertyBlock.CopySHCoefficientArraysFrom(lightProbes, lightProbeIndex, 0, 1);
                buff.Apply(cb);
            }

            Profiler.EndSample();
        }

        [ContextMenu("Recalculate Bounds")]
        public void RecalculateBounds()
        {
            Bounds bound = new Bounds();
            var renderers = _lods.SelectMany(lod => lod.renderers);
            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;
                if (bound.extents == Vector3.zero)
                    bound = r.bounds;
                else
                    bound.Encapsulate(r.bounds);
            }

            _size = bound.size;
            _quadSize = ImpostorsUtility.MaxV3(_size);
        }

        [ContextMenu("Update Settings")]
        public void UpdateSettings()
        {
            if (IndexInImpostorsManager != -1)
                ImpostorLODGroupsManager.Instance.UpdateSettings(this);
        }

        [ContextMenu("Cache")]
        public void Cache()
        {
            CreateRenderInstructionsBuffers();
        }

        /// <summary>
        /// Sets LODs and runs additional calculation to update settings.
        ///   - recalculates bound,
        ///   - caches render instructions,
        ///   - updates settings in ImpostorManager
        /// </summary>
        /// <param name="lods"></param>
        public void SetLODsAndCache(ImpostorLOD[] lods)
        {
            LODs = lods;
            RecalculateBounds();
            Cache();
            UpdateSettings();
        }
        
        private void CreateRenderInstructionsBuffers()
        {
            var renderers = _lods.SelectMany(lod => lod.renderers).ToArray();

            // Disabling renderers that are not presented in LODGroup to make them invisible.
            // (LODGroup doesn't control renderers that are not present in any LOD level)
            {
                var lodRenderers = _lodGroup.GetLODs().SelectMany(x => x.renderers).Distinct();
                foreach (var impostorRenderer in renderers)
                {
                    if (lodRenderers.Contains(impostorRenderer) == false)
                        impostorRenderer.enabled = false;
                }
            }

            if (_dictRendererToRenderInstructionsBuffer == null)
                _dictRendererToRenderInstructionsBuffer =
                    new Dictionary<Renderer, RenderInstructionsBuffer>(renderers.Length);
            _dictRendererToRenderInstructionsBuffer.Clear();

            List<Material> sharedMaterials = new List<Material>(1);
            var lightmaps = LightmapSettings.lightmaps;

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var buff = new RenderInstructionsBuffer();
                _dictRendererToRenderInstructionsBuffer.Add(renderer, buff);

                Mesh mesh = null;
                if (renderer is MeshRenderer)
                    mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                else if (renderer is SkinnedMeshRenderer)
                    mesh = ((SkinnedMeshRenderer) renderer).sharedMesh;
                else
                {
                    Debug.LogError(
                        $"Unsupported renderer type '{renderer.GetType().Name}'. Please, remove it from impostor's LODs.",
                        renderer);
                    continue;
                }

                if (renderer.isPartOfStaticBatch)
                {
                    Debug.LogError("Mesh already combined and cannot be used in ImpostorLODGroup", renderer);
                    continue;
                }

                renderer.GetSharedMaterials(sharedMaterials);
                var matrix = renderer.localToWorldMatrix;
                var lossyScale = matrix.lossyScale;
                bool requiresInvertCulling = Mathf.Sign(lossyScale.x * lossyScale.y * lossyScale.z) < 0;
                if (requiresInvertCulling)
                    buff.Add(new SetInvertCulling(true));
                bool hasLightmap = renderer.lightmapIndex != -1;

//                buff.Add(new SetVectorInstruction("_ProjectionParams", new Vector4(-1, 0.1f, 5000, 0.0002f))); //CHANGED
//                buff.Add(new EnableShaderKeywordInstruction("SHADOWS_SCREEN")); //CHANGED
//                buff.Add(new DisableShaderKeywordInstruction("DIRECTIONAL")); //CHANGED

                if (hasLightmap)
                {
                    buff.Add(new EnableShaderKeywordInstruction("LIGHTMAP_ON"));
                    buff.Add(new DisableShaderKeywordInstruction("LIGHTPROBE_SH"));
#if IMPOSTORS_UNITY_PIPELINE_URP
                    buff.Add(new SetVectorInstruction(ShaderProperty._MainLightColor, Vector4.zero));
#else
                    buff.Add(new SetVectorInstruction(ShaderProperty._LightColor0, Vector4.zero));
#endif
                    var lightmapData = lightmaps[renderers[i].lightmapIndex];
                    buff.Add(new SetTextureInstruction(ShaderProperty.unity_Lightmap, lightmapData.lightmapColor));
                    buff.Add(new SetVectorInstruction(ShaderProperty.unity_LightmapST, renderer.lightmapScaleOffset));
                    if (lightmapData.lightmapDir)
                    {
                        buff.Add(new EnableShaderKeywordInstruction("DIRLIGHTMAP_COMBINED"));
                        buff.Add(new SetTextureInstruction(ShaderProperty.unity_LightmapInd, lightmapData.lightmapDir));
                    }
                }
                else
                {
                    buff.Add(new DisableShaderKeywordInstruction("LIGHTMAP_ON"));
                    buff.Add(new DisableShaderKeywordInstruction("DIRLIGHTMAP_COMBINED"));
                    buff.Add(new EnableShaderKeywordInstruction("LIGHTPROBE_SH"));
                }


                for (int submeshIndex = matFrom;
                    submeshIndex < Mathf.Min(sharedMaterials.Count, matTo);
                    submeshIndex++) //CHANGED added matFrom matTo
                {
                    buff.Add(new DrawMeshInstruction(
                        mesh,
                        matrix,
                        sharedMaterials[submeshIndex],
                        submeshIndex,
                        shaderPass: 0
                    ));
                }

                if (requiresInvertCulling)
                    buff.Add(new SetInvertCulling(false));

                buff.RegenerateMaterialPropertyBlock();
            }
        }
    }
}