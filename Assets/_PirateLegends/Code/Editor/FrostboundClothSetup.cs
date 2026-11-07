using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Editor-only setup. The saved prefab needs no custom runtime script.
public static class FrostboundClothSetup
{
    const string ModelName = "Frostbound_Unity_Cloth";
    const float MaxTravel = 0.22f;

    [MenuItem("Tools/Frostbound/Crear prefab con ropa")]
    public static void CreateFromSelection()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || Path.GetFileNameWithoutExtension(path) != ModelName)
            throw new InvalidOperationException("Selecciona Frostbound_Unity_Cloth.fbx en Project.");
        string prefab = Build(path);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("Frostbound listo: arrastra " + prefab + " a la escena y pulsa Play.");
    }

    public static string Build(string modelPath)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("No se encontro el FBX.");
        importer.isReadable = true;
        importer.optimizeGameObjects = false;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips) clip.loopTime = true;
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        string folder = Path.GetDirectoryName(Path.GetDirectoryName(modelPath)).Replace('\\', '/');
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Frostbound_Con_Ropa";
        try
        {
            var animator = instance.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("El Avatar humanoide no es valido; revisar Rig antes de continuar.");
            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("HDRP/Lit");
            if (shader == null) throw new InvalidOperationException("No se encontro un shader Lit para el material.");
            string materialPath = folder + "/Materials/Frostbound_Material.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Textures/Frostbound_BaseColor.png");
            if (texture == null) throw new InvalidOperationException("Falta Frostbound_BaseColor.png.");
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColorMap")) material.SetTexture("_BaseColorMap", texture);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0); // Thin coat panels are two-sided.
            if (material.HasProperty("_DoubleSidedEnable")) material.SetFloat("_DoubleSidedEnable", 1);

            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var renderer = renderers.Single(r => r.name == "Frostbound_Faldones");
            foreach (var r in renderers)
            {
                r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                var bounds = r.localBounds;
                bounds.Expand(0.6f);
                r.localBounds = bounds;
                r.updateWhenOffscreen = true;
            }

            var cloth = renderer.gameObject.AddComponent<Cloth>();
            AssignConstraints(renderer, cloth);
            cloth.useGravity = true;
            cloth.useTethers = true;
            cloth.stretchingStiffness = 1.0f;
            cloth.bendingStiffness = 0.35f;
            cloth.damping = 0.45f;
            cloth.friction = 0.3f;
            cloth.clothSolverFrequency = 120;
            cloth.worldVelocityScale = 0.5f;
            cloth.worldAccelerationScale = 0.5f;
            cloth.enableContinuousCollision = true;
            cloth.useVirtualParticles = 1;
            cloth.randomAcceleration = Vector3.zero;
            cloth.externalAcceleration = Vector3.zero;
            float height = renderers.Aggregate(new Bounds(instance.transform.position, Vector3.zero),
                (b, r) => { b.Encapsulate(r.bounds); return b; }).size.y;
            float scale = height / 1.8f;
            var colliders = new List<CapsuleCollider>();
            AddCapsule(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.075f * scale, colliders);
            AddCapsule(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.075f * scale, colliders);
            AddCapsule(animator, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.055f * scale, colliders);
            AddCapsule(animator, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.055f * scale, colliders);
            cloth.capsuleColliders = colliders.ToArray();

            var animation = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (animation == null) throw new InvalidOperationException("No se encontro la caminata en el FBX.");
            string controllerPath = folder + "/Animations/Frostbound_Walking.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Walking");
            if (state == null) state = machine.AddState("Walking");
            state.motion = animation;
            machine.defaultState = state;
            animator.runtimeAnimatorController = controller;

            string prefabPath = "Assets/_PirateLegends/Prefabs/Characters/Frostbound_Con_Ropa.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("Cloth configurado: " + cloth.coefficients.Length + " particulas, " +
                cloth.coefficients.Count(c => c.maxDistance == 0) + " ancladas, " + colliders.Count + " colisionadores.");
            return prefabPath;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }

    static void AssignConstraints(SkinnedMeshRenderer renderer, Cloth cloth)
    {
        // Cloth welds UV seams, so its particle indices cannot index mesh.colors directly.
        var mesh = renderer.sharedMesh;
        var colors = mesh.colors;
        if (colors.Length != mesh.vertexCount) throw new InvalidOperationException("Faltan colores Cloth_MaxDistance.");
        var baked = new Mesh();
        renderer.BakeMesh(baked);
        try
        {
            var particles = cloth.vertices;
            var source = mesh.vertices;
            var posed = baked.vertices;
            // Depending on import transforms, cloth exposes its initial positions in renderer or root-bone space.
            var candidates = new List<Vector3[]> { source, posed,
                posed.Select(renderer.transform.TransformPoint).ToArray() };
            if (renderer.rootBone != null)
            {
                candidates.Add(posed.Select(v => renderer.rootBone.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray());
                // BakeMesh already includes the FBX mesh scale. Apply only rotation and
                // translation before converting to Cloth's root-bone coordinates.
                candidates.Add(posed.Select(v => renderer.rootBone.InverseTransformPoint(
                    renderer.transform.position + renderer.transform.rotation * v)).ToArray());
            }
            int[] bestMap = null;
            double bestError = double.PositiveInfinity;
            float bestMaxError = float.PositiveInfinity;
            foreach (var points in candidates)
            {
                var map = new int[particles.Length];
                double error = 0;
                float maxError = 0;
                for (int i = 0; i < particles.Length; i++)
                {
                    float closest = float.PositiveInfinity;
                    for (int j = 0; j < points.Length; j++)
                    {
                        float d = (particles[i] - points[j]).sqrMagnitude;
                        if (d < closest) { closest = d; map[i] = j; }
                    }
                    error += closest;
                    maxError = Mathf.Max(maxError, closest);
                }
                if (error < bestError) { bestError = error; bestMap = map; bestMaxError = maxError; }
            }
            float tolerance = Mathf.Max(mesh.bounds.size.magnitude * 0.0001f, 0.00001f);
            if (particles.Length == 0 || bestMap == null || bestMaxError > tolerance * tolerance)
                throw new InvalidOperationException("No se pudo relacionar la malla con las particulas de Cloth; no se genero un prefab incorrecto.");
            var coefficients = cloth.coefficients;
            for (int i = 0; i < coefficients.Length; i++)
            {
                coefficients[i].maxDistance = Mathf.Clamp01(colors[bestMap[i]].r) * MaxTravel;
                coefficients[i].collisionSphereDistance = 0;
            }
            if (!coefficients.Any(c => c.maxDistance == 0) || !coefficients.Any(c => c.maxDistance > 0))
                throw new InvalidOperationException("La ropa necesita anclajes y vertices libres.");
            cloth.coefficients = coefficients;
        }
        finally { UnityEngine.Object.DestroyImmediate(baked); }
    }

    static void AddCapsule(Animator animator, HumanBodyBones start, HumanBodyBones end, float radius, List<CapsuleCollider> result)
    {
        Transform a = animator.GetBoneTransform(start), b = animator.GetBoneTransform(end);
        if (a == null || b == null) throw new InvalidOperationException("Faltan huesos de piernas.");
        var child = new GameObject("ClothCollider_" + start);
        child.transform.SetParent(a, false);
        child.transform.position = (a.position + b.position) * 0.5f;
        child.transform.rotation = Quaternion.FromToRotation(Vector3.up, b.position - a.position);
        var capsule = child.AddComponent<CapsuleCollider>();
        var s = child.transform.lossyScale;
        capsule.radius = radius / Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        capsule.height = Mathf.Max(2 * capsule.radius, Vector3.Distance(a.position, b.position) * 0.88f / Mathf.Abs(s.y));
        capsule.direction = 1;
        capsule.isTrigger = true;
        result.Add(capsule);
    }

    // Optional batch validation in an isolated Unity project.
    public static void ValidateBatch()
    {
        try
        {
            var prefabPath = Build("Assets/_PirateLegends/Art/Characters/iceCharacter/Models/Frostbound_Unity_Cloth.fbx");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var cloth = prefab.GetComponentInChildren<Cloth>(true);
            if (cloth == null || cloth.capsuleColliders.Length != 4)
                throw new InvalidOperationException("Validation failed: missing cloth or colliders.");
            File.WriteAllText("validation-result.txt", "PASS: " + prefabPath + "\nParticles: " + cloth.coefficients.Length);
            AssetDatabase.ExportPackage(new[] { "Assets/_PirateLegends/Art/Characters/iceCharacter", prefabPath }, "Frostbound_Unity_Cloth.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
