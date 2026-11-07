using System;
using System.Linq;
using PirateLegends.Player;
using PirateLegends.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WindCharacterSetup
{
    const string Root = "Assets/_PirateLegends/Art/Characters/greenCharacter/";
    const string PrefabPath = "Assets/_PirateLegends/Prefabs/Characters/PF_Player_Wind.prefab";

    [MenuItem("Tools/Verde/Preparar personaje de viento")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de configurar.");
        string[] files = { "Personaje_Verde_Para_Mixamo@Idle.fbx", "walkVientoPerosnaje.fbx", "Personaje_Verde_Para_Mixamo@Fast Run.fbx", "Personaje_Verde_Para_Mixamo@Jump.fbx", "Personaje_Verde_Para_Mixamo@Hability1.fbx", "Hability2.fbx", "Personaje_Verde_Para_Mixamo@Sword And Shield Death.fbx" };
        string[] names = { "Idle", "Walk", "Run", "Jump", "Ability1", "Ability2", "Death" };
        var clips = new AnimationClip[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string path = Root + "Animations/" + files[i];
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            var settings = importer.defaultClipAnimations;
            if (settings.Length != 1) throw new InvalidOperationException("Se esperaba un clip: " + path);
            settings[0].name = names[i];
            settings[0].loopTime = i < 3;
            settings[0].loopPose = i < 3;
            settings[0].lockRootRotation = true;
            settings[0].keepOriginalOrientation = true;
            settings[0].lockRootPositionXZ = true;
            settings[0].lockRootHeightY = true;
            settings[0].heightFromFeet = true;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            if (!clips[i].humanMotion || clips[i].length <= .05f) throw new InvalidOperationException("Clip humanoide invalido: " + path);
        }

        string controllerPath = Root + "Animations/AC_Wind.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null) throw new InvalidOperationException("El controlador ya existe; revisar antes de regenerar.");
        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        var locomotion = sm.AddState("Locomotion");
        var tree = new BlendTree { name = "Wind Movement", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(clips[0], 0); tree.AddChild(clips[1], 1); tree.AddChild(clips[2], 3);
        locomotion.motion = tree;
        sm.defaultState = locomotion;
        for (int i = 3; i < clips.Length; i++)
        {
            var state = sm.AddState(names[i]); state.motion = clips[i];
            if (i == 3)
            {
                var enter = locomotion.AddTransition(state);
                enter.hasExitTime = false; enter.duration = .08f;
                enter.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            }
            if (i != 6)
            {
                var exit = state.AddTransition(locomotion);
                exit.hasExitTime = true; exit.exitTime = 1f; exit.duration = .1f;
            }
        }

        var root = new GameObject("PF_Player_Wind");
        try
        {
            root.tag = "Player";
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/Verde_Con_Ropa.prefab"));
            visual.transform.SetParent(root.transform, false);
            var animator = visual.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            clips[0].SampleAnimation(visual, 0);
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            visual.transform.localPosition -= Vector3.up * bounds.min.y;
            var body = root.AddComponent<CharacterController>();
            body.height = Mathf.Clamp(bounds.size.y, 1.5f, 2.5f);
            body.radius = .25f; body.center = Vector3.up * (body.height * .5f);
            body.skinWidth = .025f; body.stepOffset = .3f;
            root.AddComponent<PlayerInputReader>();
            var motor = root.AddComponent<PlayerLocomotion>();
            var motorData = new SerializedObject(motor);
            motorData.FindProperty("animator").objectReferenceValue = animator;
            motorData.ApplyModifiedPropertiesWithoutUndo();
            var abilities = root.AddComponent<WindAbilities>();
            var abilityData = new SerializedObject(abilities);
            abilityData.FindProperty("ability1Duration").floatValue = clips[4].length + .1f;
            abilityData.FindProperty("ability2Duration").floatValue = clips[5].length + .1f;
            abilityData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("WIND_READY: " + string.Join(", ", clips.Select(c => c.name + "=" + c.length.ToString("F2") + "s")));
    }

    [MenuItem("Tools/Verde/Agregar viento al selector")]
    public static void ConfigureSelector()
    {
        var selector = UnityEngine.Object.FindAnyObjectByType<TestCharacterSelector>();
        if (!selector) throw new InvalidOperationException("Abrir la escena del selector primero.");
        var data = new SerializedObject(selector);
        var slots = data.FindProperty("characters");
        if (slots.arraySize < 3) slots.arraySize = 3;
        var slot = slots.GetArrayElementAtIndex(2);
        slot.FindPropertyRelative("displayName").stringValue = "Viento";
        slot.FindPropertyRelative("description").stringValue = "Pirata de viento con ropa dinámica, caminata, carrera, salto y dos movimientos especiales.";
        slot.FindPropertyRelative("controls").stringValue = "WASD · Mover   Shift · Correr   Espacio · Saltar   1/2 · Habilidades   3 · Muerte   0 · Revivir   Esc · Selector";
        slot.FindPropertyRelative("accent").colorValue = new Color(.3f, .9f, .55f);
        slot.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
        EditorSceneManager.SaveScene(selector.gameObject.scene);
    }
}
