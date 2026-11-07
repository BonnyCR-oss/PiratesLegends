using System;
using System.Linq;
using PirateLegends.Player;
using PirateLegends.Frostbound;
using PirateLegends.CameraSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FrostboundIntegration
{
    const string Root = "Assets/_PirateLegends/Frostbound/";
    public const string ScenePath = "Assets/_PirateLegends/Scenes/Tests/SCN_Frostbound_AbilitiesTest.unity";
    public const string PlayerPath = Root + "Prefabs/PF_Player_Frostbound.prefab";

    [MenuItem("Tools/Frostbound/Crear prueba de habilidades")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Salir de Play y guardar la escena actual primero.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath)) throw new InvalidOperationException("La integración ya existe; abrir su escena de prueba.");
        var clips = ImportClips();
        var controller = MakeController(clips);
        var ice = MakeMaterial("Ice",new Color(.18f,.65f,.88f),true);
        var glow = MakeMaterial("IceGlow",new Color(.3f,.9f,1),false);
        var bodyMat = MakeMaterial("Frostbound",Color.white,true);
        bodyMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/Frostbound_BaseColor.png"));
        bodyMat.SetFloat("_Cull",0);
        var bow = MakeEffect("IceBow",ice,Quaternion.identity,.9f,false);
        var arrow = MakeEffect("IceArrow",ice,Quaternion.identity,.85f,false);
        var lance = MakeEffect("IceLance",ice,Quaternion.Euler(90,0,0),1.2f,false);
        var spike = MakeEffect("IceSpike",ice,Quaternion.identity,1,true);
        var fragment = MakeEffect("IceFragment",glow,Quaternion.identity,5,false);

        var root = new GameObject("PF_Player_Frostbound");
        try
        {
            root.tag="Player";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PirateLegends/Prefabs/Characters/Frostbound_Con_Ropa.prefab");
            if(!source) throw new InvalidOperationException("Falta el prefab Frostbound con Cloth existente.");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(source); visual.transform.SetParent(root.transform,false);
            var animator=visual.GetComponentInChildren<Animator>();
            if(!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman) throw new InvalidOperationException("Avatar del personaje inválido.");
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            clips[0].SampleAnimation(visual,0);
            var renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds=renderers[0].bounds;
            foreach(var r in renderers) { bounds.Encapsulate(r.bounds); r.sharedMaterials=Enumerable.Repeat(bodyMat,r.sharedMaterials.Length).ToArray(); }
            float scale=1.85f/bounds.size.y; visual.transform.localScale*=scale;
            bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            visual.transform.localPosition-=Vector3.up*bounds.min.y;
            var body=root.AddComponent<CharacterController>(); body.height=1.85f;body.center=Vector3.up*.925f;body.radius=.27f;body.skinWidth=.025f;body.stepOffset=.3f;
            root.AddComponent<PlayerInputReader>(); var motor=root.AddComponent<PlayerLocomotion>();
            var serialized=new SerializedObject(motor);serialized.FindProperty("animator").objectReferenceValue=animator;serialized.ApplyModifiedPropertiesWithoutUndo();
            var skills=root.AddComponent<FrostboundAbilities>();skills.animator=animator;
            skills.bowPrefab=bow;skills.arrowPrefab=arrow;skills.lancePrefab=lance;skills.spikePrefab=spike;skills.fragmentPrefab=fragment;
            skills.iceMaterial=ice;skills.glowMaterial=glow;
            skills.clip1Length=clips[4].length;skills.clip2Length=clips[5].length;skills.clip3Length=clips[6].length;
            PrefabUtility.SaveAsPrefabAsset(root,PlayerPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        CreateScene();
        AssetDatabase.SaveAssets();
        Debug.Log("FROSTBOUND_READY: "+ScenePath+" | "+string.Join(", ",clips.Select(c=>c.name+"="+c.length.ToString("F3"))));
    }
    static AnimationClip[] ImportClips()
    {
        string[] files={"Idleforunity.fbx","Frostbound_Optimizado_Mixamo@Walking.fbx","Fast Run.fbx","Jump.fbx","Frostbound_Habilidad_1.fbx","Frostbound_Habilidad_2.fbx","Frostbound_Habilidad_3.fbx"};
        string[] names={"Idle","Walk","Run","Jump","Ability1","Ability2","Ability3"};
        var clips=new AnimationClip[files.Length];
        for(int i=0;i<files.Length;i++)
        {
            string path=Root+"Animations/"+files[i];
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(!importer) throw new InvalidOperationException("No se importó "+path);
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.optimizeGameObjects=false;
            var settings=importer.defaultClipAnimations;
            if(settings.Length!=1) throw new InvalidOperationException("Se esperaba un clip en "+path+"; encontrados "+settings.Length);
            settings[0].name=names[i];settings[0].loopTime=i<3;settings[0].loopPose=i<3;
            settings[0].lockRootRotation=true;settings[0].keepOriginalOrientation=true;
            settings[0].lockRootPositionXZ=true;settings[0].lockRootHeightY=true;settings[0].heightFromFeet=true;
            importer.clipAnimations=settings;importer.SaveAndReimport();
            var assets=AssetDatabase.LoadAllAssetsAtPath(path);
            var avatar=assets.OfType<Avatar>().FirstOrDefault();
            clips[i]=assets.OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            if(!avatar || !avatar.isValid || !avatar.isHuman || !clips[i].humanMotion || clips[i].length<.1f) throw new InvalidOperationException("Clip/Avatar Humanoid inválido: "+path);
            Debug.Log("FROSTBOUND_CLIP "+names[i]+" frames="+settings[0].firstFrame+".."+settings[0].lastFrame+" length="+clips[i].length+" human="+clips[i].humanMotion);
        }
        return clips;
    }
    static AnimatorController MakeController(AnimationClip[] clips)
    {
        string path=Root+"Animations/AC_Frostbound.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path)) throw new InvalidOperationException("El controller ya existe.");
        var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Jump",AnimatorControllerParameterType.Trigger);controller.AddParameter("Attack",AnimatorControllerParameterType.Trigger);
        var machine=controller.layers[0].stateMachine;var locomotion=machine.AddState("Locomotion");machine.defaultState=locomotion;
        var tree=new BlendTree{name="Frostbound Idle Walk Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(clips[0],0);tree.AddChild(clips[1],1);tree.AddChild(clips[2],3);locomotion.motion=tree;
        var jump=machine.AddState("Jump");jump.motion=clips[3];
        var enter=locomotion.AddTransition(jump);enter.hasExitTime=false;enter.duration=.08f;enter.AddCondition(AnimatorConditionMode.If,0,"Jump");
        var exit=jump.AddTransition(locomotion);exit.hasExitTime=true;exit.exitTime=.95f;exit.duration=.1f;
        for(int i=4;i<7;i++){var state=machine.AddState("Ability"+(i-3));state.motion=clips[i];}
        return controller;
    }
    static Material MakeMaterial(string name,Color color,bool lit)
    {
        string path=Root+"Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find(lit?"Universal Render Pipeline/Lit":"Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);
        if(lit){material.SetFloat("_Smoothness",.7f);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",name=="Ice"?color*.25f:Color.black);}
        return material;
    }
    static GameObject MakeEffect(string name,Material material,Quaternion rotation,float scale,bool sitOnGround)
    {
        var root=new GameObject("PF_"+name);
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+name+".fbx"));
            model.transform.SetParent(root.transform,false);model.transform.localRotation=rotation;model.transform.localScale*=scale;
            foreach(var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
            if(sitOnGround){var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.localPosition-=Vector3.up*b.min.y;}
            return PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/PF_"+name+".prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    public static void CreateScene()
    {
        if(System.IO.File.Exists(ScenePath)) throw new InvalidOperationException("La escena de prueba ya existe; no se sobrescribe.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.5f,.65f);
        var floor=MakeMaterial("ArenaFloor",new Color(.07f,.12f,.19f),true);floor.SetFloat("_Smoothness",.18f);
        var grid=MakeMaterial("ArenaLines",new Color(.15f,.36f,.48f),false);
        var targetMat=MakeMaterial("Target",new Color(1,.36f,.15f),true);
        var wallMat=MakeMaterial("Cover",new Color(.23f,.3f,.4f),true);
        Primitive("Arena_Ground",PrimitiveType.Cube,new Vector3(0,-.25f,5),new Vector3(42,.5f,42),floor);
        for(int i=-4;i<=6;i++)
        {
            var x=Primitive("Grid_X_"+i,PrimitiveType.Cube,new Vector3(i*3,.005f,5),new Vector3(.025f,.01f,40),grid);UnityEngine.Object.DestroyImmediate(x.GetComponent<Collider>());
            var z=Primitive("Grid_Z_"+i,PrimitiveType.Cube,new Vector3(0,.005f,i*3),new Vector3(40,.01f,.025f),grid);UnityEngine.Object.DestroyImmediate(z.GetComponent<Collider>());
        }
        var light=new GameObject("Directional Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(42,-28,0);
        var camera=new GameObject("Frostbound_TestCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=60;camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.05f,.1f);camera.gameObject.AddComponent<AudioListener>();
        camera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));player.name="Player_Frostbound";player.transform.position=new Vector3(0,.05f,-7);
        var skills=player.GetComponent<FrostboundAbilities>();skills.aimCamera=camera;
        camera.transform.position=new Vector3(0,2.4f,-11);camera.transform.rotation=Quaternion.Euler(15,0,0);
        Vector3[] positions={new Vector3(-3,1,3),new Vector3(0,1,5),new Vector3(3,1,8),new Vector3(6,1,9)};
        for(int i=0;i<positions.Length;i++)
        {
            var target=Primitive("Blanco_"+(i+1),PrimitiveType.Capsule,positions[i],Vector3.one,targetMat);
            target.AddComponent<FrostboundHealth>();
            // A second collider verifies damage deduplication by the health owner.
            var extra=new GameObject("SecondaryCollider");extra.transform.SetParent(target.transform,false);extra.AddComponent<SphereCollider>().radius=.55f;
        }
        Primitive("Muro_Cobertura",PrimitiveType.Cube,new Vector3(6,1.5f,5),new Vector3(3,3,.6f),wallMat);
        EditorSceneManager.SaveScene(scene,ScenePath);
        Selection.activeGameObject=player;SceneView.lastActiveSceneView?.FrameSelected();
    }
}
