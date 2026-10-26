using Duckov.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using YuukaDogModle;

namespace YuukaDogModle
{
    internal class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private AssetBundle loadedBundle;
        private GameObject loadedObject;
        private GameObject instantedObject;
        private Animator petAnimator;
        private CharacterModel characterModel;
        private Movement movement;
        private string[] pathsToHide = new string[]
        {
            "Dog/Rig",
            "Dog/Mesh_LOD1"
        };
        private string bundleName = "sensei_dog";
        private string assetName = "SenseiDogModle";
        private string targetShaderName = "SodaCraft/SodaCharacter";
        private bool isModelLoaded;
        private bool isBundleLoaded = false;
        private bool isWaitingForPet = false;

        private void Start()
        {
            ModLog.Log("[YuukaDogModle]开始加载");
            this.StartCoroutine(LoadPetBundle());
            LevelManager.OnAfterLevelInitialized += OnLevelLoaded;
        }

        private void Update()
        {
            if (petAnimator == null || characterModel == null || movement == null)
                return;
            Vector3 velocity = movement.Velocity;
            bool isMoving = velocity.magnitude > 0.1f;
            petAnimator.SetBool("Moving", isMoving);
        }
        private void OnLevelLoaded()
        {
            ModLog.Log("[YuukaDogModle]场景加载完成宠物替换");
            characterModel = null;
            movement = null;
            petAnimator = null;
            if (instantedObject != null)
            {
                Destroy(instantedObject);
                instantedObject = null;
            }
            if (isBundleLoaded)
            {
                StartCoroutine(WaitForPet());
            }
        }
        private IEnumerator LoadPetBundle()
        {
            string bundlePath = Path.Combine(GetModDirectory(), bundleName);
            ModLog.Log($"[YuukaDogModle]尝试加载资源包_{bundlePath}");
            if (!File.Exists(bundlePath))
            {
                ModLog.Error($"[YuukaDogModle]资源包不存在_{bundlePath}");
                yield break;
            }
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;
            loadedBundle = request.assetBundle;
            if (loadedBundle == null)
            {
                ModLog.Error("[YuukaDogModle]资源包加载失败");
                yield break;
            }
            ModLog.Log("[YuukaDogModle]资源包加载成功，开始加载模型");
            AssetBundleRequest assetRequest = loadedBundle.LoadAssetAsync<GameObject>(assetName);
            yield return assetRequest;
            loadedObject = assetRequest.asset as GameObject;
            if (loadedObject == null)
            {
                ModLog.Error("[YuukaDogModle]模型资源加载失败_SenseiDogModle");
                loadedBundle.Unload(true);
                yield break;
            }

            ModLog.Log("[YuukaDogModle]模型资源加载成功");
            isBundleLoaded = true;
            this.StartCoroutine(WaitForPet());
        }

        private IEnumerator WaitForPet()
        {
            isWaitingForPet = true;
            ModLog.Log("[YuukaDogModle]等待宠物角色加载...");
            while (LevelManager.Instance == null ||
                   LevelManager.Instance.PetCharacter == null ||
                   LevelManager.Instance.PetCharacter.characterModel == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
            characterModel = LevelManager.Instance.PetCharacter.characterModel;
            movement = characterModel.characterMainControl.movementControl;
            ModLog.Log("[YuukaDogModle]宠物角色已找到，开始替换模型");
            this.ReplaceModel();
        }

        private void ReplaceModel()
        {
            if (loadedObject == null)
            {
                ModLog.Error("[YuukaDogModle]模型资源为空");
                return;
            }

            HideOriginalPet();
            instantedObject = Instantiate(loadedObject, characterModel.transform);
            instantedObject.transform.localPosition = Vector3.zero;
            instantedObject.transform.localRotation = Quaternion.identity;
            instantedObject.transform.localScale = Vector3.one;
            this.ReplaceAllShaders(instantedObject, targetShaderName);
            petAnimator = instantedObject.GetComponent<Animator>();
            if (petAnimator == null)
            {
                petAnimator = instantedObject.GetComponentInChildren<Animator>();
            }
        }

        private void ReplaceAllShaders(GameObject target, string shaderName)
        {
            Shader targetShader = Shader.Find(shaderName);
            if (targetShader == null)
            {
                ModLog.Error($"[YuukaDogModle]找不到着色器 - {shaderName}");
                return;
            }
            ModLog.Log($"[YuukaDogModle]开始替换着色器 - {shaderName}");
            this.ProcessRenderersRecursive(target, targetShader);
            ModLog.Log("[YuukaDogModle]着色器替换完成");
        }

        private void ProcessRenderersRecursive(GameObject obj, Shader targetShader)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                ReplaceRendererShaders(renderer, targetShader);
            }
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                this.ProcessRenderersRecursive(obj.transform.GetChild(i).gameObject, targetShader);
            }
        }

        private void ReplaceRendererShaders(Renderer renderer, Shader targetShader)
        {
            Material[] materials = renderer.materials;
            bool replaced = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    materials[i].shader = targetShader;
                    replaced = true;
                }
            }
            if (replaced)
            {
                ModLog.Log($"[YuukaDogModle]已替换 {renderer.gameObject.name} 的着色器");
            }
        }

        private void HideOriginalPet()
        {
            bool foundAny = false;
            foreach (string path in pathsToHide)
            {
                Transform target = characterModel.transform.Find(path);
                if (target != null)
                {
                    target.gameObject.SetActive(false);
                    foundAny = true;
                    ModLog.Log($"[YuukaDogModle]已隐藏 - {path}");
                }
                else
                {
                    ModLog.Warning($"[YuukaDogModle]未找到路径 - {path}");
                }
            }
        }

        private string GetModDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void OnDisable()
        {
            ModLog.Log("[YuukaDogModle]模组禁用");
            this.CleanupResources();
        }

        private void OnDestroy()
        {
            ModLog.Log("[YuukaDogModle]模组销毁");
            this.CleanupResources();
            LevelManager.OnAfterLevelInitialized -= OnLevelLoaded;
            isBundleLoaded = false;
            isWaitingForPet = false;
        }

        private void CleanupResources()
        {
            if (instantedObject != null)
            {
                Destroy(instantedObject);
                instantedObject = null;
            }
            if (loadedBundle != null)
            {
                loadedBundle.Unload(true);
                loadedBundle = null;
            }
            loadedObject = null;
            petAnimator = null;
        }
    }
}

