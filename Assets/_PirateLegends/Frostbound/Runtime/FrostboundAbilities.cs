using System.Collections;
using PirateLegends.Player;
using PirateLegends.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateLegends.Frostbound
{
    [DefaultExecutionOrder(-105)]
    [RequireComponent(typeof(PlayerLocomotion), typeof(PlayerInputReader))]
    public sealed class FrostboundAbilities : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;
        public Camera aimCamera;
        public GameObject bowPrefab, arrowPrefab, lancePrefab, spikePrefab, fragmentPrefab;
        public Material iceMaterial, glowMaterial;
        [Header("Gameplay")]
        public float range = 28, groundRange = 20, groundRadius = 3.2f;
        public float slamDamage = 55, lanceDamage = 22, arrowDamage = 65;
        public float lanceSpeed = 19, arrowSpeed = 27, arrowRadius = 2.7f;
        public float cooldown1 = 5, cooldown2 = 5, cooldown3 = 6;
        public int team = 1;
        public bool friendlyFire;
        public LayerMask collisionMask = ~0;
        [Header("Clip lengths (seconds)")]
        public float clip1Length = 3.74f, clip2Length = 3.57f, clip3Length = 5.04f;
        PlayerLocomotion motor;
        PlayerInputReader input;
        Transform leftHand, rightHand;
        GameObject bow, nockedArrow, dissolvingBow;
        LineRenderer stringLine, marker;
        float[] ready = new float[3];
        float bowScale = 1;
        bool busy, releaseRequested, aimingBow;
        bool groundValid;
        Vector3 aimPoint, groundPoint;
        string status = "Apunta al suelo para Impacto glacial";
        Coroutine cast;
        FrostboundVfx vfx;
        public bool IsBusy => busy;
        public bool IsAimingBow => aimingBow;
        public Vector3 AimPoint => aimPoint;
        public int Impacts { get; private set; }
        public int DamagedTargets { get; private set; }
        public Vector3 LastImpact { get; private set; }

        void Awake()
        {
            motor = GetComponent<PlayerLocomotion>(); input = GetComponent<PlayerInputReader>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (animator && animator.isHuman)
            {
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
            if (!aimCamera) aimCamera = Camera.main;
            vfx = GetComponent<FrostboundVfx>();
            if (!vfx) vfx = gameObject.AddComponent<FrostboundVfx>();
            vfx.Configure(this, leftHand, rightHand);
        }
        void Start()
        {
            var go = new GameObject("Frostbound_TargetRing"); marker = go.AddComponent<LineRenderer>();
            marker.sharedMaterial = glowMaterial; marker.useWorldSpace = true; marker.loop = true;
            marker.widthMultiplier = .045f; marker.positionCount = 65;
            marker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (aimCamera)
            {
                var follow = new GameObject("Frostbound_CameraTarget").transform;
                follow.SetParent(transform,false); follow.localPosition = Vector3.up*1.35f;
                var orbit = aimCamera.GetComponent<ThirdPersonOrbitCamera>();
                if (orbit) orbit.Configure(follow,input,transform);
                motor.CameraTransform = aimCamera.transform;
            }
        }
        void Update()
        {
            UpdateAim();
            bool active = input.enabled && motor.enabled && Cursor.lockState == CursorLockMode.Locked;
            if (marker) marker.enabled = active && groundValid && !busy;
            if (groundValid && marker)
                for (int i=0;i<65;i++) { float a=i*Mathf.PI*2/64; marker.SetPosition(i,groundPoint+new Vector3(Mathf.Cos(a)*groundRadius,.04f,Mathf.Sin(a)*groundRadius)); }
            if (!active)
            {
                if (busy) CancelCast();
                return;
            }
            var keys = Keyboard.current; if (keys == null) return;
            if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) TryCast(1);
            if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) TryCast(2);
            if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) TryCast(3);
            if (keys.digit3Key.wasReleasedThisFrame || keys.numpad3Key.wasReleasedThisFrame) ReleaseBow();
            if (aimingBow && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ReleaseBow();
        }
        void UpdateAim()
        {
            if (!aimCamera) return;
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            aimPoint = ray.GetPoint(range);
            var hits = Physics.RaycastAll(ray,range+6,collisionMask,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            groundValid = false;
            foreach(var hit in hits)
            {
                if(hit.collider.transform.IsChildOf(transform)) continue;
                aimPoint = hit.point;
                groundPoint = hit.point;
                groundValid = hit.normal.y >= .65f && Vector3.Distance(transform.position,hit.point) <= groundRange
                    && !hit.collider.GetComponentInParent<FrostboundHealth>();
                // Ground targeting cannot go through intervening walls/targets.
                if (groundValid)
                {
                    Vector3 origin=transform.position+Vector3.up;
                    foreach(var block in Physics.RaycastAll(origin,groundPoint+Vector3.up*.08f-origin,Vector3.Distance(origin,groundPoint+Vector3.up*.08f),collisionMask,QueryTriggerInteraction.Ignore))
                        if (!block.collider.transform.IsChildOf(transform)) { groundValid=false; break; }
                }
                break;
            }
        }
        public bool TryCast(int number)
        {
            if (number<1 || number>3 || busy || !motor.enabled || !input.enabled || !motor.Grounded || Time.time<ready[number-1]) return false;
            UpdateAim();
            if (number==1 && !groundValid) { status="H1: apunta a suelo visible a menos de "+groundRange+" m"; return false; }
            busy=true; motor.MovementLocked=true; releaseRequested=false;
            ready[number-1]=Time.time+(number==1?cooldown1:number==2?cooldown2:cooldown3);
            animator.ResetTrigger("Attack"); animator.ResetTrigger("Jump"); animator.speed=1;
            Face(aimPoint); animator.CrossFadeInFixedTime("Ability"+number,.06f);
            if(number==1) cast=StartCoroutine(Slam(groundPoint));
            else if(number==2) cast=StartCoroutine(Lances(aimPoint));
            else cast=StartCoroutine(Bow());
            return true;
        }
        void Face(Vector3 point)
        {
            Vector3 direction=point-transform.position; direction.y=0;
            if(direction.sqrMagnitude>.001f) transform.rotation=Quaternion.LookRotation(direction);
        }
        IEnumerator Slam(Vector3 target)
        {
            status="Impacto glacial · objetivo fijado";
            yield return new WaitForSeconds(13f/30);
            vfx.Begin(1, target);
            yield return new WaitForSeconds(44f/30);
            Impact(target+Vector3.up*.03f,groundRadius,slamDamage,false,true);
            yield return new WaitForSeconds(Mathf.Max(.1f,clip1Length-57f/30));
            Finish();
        }
        IEnumerator Lances(Vector3 target)
        {
            status="Lanzas de escarcha · dirección fijada";
            yield return new WaitForSeconds(33f/30);
            vfx.Begin(2, target);
            yield return new WaitForSeconds(34f/30);
            for(int i=0;i<3;i++)
            {
                Vector3 origin=(rightHand?rightHand.position:transform.position+Vector3.up*1.3f)+transform.right*(i-1)*.22f;
                origin=vfx.TakeLance(i,origin);
                Launch(lancePrefab,origin,target,lanceSpeed,lanceDamage,1.2f,false);
                if(i<2) yield return new WaitForSeconds(4f/30);
            }
            yield return new WaitForSeconds(Mathf.Max(.1f,clip2Length-75f/30)); Finish();
        }
        IEnumerator Bow()
        {
            status="Arco de hielo · mantén 3 para tensar; suelta para disparar";
            yield return new WaitForSeconds(5f/30);
            vfx.Begin(3, aimPoint);
            bow=Instantiate(bowPrefab); bow.name="IceBow_Equipped";
            var lineObject=new GameObject("Dynamic_BowString"); lineObject.transform.SetParent(bow.transform,false);
            stringLine=lineObject.AddComponent<LineRenderer>(); stringLine.sharedMaterial=vfx.EnergyMaterial?vfx.EnergyMaterial:glowMaterial;
            stringLine.positionCount=3; stringLine.widthMultiplier=.015f; stringLine.useWorldSpace=true;
            stringLine.enabled=false; bowScale=.01f;
            float age=5f/30;
            while(age<79f/30)
            {
                age+=Time.deltaTime; bowScale=Mathf.SmoothStep(.01f,1,Mathf.InverseLerp(12f/30,34f/30,age));
                if(stringLine) stringLine.enabled=age>=34f/30;
                if(Random.value<Time.deltaTime*18 && age<36f/30) Shard(leftHand.position+Random.onUnitSphere*.25f,.18f);
                yield return null;
            }
            aimingBow=true; animator.Play("Ability3",0,(79f/30)/clip3Length); animator.speed=0;
            nockedArrow=Instantiate(arrowPrefab); nockedArrow.name="IceArrow_Nocked";
            vfx.Nock(nockedArrow);
            while(!releaseRequested) { Face(aimPoint); yield return null; }
            aimingBow=false;
            Vector3 chosen=aimPoint;
            animator.speed=1; animator.Play("Ability3",0,(114f/30)/clip3Length);
            yield return new WaitForSeconds(3f/30);
            Vector3 origin=rightHand?rightHand.position:transform.position+Vector3.up*1.4f;
            if(nockedArrow) Destroy(nockedArrow);
            Launch(arrowPrefab,origin,chosen,arrowSpeed,arrowDamage,arrowRadius,true);
            status="Flecha liberada · impacto por colisión";
            yield return new WaitForSeconds(28f/30);
            if(stringLine) stringLine.enabled=false;
            // Dissolution continues beyond the skeletal clip, independently of the Animator.
            var dissolving=bow; bow=null; stringLine=null;
            if(dissolving) StartCoroutine(Dissolve(dissolving));
            yield return new WaitForSeconds(.18f); Finish();
        }
        public void ReleaseBow() { if(busy) releaseRequested=true; }
        void LateUpdate()
        {
            if(!bow || !leftHand) return;
            Vector3 direction=(aimPoint-leftHand.position).normalized;
            if(direction.sqrMagnitude<.01f) direction=transform.forward;
            bow.transform.SetPositionAndRotation(leftHand.position,Quaternion.LookRotation(direction,Vector3.up));
            bow.transform.localScale=Vector3.one*bowScale;
            if(stringLine)
            {
                Vector3 top=bow.transform.TransformPoint(new Vector3(0,.72f,-.05f));
                Vector3 bottom=bow.transform.TransformPoint(new Vector3(0,-.72f,-.05f));
                Vector3 center=aimingBow && rightHand?rightHand.position:leftHand.position-direction*.17f;
                stringLine.SetPosition(0,top); stringLine.SetPosition(1,center); stringLine.SetPosition(2,bottom);
                if(nockedArrow) nockedArrow.transform.SetPositionAndRotation(center,Quaternion.LookRotation(direction));
            }
        }
        IEnumerator Dissolve(GameObject visual)
        {
            dissolvingBow=visual;
            float t=0; Vector3 initial=visual.transform.localScale;
            while(visual && t<1.2f)
            {
                t+=Time.deltaTime;
                if(leftHand) visual.transform.position=leftHand.position;
                visual.transform.localScale=initial*Mathf.Max(0,1-t/1.2f);
                if(Random.value<Time.deltaTime*25) Shard(visual.transform.position+Random.onUnitSphere*.35f,.2f);
                yield return null;
            }
            if(visual) Destroy(visual);
            dissolvingBow=null;
        }
        public void Launch(GameObject prefab,Vector3 origin,Vector3 target,float speed,float damage,float radius,bool arrow)
        {
            Vector3 direction=(target-origin).normalized;
            if(direction.sqrMagnitude<.01f) direction=transform.forward;
            var go=Instantiate(prefab,origin,Quaternion.LookRotation(direction));
            var projectile=go.AddComponent<FrostboundProjectile>(); projectile.owner=this; projectile.direction=direction;
            projectile.speed=speed; projectile.damage=damage; projectile.blastRadius=radius; projectile.arrow=arrow; projectile.collisionMask=collisionMask;
            vfx.Projectile(go,arrow);
        }
        public void Impact(Vector3 point,float radius,float damage,bool arrow,bool slam=false)
        {
            Impacts++; LastImpact=point;
            int hits=FrostboundHealth.Area(point,radius,damage,transform,team,friendlyFire,collisionMask);
            DamagedTargets+=hits; status=$"Impacto · {hits} objetivo(s) · daño {damage:0}";
            vfx.Impact(point,radius,slam?1:arrow?3:2);
        }
        void Shard(Vector3 position,float size)
        {
            if(!fragmentPrefab) return;
            var go=Instantiate(fragmentPrefab,position,Random.rotation); go.transform.localScale*=size;
            var effect=go.AddComponent<FrostboundEffect>(); effect.fragment=true; effect.lifetime=.7f;
            effect.velocity=Random.onUnitSphere*2+Vector3.up*1.5f;
        }
        void Finish() { vfx.StopCharge(); busy=false; aimingBow=false; motor.MovementLocked=false; animator.speed=1; animator.CrossFadeInFixedTime("Locomotion",.1f); cast=null; }
        void CancelCast()
        {
            StopAllCoroutines(); if(bow) Destroy(bow); if(nockedArrow) Destroy(nockedArrow); if(dissolvingBow) Destroy(dissolvingBow);
            if(vfx) vfx.StopCharge();
            bow=null; stringLine=null; cast=null; busy=false; aimingBow=false;
            if(motor) motor.MovementLocked=false;
            if(animator) {animator.speed=1; animator.CrossFadeInFixedTime("Locomotion",.1f);}
        }
        void OnDisable() { CancelCast(); if(marker) marker.enabled=false; }
        void OnDestroy() { if(marker) Destroy(marker.gameObject); }
        void OnGUI()
        {
            GUI.color=new Color(.025f,.05f,.09f,.94f); GUI.DrawTexture(new Rect(16,16,650,137),Texture2D.whiteTexture);
            GUI.color=Color.white;
            GUI.Label(new Rect(30,24,630,25),"FROSTBOUND  /  CAMPO DE PRUEBA");
            GUI.Label(new Rect(30,49,630,24),"WASD mover · Shift correr · Espacio saltar · Ratón apuntar · Esc liberar cursor");
            GUI.Label(new Rect(30,72,630,24),"1 área en suelo · 2 tres lanzas · Mantén 3 arco, suelta 3 dispara · R restaura blancos");
            GUI.Label(new Rect(30,97,630,24),$"H1 {Mathf.Max(0,ready[0]-Time.time):0.0}s   H2 {Mathf.Max(0,ready[1]-Time.time):0.0}s   H3 {Mathf.Max(0,ready[2]-Time.time):0.0}s   |   {status}");
            GUI.Label(new Rect(30,121,630,24),"Clic captura cursor · anillo cian: suelo válido · muro bloquea proyectiles y daño");
            GUI.color=groundValid?Color.cyan:Color.white;
            GUI.DrawTexture(new Rect(Screen.width/2f-7,Screen.height/2f-1,14,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width/2f-1,Screen.height/2f-7,2,14),Texture2D.whiteTexture);
            GUI.color=Color.white;
        }
    }
}
