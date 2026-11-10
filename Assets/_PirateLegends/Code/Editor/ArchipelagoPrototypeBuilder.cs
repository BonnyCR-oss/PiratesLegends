using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using PirateLegends.Player;
using PirateLegends.CameraSystem;
using PirateLegends.World;

public static class ArchipelagoPrototypeBuilder
{
    const string Root = "Assets/_PirateLegends/Art/Environment/Archipelago";
    public const string ScenePath = "Assets/_PirateLegends/Scenes/Ocean/SCN_Archipelago_Prototype.unity";
    static System.Random rng;
    static float R(float a,float b) { return Mathf.Lerp(a,b,(float)rng.NextDouble()); }
    static float Smooth(float a,float b,float v) { return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v)); }
    static void Folder(string path) { if(AssetDatabase.IsValidFolder(path))return; int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1)); }
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(value,path);return value;
    }
    static Material Mat(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.18f);
        return Save(m,Root+"/Materials/"+name+".mat");
    }
    public static float Height(float x,float z)
    {
        float r=Mathf.Sqrt(x*x/(100*100)+z*z/(89*89));
        float h=-8+18*(1-Smooth(.48f,1.22f,r));
        h+=23*Mathf.Exp(-((x+25)*(x+25)/750f+(z-22)*(z-22)/650f));
        h+=9*Mathf.Exp(-((x-37)*(x-37)/440f+(z-8)*(z-8)/550f));
        h+=1.1f*(Mathf.PerlinNoise((x+210)*.045f,(z+310)*.045f)-.5f)*(1-Smooth(.8f,1.1f,r));
        h-=4.2f*Mathf.Exp(-((x-15)*(x-15)/450f+(z+65)*(z+65)/530f));
        float dockPath=(1-Smooth(4,9,Mathf.Abs(x-8)))*Smooth(-84,-74,z)*(1-Smooth(-58,-48,z));
        float approach=Mathf.Lerp(1.8f,4.4f,Mathf.InverseLerp(-75,-53,z));
        return Mathf.Lerp(h,approach,dockPath);
    }
    static Texture2D Texture(string name,Color a,Color b)
    {
        var t=new Texture2D(128,128,TextureFormat.RGB24,false);t.name=name;
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)
        {
            float n=.6f*Mathf.PerlinNoise(x*.07f,y*.07f)+.4f*Mathf.PerlinNoise(x*.3f+8,y*.3f+8);
            t.SetPixel(x,y,Color.Lerp(a,b,n));
        }
        t.Apply();string path=Root+"/Textures/"+name+".png";System.IO.File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.wrapMode=TextureWrapMode.Repeat;imp.maxTextureSize=256;imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static TerrainLayer Layer(string name,Color a,Color b,float tile)
    {
        var l=new TerrainLayer { diffuseTexture=Texture(name,a,b), tileSize=new Vector2(tile,tile),smoothness=0,metallic=0};
        return Save(l,Root+"/Terrain/"+name+".terrainlayer");
    }
    static GameObject Group(string name,Transform parent=null)
    {var g=new GameObject(name);if(parent)g.transform.SetParent(parent,false);return g;}
    static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material material)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;return g;}
    static GameObject Cylinder(string name,Transform parent,Vector3 pos,Vector3 scale,Material material)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;return g;}
    static Mesh OceanMesh()
    {
        int n=160;var v=new Vector3[(n+1)*(n+1)];var tri=new int[n*n*6];
        for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)v[z*(n+1)+x]=new Vector3((x/(float)n-.5f)*2400,0,(z/(float)n-.5f)*2400);
        int k=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*(n+1)+x;tri[k++]=i;tri[k++]=i+n+1;tri[k++]=i+1;tri[k++]=i+1;tri[k++]=i+n+1;tri[k++]=i+n+2;}
        var m=new Mesh{name="MSH_PrototypeOcean"};m.vertices=v;m.triangles=tri;m.RecalculateNormals();m.RecalculateBounds();var b=m.bounds;b.Expand(Vector3.up*2);m.bounds=b;
        return Save(m,Root+"/Meshes/MSH_PrototypeOcean.asset");
    }
    static Mesh Frond()
    {
        int n=10;var v=new Vector3[(n+1)*2];var tr=new int[n*6];
        for(int i=0;i<=n;i++){float t=i/(float)n;float width=.6f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.65f);float y=.85f*Mathf.Sin(t*Mathf.PI)-1.1f*t*t;v[i*2]=new Vector3(-width,y,t*3.8f);v[i*2+1]=new Vector3(width,y+.06f,t*3.8f);}
        int k=0;for(int i=0;i<n;i++){int a=i*2;int[] q={a,a+2,a+1,a+1,a+2,a+3};foreach(int j in q)tr[k++]=j;}
        var m=new Mesh{name="MSH_PalmFrond"};m.vertices=v;m.triangles=tr;m.RecalculateNormals();m.RecalculateBounds();return Save(m,Root+"/Meshes/MSH_PalmFrond.asset");
    }
    static Mesh Rock()
    {
        int n=8;var v=new Vector3[n*4+2];v[0]=new Vector3(0,-.6f,0);v[v.Length-1]=new Vector3(.1f,.85f,.07f);
        for(int row=0;row<4;row++)for(int i=0;i<n;i++){float ang=i*Mathf.PI*2/n;float rad=(row==0?.6f:row==3?.42f:1)*R(.82f,1.13f);v[1+row*n+i]=new Vector3(Mathf.Cos(ang)*rad,-.52f+row*.4f,Mathf.Sin(ang)*rad);}
        var tr=new System.Collections.Generic.List<int>();
        for(int i=0;i<n;i++){int j=(i+1)%n;tr.AddRange(new[]{0,1+j,1+i});for(int row=0;row<3;row++){int a=1+row*n+i,b=1+row*n+j;tr.AddRange(new[]{a,b,a+n,b,b+n,a+n});}tr.AddRange(new[]{1+3*n+i,1+3*n+j,v.Length-1});}
        for(int i=0;i<tr.Count;i+=3){int swap=tr[i];tr[i]=tr[i+1];tr[i+1]=swap;}
        var m=new Mesh{name="MSH_PrototypeRock"};m.vertices=v;m.triangles=tr.ToArray();m.RecalculateNormals();m.RecalculateBounds();return Save(m,Root+"/Meshes/MSH_PrototypeRock.asset");
    }
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material mat,Vector3 pos,Vector3 scale,bool collision)
    {var g=Group(name,parent);g.transform.localPosition=pos;g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collision)g.AddComponent<MeshCollider>().sharedMesh=mesh;return g;}
    static readonly System.Collections.Generic.Dictionary<Material,Material> Converted=new System.Collections.Generic.Dictionary<Material,Material>();
    static void ShipMaterials(GameObject ship)
    {
        foreach(var r in ship.GetComponentsInChildren<Renderer>())
        {
            var mats=r.sharedMaterials;
            for(int j=0;j<mats.Length;j++)
            {
                var src=mats[j];if(!src)continue;
                if(!Converted.TryGetValue(src,out var dst))
                {
                    dst=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=src.name+"_URP"};
                    if(src.HasProperty("_MainTex"))dst.SetTexture("_BaseMap",src.GetTexture("_MainTex"));
                    if(src.HasProperty("_Color"))dst.SetColor("_BaseColor",src.GetColor("_Color"));
                    if(src.HasProperty("_BumpMap")&&src.GetTexture("_BumpMap")){dst.SetTexture("_BumpMap",src.GetTexture("_BumpMap"));dst.EnableKeyword("_NORMALMAP");}
                    if(src.HasProperty("_MetallicGlossMap")&&src.GetTexture("_MetallicGlossMap")){dst.SetTexture("_MetallicGlossMap",src.GetTexture("_MetallicGlossMap"));dst.EnableKeyword("_METALLICSPECGLOSSMAP");}
                    dst.SetFloat("_Smoothness",.25f);
                    if(src.name.Contains("Sails")){dst.SetFloat("_Cull",0);dst.SetFloat("_AlphaClip",1);dst.SetFloat("_Cutoff",.35f);dst.EnableKeyword("_ALPHATEST_ON");dst.renderQueue=2450;dst.doubleSidedGI=true;}
                    dst=Save(dst,Root+"/Materials/"+dst.name+".mat");Converted[src]=dst;
                }
                mats[j]=dst;
            }
            r.sharedMaterials=mats;
        }
    }
    static void Prop(string asset,string name,Transform parent,Vector3 pos,float size)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stylized_Pirate_Ship/"+asset+".prefab");if(!src)return;
        var g=(GameObject)PrefabUtility.InstantiatePrefab(src);g.name=name;g.transform.SetParent(parent,false);
        var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        float s=size/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));g.transform.localScale=Vector3.one*s;
        g.transform.position=pos-new Vector3(b.center.x*s,b.min.y*s,b.center.z*s);ShipMaterials(g);
    }

    [MenuItem("Tools/Pirate Legends/Crear bahía inicial")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Salir de Play antes de construir.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Guardar la escena actual antes de construir.");
        rng=new System.Random(1217);Converted.Clear();
        foreach(var p in new[]{Root+"/Materials",Root+"/Meshes",Root+"/Textures",Root+"/Terrain","Assets/_PirateLegends/Scenes/Ocean"})Folder(p);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var land=Group("01_Isla_Terrain");var sea=Group("02_Mar");var harbor=Group("03_Puerto_y_barco");var nature=Group("04_Rocas_y_palmeras");var gameplay=Group("05_Jugador_y_camaras");var lighting=Group("06_Luz_y_atmosfera");
        var sand=Layer("TL_Sand",new Color(.73f,.62f,.4f),new Color(.94f,.85f,.6f),8);
        var grass=Layer("TL_Grass",new Color(.18f,.31f,.12f),new Color(.4f,.49f,.22f),10);
        var stone=Layer("TL_Rock",new Color(.26f,.29f,.28f),new Color(.44f,.44f,.37f),12);
        var data=new TerrainData{name="TD_StarterIsland",heightmapResolution=257,alphamapResolution=256,size=new Vector3(256,64,256)};
        var h=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)h[z,x]=(Height(x-128,z-128)+12)/64;
        data.SetHeights(0,0,h);data.terrainLayers=new[]{sand,grass,stone};
        var alpha=new float[256,256,3];
        for(int z=0;z<256;z++)for(int x=0;x<256;x++)
        {
            float wx=x/255f*256-128,wz=z/255f*256-128,y=Height(wx,wz);
            float dx=(Height(wx+1,wz)-Height(wx-1,wz))*.5f,dz=(Height(wx,wz+1)-Height(wx,wz-1))*.5f;
            float slope=Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
            float rock=Smooth(25,47,slope)*.8f+Smooth(24,34,y)*.3f;rock=Mathf.Clamp01(rock);
            float pathX=8-.32f*(wz+58)+5*Mathf.Sin((wz+58)*.045f);
            float path=(1-Smooth(1.5f,3.8f,Mathf.Abs(wx-pathX)))*Smooth(-64,-52,wz)*(1-Smooth(22,40,wz));
            float beach=1-Smooth(2.3f,5.8f,y);float sandWeight=Mathf.Max(beach,path*.9f)*(1-rock);
            alpha[z,x,0]=sandWeight;alpha[z,x,1]=1-sandWeight-rock;alpha[z,x,2]=rock;
        }
        data=Save(data,Root+"/Terrain/TD_StarterIsland.asset");data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);
        var tg=Terrain.CreateTerrainGameObject(data);tg.name="Isla_Alba";tg.transform.SetParent(land.transform);tg.transform.position=new Vector3(-128,-12,-128);
        var terrain=tg.GetComponent<Terrain>();terrain.heightmapPixelError=4;terrain.basemapDistance=500;terrain.drawInstanced=true;
        var terrainMat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));terrain.materialTemplate=Save(terrainMat,Root+"/Materials/MAT_IslandTerrain.mat");
        data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);
        var water=MeshObject("Mar_animado",sea.transform,OceanMesh(),Save(new Material(Shader.Find("PirateLegends/PrototypeOcean")),Root+"/Materials/MAT_PrototypeOcean.mat"),Vector3.zero,Vector3.one,false);
        water.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        var wood=Mat("MAT_DockWood",new Color(.32f,.19f,.095f));
        var paleWood=Mat("MAT_DockBoards",new Color(.46f,.30f,.15f));
        var rockMat=Mat("MAT_CoastalRock",new Color(.34f,.37f,.32f));
        var trunkMat=Mat("MAT_PalmTrunk",new Color(.35f,.23f,.11f));
        var leafMat=Mat("MAT_PalmLeaf",new Color(.19f,.38f,.12f));
        leafMat.SetFloat("_Cull",0);EditorUtility.SetDirty(leafMat);
        var dock=Group("Muelle_modular",harbor.transform);
        for(int i=0;i<29;i++)Cube("Tablon_"+i,dock.transform,new Vector3(8,1.9f,-71-i),new Vector3(4,.22f,.92f),i%4==0?wood:paleWood);
        for(int i=0;i<8;i++)foreach(float x in new[]{6.25f,9.75f})
        {
            Cylinder("Pilote",dock.transform,new Vector3(x,-.6f,-71-i*4),new Vector3(.25f,3.5f,.25f),wood);
            if(i<7)Cube("Baranda",dock.transform,new Vector3(x,2.65f,-73-i*4),new Vector3(.13f,.13f,3.8f),wood);
        }
        var ship=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stylized_Pirate_Ship/StylShip_Unity.prefab"));
        ship.name="Barco_pirata_fondeado";ship.transform.SetParent(harbor.transform);ship.transform.SetPositionAndRotation(new Vector3(16,3.8f,-106),Quaternion.Euler(0,90,0));ship.transform.localScale=Vector3.one*.65f;ShipMaterials(ship);
        var plank=Cube("Pasarela_al_barco",dock.transform,new Vector3(8,1.65f,-101),new Vector3(1.5f,.16f,4.8f),paleWood);plank.transform.rotation=Quaternion.Euler(-7,0,0);
        Prop("StylShip_Barrel","Barriles_del_muelle",harbor.transform,new Vector3(6.9f,2.02f,-73),1.1f);
        Prop("StylShip_Box","Cajon_del_muelle",harbor.transform,new Vector3(9,2.02f,-77),.8f);
        Prop("StylShip_Pallet","Suministros_playa",harbor.transform,new Vector3(15,Height(15,-60),-60),1.8f);
        var frond=Frond();var rockMesh=Rock();
        int palmCount=0;
        for(int i=0;i<100&&palmCount<20;i++)
        {
            float x=R(-77,78),z=R(-65,65),y=Height(x,z);
            if(y<3.4f||y>11||Mathf.Abs(x-8)<9&&z<-35||terrain.terrainData.GetSteepness((x+128)/256,(z+128)/256)>22)continue;
            var palm=Group("Palmera_"+palmCount,nature.transform);palm.transform.position=new Vector3(x,y,z);palm.transform.rotation=Quaternion.Euler(0,R(0,360),0);
            float height=R(4.5f,6.5f);var trunk=Cylinder("Tronco",palm.transform,new Vector3(.15f,height*.5f,0),new Vector3(.27f,height*.5f,.27f),trunkMat);trunk.transform.localRotation=Quaternion.Euler(0,0,-5);
            var crown=Group("Copa",palm.transform);crown.transform.localPosition=new Vector3(.4f,height,0);
            for(int j=0;j<7;j++){var leaf=MeshObject("Hoja",crown.transform,frond,leafMat,Vector3.zero,Vector3.one*R(.8f,1.1f),false);leaf.transform.localRotation=Quaternion.Euler(R(-8,12),j*360f/7+R(-8,8),0);}
            palmCount++;
        }
        for(int i=0;i<42;i++)
        {
            float angle=R(0,Mathf.PI*2),radius=R(.63f,.96f);float x=Mathf.Cos(angle)*100*radius,z=Mathf.Sin(angle)*89*radius,y=Height(x,z);
            if(y<-.8f||Mathf.Abs(x-8)<13&&z<-45)continue;
            float s=R(1.2f,4.2f);var rock=MeshObject("Roca_costera_"+i,nature.transform,rockMesh,rockMat,new Vector3(x,y+s*.2f,z),new Vector3(s,R(.9f,2)*s,s*R(.7f,1.2f)),true);rock.transform.localRotation=Quaternion.Euler(0,R(0,360),0);
        }
        // Distant silhouettes establish a direction for future islands.
        for(int i=0;i<3;i++){float x=240+i*24,z=160+i*13;MeshObject("Islote_lejano_"+i,nature.transform,rockMesh,rockMat,new Vector3(x,2,z),new Vector3(22-i*3,22-i*4,20),false);}
        var sun=Group("Sol",lighting.transform).AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.color=new Color(1,.91f,.77f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(43,-35,0);RenderSettings.sun=sun;
        var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.37f,.64f,.83f));sky.SetColor("_GroundColor",new Color(.38f,.48f,.48f));sky.SetFloat("_AtmosphereThickness",.8f);sky.SetFloat("_SunSize",.035f);RenderSettings.skybox=Save(sky,Root+"/Materials/MAT_ArchipelagoSky.mat");
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.55f,.7f,.81f);RenderSettings.ambientEquatorColor=new Color(.45f,.52f,.49f);RenderSettings.ambientGroundColor=new Color(.27f,.29f,.24f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.58f,.73f,.79f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0017f;
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PirateLegends/Prefabs/Characters/PF_Player_Fire.prefab"));
        player.name="Player_Fire_Exploracion";player.transform.SetParent(gameplay.transform);var spawn=new Vector3(8,Height(8,-58)+.12f,-58);player.transform.SetPositionAndRotation(spawn,Quaternion.Euler(0,180,0));
        var cameraGo=Group("Main Camera",gameplay.transform);cameraGo.tag="MainCamera";var cam=cameraGo.AddComponent<Camera>();cam.fieldOfView=60;cam.nearClipPlane=.08f;cam.farClipPlane=1800;cameraGo.AddComponent<AudioListener>();
        var cameraData=cameraGo.AddComponent<UniversalAdditionalCameraData>();cameraData.requiresDepthTexture=true;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var target=Group("CameraTarget_Exploracion",player.transform);target.transform.localPosition=Vector3.up*1.45f;
        var orbit=cameraGo.AddComponent<ThirdPersonOrbitCamera>();orbit.Configure(target.transform,player.GetComponent<PlayerInputReader>(),player.transform);
        var cs=new SerializedObject(orbit);cs.FindProperty("distance").floatValue=5.5f;cs.FindProperty("pitch").floatValue=14;cs.ApplyModifiedPropertiesWithoutUndo();
        cam.transform.SetPositionAndRotation(spawn+new Vector3(0,3,5.5f),Quaternion.Euler(14,180,0));
        var guide=player.AddComponent<IslandPrototypeGuide>();var gs=new SerializedObject(guide);gs.FindProperty("safePosition").vector3Value=spawn;gs.FindProperty("island").objectReferenceValue=terrain;gs.ApplyModifiedPropertiesWithoutUndo();
        var overview=Group("Camera_Vista_general",gameplay.transform).AddComponent<Camera>();overview.enabled=false;overview.fieldOfView=50;overview.farClipPlane=1800;overview.transform.position=new Vector3(155,132,-202);overview.transform.LookAt(new Vector3(0,5,-16));overview.gameObject.AddComponent<UniversalAdditionalCameraData>().requiresDepthTexture=true;
        Physics.SyncTransforms();DynamicGI.UpdateEnvironment();
        // Paint last: later asset imports can reset unsaved native terrain splat textures.
        data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);
        var coastTex=new Texture2D(257,257,TextureFormat.RGBA32,false,true){name="T_IslandHeight",wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[257*257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)pixels[z*257+x]=new Color(h[z,x],0,0,1);
        coastTex.SetPixels(pixels);coastTex.Apply();coastTex=Save(coastTex,Root+"/Textures/T_IslandHeight.asset");
        water.GetComponent<Renderer>().sharedMaterial.SetTexture("_IslandHeight",coastTex);EditorUtility.SetDirty(water.GetComponent<Renderer>().sharedMaterial);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();Selection.activeGameObject=land;
        EditorApplication.delayCall += FinishTerrain;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0,0,-12),Quaternion.Euler(40,-35,0),170);
        Debug.Log("ARCHIPELAGO_READY: island=256m terrain, palms="+palmCount+", ship=35m, scene="+ScenePath);
    }
    [MenuItem("Tools/Pirate Legends/Actualizar costa y repintar isla")]
    public static void FinishTerrain()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath) return;
        var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
        if (!terrain) return;
        var d = terrain.terrainData;
        int n = d.heightmapResolution;
        var heights = d.GetHeights(0,0,n,n);
        var map = new Texture2D(n,n,TextureFormat.RGBA32,false,true) { name="T_IslandHeight",wrapMode=TextureWrapMode.Clamp };
        var pixels = new Color[n*n];
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)pixels[z*n+x]=new Color(heights[z,x],0,0,1);
        map.SetPixels(pixels);map.Apply();map=Save(map,Root+"/Textures/T_IslandHeight.asset");
        var waterMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/MAT_PrototypeOcean.mat");
        waterMat.SetTexture("_IslandHeight",map);EditorUtility.SetDirty(waterMat);
        AssetDatabase.SaveAssets();
        int size=d.alphamapResolution;var paint=new float[size,size,3];
        for(int z=0;z<size;z++)for(int x=0;x<size;x++)
        {
            float u=x/(float)(size-1),v=z/(float)(size-1),wx=u*256-128,wz=v*256-128;
            float y=d.GetInterpolatedHeight(u,v)+terrain.transform.position.y;
            float slope=d.GetSteepness(u,v);
            if(float.IsNaN(slope)||float.IsInfinity(slope))slope=0;
            float rock=Mathf.Clamp01(Smooth(25,47,slope)*.8f+Smooth(24,34,y)*.3f);
            float pathX=8-.32f*(wz+58)+5*Mathf.Sin((wz+58)*.045f);
            float path=(1-Smooth(1.5f,3.8f,Mathf.Abs(wx-pathX)))*Smooth(-64,-52,wz)*(1-Smooth(22,40,wz));
            float sand=Mathf.Max(1-Smooth(2.3f,5.8f,y),path*.9f)*(1-rock);
            paint[z,x,0]=sand;paint[z,x,1]=1-sand-rock;paint[z,x,2]=rock;
        }
        d.SetAlphamaps(0,0,paint);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();terrain.Flush();
    }
}