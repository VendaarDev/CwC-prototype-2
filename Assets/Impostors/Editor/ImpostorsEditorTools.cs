using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Impostors.Managers;
using Impostors.Structs;
using Impostors.RenderPipelineProxy;
#if IMPOSTORS_UNITY_PIPELINE_URP
using Impostors.URP;
#endif
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Impostors.Editor
{
    public static class ImpostorsEditorTools
    {
        [MenuItem("Tools/Impostors/Create Scene Managers")]
        public static void CreateSceneManagers()
        {
            var impostorLodGroupManager = new GameObject("IMPOSTORS").AddComponent<ImpostorLODGroupsManager>();
            impostorLodGroupManager.transform.SetAsFirstSibling();
            var impostorLodGroupManagerType = impostorLodGroupManager.GetType();
            impostorLodGroupManagerType.GetField("_ditherTexture", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(impostorLodGroupManager,
                    Resources.Load<Texture>("impostors-dither-pattern"));
            impostorLodGroupManagerType.GetField("_shader", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(impostorLodGroupManager,
                    Shader.Find("Impostors/ImpostorsShader"));

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var go = new GameObject($"{(mainCamera ? mainCamera.name : "Main Camera")} Impostors");
                go.SetActive(false);
                var impostorableObjectsManagerForMainCamera = go.AddComponent<ImpostorableObjectsManager>();
                impostorableObjectsManagerForMainCamera.transform.SetParent(impostorLodGroupManager.transform);
                var type = impostorableObjectsManagerForMainCamera.GetType();
                type.GetField("_mainCamera", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(impostorableObjectsManagerForMainCamera,
                        mainCamera);
                type.GetField("_directionalLight", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                    impostorableObjectsManagerForMainCamera,
                    Object.FindObjectsOfType<Light>().FirstOrDefault(x => x.type == LightType.Directional));


                RenderPipelineProxyBase renderPipelineProxy = null;
#if IMPOSTORS_UNITY_PIPELINE_URP
                renderPipelineProxy = impostorableObjectsManagerForMainCamera.gameObject
                    .AddComponent<UniversalRenderPipelineProxy>();
#else
                renderPipelineProxy = impostorableObjectsManagerForMainCamera.gameObject
                    .AddComponent<BuiltInRenderPipelineProxy>();
#endif
                type.GetField("_renderPipelineProxy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                    impostorableObjectsManagerForMainCamera,
                    renderPipelineProxy);

                renderPipelineProxy.GetType().GetField("_cameraWhereToScheduleCommandBufferExecution",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                    renderPipelineProxy,
                    mainCamera);

                go.SetActive(true);
            }

            Undo.RegisterCreatedObjectUndo(impostorLodGroupManager.gameObject, "impostors manager");
            EditorUtility.SetDirty(impostorLodGroupManager);
        }

        [MenuItem("Tools/Impostors/Setup Impostor(s)")]
        public static void SetupImpostorLODGroups()
        {
            Transform[] _selected = Selection.transforms;
            if (_selected.Length == 0)
            {
                Debug.LogError(
                    "No gameObject selected! Please, select gameObject with LODGroup component to setup impostors");
                return;
            }

            Undo.SetCurrentGroupName("Setup Impostor(s)");
            bool hasProblems = false;

            foreach (Transform trans in _selected)
            {
                var lodGroup = trans.GetComponent<LODGroup>();
                if (lodGroup == null && lodGroup.GetComponent<ImpostorLODGroup>() == null)
                {
                    hasProblems = true;
                    Debug.LogError(
                        $"Can't add {nameof(ImpostorLODGroup)} to '{trans.name}' because it has no {nameof(LODGroup)} component. Impostors work only in combination with {nameof(LODGroup)} component. Click to navigate to this object.",
                        trans);
                    continue;
                }

                SetupImpostorLODGroupToObject(lodGroup);
            }

            if (hasProblems)
                EditorUtility.DisplayDialog(
                    "Setup Imposters",
                    "Automatic imposter setup faced some problems. Look at the console for more details.",
                    "Ok");
        }

        [MenuItem("Tools/Impostors/Remove Impostor(s)")]
        public static void RemoveImpostorLODGroups()
        {
            Transform[] _selected = Selection.transforms;
            if (_selected.Length == 0)
            {
                Debug.LogError("No gameObject selected! Please, select gameObject to remove imposter");
                return;
            }

            Undo.SetCurrentGroupName("Remove Impostor(s)");

            ImpostorLODGroup[] ilods;
            foreach (Transform trans in _selected)
            {
                ilods = trans.GetComponentsInChildren<ImpostorLODGroup>(true);
                for (int i = 0; i < ilods.Length; i++)
                {
                    Undo.DestroyObjectImmediate(ilods[i]);
                }
            }
        }

        [MenuItem("Tools/Impostors/Optimize Scene")]
        public static void OptimizeScene()
        {
            Undo.SetCurrentGroupName("Optimize Scene");
            if (Object.FindObjectOfType<ImpostorLODGroupsManager>() == null)
                CreateSceneManagers();
            var lodGroups = Object.FindObjectsOfType<LODGroup>();
            var title = "Optimizing Scene with Impostors";
            int i = 0;
            int count = 0;
            foreach (var lodGroup in lodGroups)
            {
                i++;
                if (lodGroup.size * ImpostorsUtility.MaxV3(lodGroup.transform.lossyScale) < 2f)
                    continue;
                count++;
                EditorUtility.DisplayProgressBar(title, lodGroup.name, i / (float) lodGroups.Length);
                if (lodGroup.GetComponent<ImpostorLODGroup>() == null)
                    SetupImpostorLODGroupToObject(lodGroup);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"Populated scene with {count} {nameof(ImpostorLODGroup)} components");
        }

        [MenuItem("Tools/Impostors/Enable Impostors")]
        public static void EnableImpostors()
        {
            var lodGroups = Object.FindObjectsOfType<ImpostorLODGroup>();
            foreach (var lodGroup in lodGroups)
            {
                lodGroup.enabled = true;
            }
        }

        [MenuItem("Tools/Impostors/Disable Impostors")]
        public static void DisableImpostors()
        {
            var lodGroups = Object.FindObjectsOfType<ImpostorLODGroup>();
            foreach (var lodGroup in lodGroups)
            {
                lodGroup.enabled = false;
            }
        }


        private static void SetupImpostorLODGroupToObject(LODGroup lodGroup)
        {
            var lods = lodGroup.GetLODs();
            Undo.RegisterCompleteObjectUndo(lodGroup.gameObject, "Setup ImpostorLODGroup");
            lodGroup.gameObject.SetActive(false);
            var impostorLODGroup = Undo.AddComponent<ImpostorLODGroup>(lodGroup.gameObject);
            ImpostorLOD impostorLodEmpty = new ImpostorLOD();
            impostorLodEmpty.screenRelativeTransitionHeight = lods.Last().screenRelativeTransitionHeight * 2;
            impostorLodEmpty.renderers = new Renderer[0];
            ImpostorLOD impostorLOD = new ImpostorLOD();
            impostorLOD.screenRelativeTransitionHeight = 0.005f;
            impostorLOD.renderers = lods.Last().renderers;
            impostorLODGroup.zOffset = 0f;

            impostorLODGroup.LODs = new[] {impostorLodEmpty, impostorLOD};

            impostorLODGroup.RecalculateBounds();
            lodGroup.gameObject.SetActive(true);
        }
    }
}