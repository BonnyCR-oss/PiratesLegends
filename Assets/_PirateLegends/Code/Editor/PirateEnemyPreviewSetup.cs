using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PirateEnemyPreviewSetup
{
    const string Root = "Assets/_PirateLegends/Art/Characters/Enemies/";
    const string Scene = "Assets/_PirateLegends/Scenes/Tests/SCN_Frostbound_AbilitiesTest.unity";

    public static string Build()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Scene)
            throw new InvalidOperationException("Abrir la escena de prueba de Frostbound fuera de Play.");
        BuildEnemy("CorsarioEspadachin", new[] {"iddle", "walk", "run_normal", "runfast", "Meshy_AI_model@Standing Melee AttackconEspada", "Meshy_AI_model@Standing Melee Run Jump Attack", "Meshy_AI_model@Flying Kick", "Meshy_AI_model@Gunplay", "Meshy_AI_model@Reaction"}, new Vector3(-2, 0, -1));
        BuildEnemy("CorsarioLuchador", new[] {"Idle", "iddleCombat", "Walking", "punch", "kicking", "damagerRecibidp", "death"}, new Vector3(2, 0, -1));
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Dos prefabs y 16 clips Humanoid preparados en la escena de Frostbound.";
    }

    static void BuildEnemy(string name, string[] files, Vector3 position)
    {
        string folder = Root + name + "/";
        var clips = new AnimationClip[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string path = folder + "Animations/" + files[i] + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            var settings = importer.defaultClipAnimations;
            foreach (var c in settings)
            {
                c.name = files[i].Replace("Meshy_AI_model@", "");
                c.loopTime = i < (name == "CorsarioEspadachin" ? 4 : 3);
                c.loopPose = c.loopTime;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.heightFromFeet = true;
            }
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            if (!clips[i].humanMotion) throw new InvalidOperationException("No es Humanoid: " + path);
        }

        string controllerPath = folder + name + "_Preview.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
            ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var existing in machine.states) machine.RemoveState(existing.state);
        var states = clips.Select((c, i) => {
            var state = machine.AddState(c.name, new Vector3(220 + (i % 3) * 240, (i / 3) * 100));
            state.motion = c;
            return state;
        }).ToArray();
        machine.defaultState = states[0];
        for (int i = 0; i < states.Length; i++)
        {
            var transition = states[i].AddTransition(states[(i + 1) % states.Length]);
            transition.hasExitTime = true;
            transition.exitTime = clips[i].isLooping ? Mathf.Max(1, 3f / clips[i].length) : 1;
            transition.duration = .15f;
            transition.hasFixedDuration = true;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(folder + name + ".mat");
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, folder + name + ".mat");
        }
        material.name = name;
        var texturePath = Directory.GetFiles(folder + "Textures", "*.png").Single();
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetFloat("_Smoothness", .25f);
        EditorUtility.SetDirty(material);
        string modelPath = folder + "Models/" + name + "_Rigged.fbx";
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        modelImporter.animationType = ModelImporterAnimationType.Human;
        modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
        modelImporter.SaveAndReimport();
        var root = new GameObject(name);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
        model.name = "Visual";
        model.transform.SetParent(root.transform, false);
        var animator = model.GetComponent<Animator>();
        if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
            throw new InvalidOperationException("Avatar inválido: " + name);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        clips[0].SampleAnimation(model, 0);
        var renderers = model.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        model.transform.localScale *= 1.85f / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.85f;
        collider.center = Vector3.up * .925f;
        collider.radius = .35f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "PF_" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0, 180, 0);
        Undo.RegisterCreatedObjectUndo(instance, "Colocar enemigo de prueba");
    }
}
