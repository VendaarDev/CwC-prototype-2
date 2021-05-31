using System;
using System.Collections.Generic;
using Impostors.MemoryUsage;
using Impostors.ObjectPools;
using Impostors.TimeProvider;
using UnityEngine;
using UnityEngine.Assertions;

namespace Impostors.Managers
{
    [DefaultExecutionOrder(-777)]
    public class ImpostorLODGroupsManager : MonoBehaviour, IMemoryConsumer
    {
        public static ImpostorLODGroupsManager Instance { get; private set; }

        public ITimeProvider TimeProvider { get; private set; }

        [SerializeField]
        private bool _HDR = true;

        [SerializeField]
        private bool _useMipMap = true;

        [Range(-5f, 5f)]
        [SerializeField]
        private float _mipMapBias = 0;

        [Range(0f, 1f)]
        [SerializeField]
        private float _cutout = 0.2f;

        [Range(0f, 180f)]
        [SerializeField]
        private float _minAngleToStopLookAtCamera = 30;

        [SerializeField]
        private Shader _shader = default;

        [SerializeField]
        private Texture _ditherTexture = default;

        [Space]
        [Header("Runtime")]
        public CompositeRenderTexturePool RenderTexturePool;

        public MaterialObjectPool MaterialObjectPool;

        [SerializeField]
        private List<ImpostorableObjectsManager> _impostorsManagers = default;
        
        [SerializeField]
        private List<ImpostorLODGroup> _impostorLodGroups = default;

        private Dictionary<int, ImpostorLODGroup> _dictInstanceIdToImpostorLODGroup;

        private bool _isDestroying = false;

        private void OnEnable()
        {
            _isDestroying = false;
            Instance = this;
            _impostorLodGroups = new List<ImpostorLODGroup>();
            _dictInstanceIdToImpostorLODGroup = new Dictionary<int, ImpostorLODGroup>();
            TimeProvider = new UnscaledTimeProvider();
            RenderTexturePool = new CompositeRenderTexturePool(Enum.GetValues(typeof(AtlasResolution)) as int[], 0, 16,
                _useMipMap, _mipMapBias, _HDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            MaterialObjectPool = new MaterialObjectPool(0, _shader);
        }

        private void OnDisable()
        {
            _isDestroying = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_ditherTexture)
                _ditherTexture = Resources.Load<Texture>("impostors-dither-pattern");
            if (!_shader)
                _shader = Shader.Find("Impostors/ImpostorsShader");
            if (!_ditherTexture)
                Debug.LogError("Impostors fading won't work without specifying dither pattern texture! Default name is 'impostors-dither-pattern'.", this);
            if (!_shader)
                Debug.LogError("Impostors won't work without specifying right shader! Default path is '../Resources/Impostors/ImpostorsShader'.", this);
        }
#endif

        private void Update()
        {
            TimeProvider.Update();
            Shader.SetGlobalVector(ShaderProperty._ImpostorsTimeProvider,
                new Vector4(TimeProvider.Time, TimeProvider.DeltaTime, 0, 0));
            Shader.SetGlobalTexture(ShaderProperty._ImpostorsNoiseTexture, _ditherTexture);
            Shader.SetGlobalFloat(ShaderProperty._ImpostorsNoiseTextureResolution, _ditherTexture.width);
            Shader.SetGlobalFloat(ShaderProperty._ImpostorsCutout, _cutout);
            Shader.SetGlobalFloat(ShaderProperty._ImpostorsMinAngleToStopLookAt, _minAngleToStopLookAtCamera);
        }

        public int AddImpostorLODGroup(ImpostorLODGroup impostorLodGroup)
        {
            if (_isDestroying)
                return -1;
            _impostorLodGroups.Add(impostorLodGroup);
            _dictInstanceIdToImpostorLODGroup.Add(impostorLodGroup.GetInstanceID(), impostorLodGroup);

            for (int i = 0; i < _impostorsManagers.Count; i++)
            {
                _impostorsManagers[i].AddImpostorableObject(impostorLodGroup);
            }

            return _impostorLodGroups.Count - 1;
        }

        public void RemoveImpostorLODGroup(ImpostorLODGroup impostorLodGroup)
        {
            if (_isDestroying)
                return;
            int index = impostorLodGroup.IndexInImpostorsManager;
            Assert.AreEqual(impostorLodGroup, _impostorLodGroups[index]);
            _impostorLodGroups[index] = _impostorLodGroups[_impostorLodGroups.Count - 1];
            _impostorLodGroups[index].IndexInImpostorsManager = index;
            _impostorLodGroups.RemoveAt(_impostorLodGroups.Count - 1);

            for (int i = 0; i < _impostorsManagers.Count; i++)
            {
                _impostorsManagers[i].RemoveImpostorableObject(impostorLodGroup, index);
            }

            _dictInstanceIdToImpostorLODGroup.Remove(impostorLodGroup.GetInstanceID());
        }

        internal void RegisterImpostorableObjectsManager(ImpostorableObjectsManager manager)
        {
            if (_impostorsManagers.Contains(manager))
                return;
            _impostorsManagers.Add(manager);

            for (int i = 0; i < _impostorLodGroups.Count; i++)
            {
                manager.AddImpostorableObject(_impostorLodGroups[i]);
            }
        }

        internal void UnregisterImpostorableObjectsManager(ImpostorableObjectsManager manager)
        {
            _impostorsManagers.Remove(manager);
        }
        
        public ImpostorLODGroup GetByInstanceId(int instanceId)
        {
            return _dictInstanceIdToImpostorLODGroup[instanceId];
        }

        public void UpdateSettings(ImpostorLODGroup impostorLODGroup)
        {
            int index = impostorLODGroup.IndexInImpostorsManager;
            for (int i = 0; i < _impostorsManagers.Count; i++)
            {
                _impostorsManagers[i].UpdateSettings(index, impostorLODGroup);
            }
        }

        public int GetUsedBytes()
        {
            int res = 0;
            res += MemoryUsageUtility.GetMemoryUsage(_impostorsManagers);
            res += MemoryUsageUtility.GetMemoryUsage(_impostorLodGroups);
            res += _dictInstanceIdToImpostorLODGroup.Count * (8 + 4);

            foreach (var impostorableObjectsManager in _impostorsManagers)
            {
                res += impostorableObjectsManager.GetUsedBytes();
            }

            return res;
        }
    }
}