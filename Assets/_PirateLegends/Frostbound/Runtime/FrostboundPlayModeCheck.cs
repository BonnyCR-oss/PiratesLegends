using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PirateLegends.CameraSystem;
using PirateLegends.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PirateLegends.Frostbound
{
    // Attached only during an explicit editor verification; never included in the saved scene/prefab.
    public sealed class FrostboundPlayModeCheck : MonoBehaviour
    {
        [Serializable] public class Check { public string name; public bool pass; public string evidence; }
        [Serializable] public class Report { public List<Check> checks = new List<Check>(); public bool complete; }
        public Report report = new Report();
        FrostboundAbilities skills;
        Camera camera;
        ThirdPersonOrbitCamera orbit;
        PlayerInputReader input;
        PlayerLocomotion motor;
        Animator animator;
        Vector3 start;
        const string Folder = "Captures/Frostbound";
        void Record(string name,bool pass,string evidence) {report.checks.Add(new Check{name=name,pass=pass,evidence=evidence});Debug.Log("FROSTBOUND_CHECK "+name+" "+(pass?"PASS":"FAIL")+" "+evidence);Save();}
        void Save(){ Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/PlayModeReport.json",JsonUtility.ToJson(report,true)); }
        void Keys(params Key[] keys) { InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys)); }
        void Aim(Vector3 point)
        {
            orbit.enabled=false;
            camera.transform.position=transform.position+new Vector3(0,2.7f,-4);
            camera.transform.LookAt(point);
        }
        IEnumerator Start()
        {
            skills=GetComponent<FrostboundAbilities>();motor=GetComponent<PlayerLocomotion>();input=GetComponent<PlayerInputReader>();
            animator=GetComponentInChildren<Animator>();camera=Camera.main;orbit=camera.GetComponent<ThirdPersonOrbitCamera>();start=transform.position;
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            yield return new WaitForSeconds(.2f);
            Record("Humanoid",animator.isHuman && animator.avatar.isValid,"Avatar="+animator.avatar.name+"; clips="+animator.runtimeAnimatorController.animationClips.Length);
            Keys(Key.W);yield return new WaitForSeconds(.65f);Keys();yield return new WaitForSeconds(.15f);
            float walked=Vector3.Distance(start,transform.position);
            Record("Walk input",walked>.7f,"distance="+walked+" Speed="+animator.GetFloat("Speed"));
            Vector3 before=transform.position;
            Keys(Key.W,Key.LeftShift);yield return new WaitForSeconds(.65f);Keys();yield return new WaitForSeconds(.15f);
            float ran=Vector3.Distance(before,transform.position);
            Record("Run input",ran>walked*1.25f,"distance="+ran+" walk="+walked);
            float baseline=transform.position.y,peak=baseline;
            Keys(Key.Space);yield return new WaitForSeconds(.06f);Keys();
            for(float t=0;t<.95f;t+=Time.deltaTime){peak=Mathf.Max(peak,transform.position.y);yield return null;}
            Record("Jump input",peak-baseline>.7f && motor.Grounded,"height="+(peak-baseline)+" grounded="+motor.Grounded);
            Keys();ResetPosition();yield return new WaitForSeconds(.2f);
            var first=GameObject.Find("Blanco_1").GetComponent<FrostboundHealth>();
            var second=GameObject.Find("Blanco_2").GetComponent<FrostboundHealth>();
            var covered=GameObject.Find("Blanco_4").GetComponent<FrostboundHealth>();
            // Cast through the actual keyboard path, at a valid ground point beside Blanco_1.
            Aim(new Vector3(-3,0,2));yield return null;
            Keys(Key.Digit1);yield return new WaitForSeconds(.08f);Keys();
            yield return new WaitForSeconds(2.05f);
            Record("H1 ground and dedup",first.Current==first.maximum-skills.slamDamage && first.HitCount==1,"health="+first.Current+" hits="+first.HitCount+" impact="+skills.LastImpact);
            ScreenCapture.CaptureScreenshot(Folder+"/02_H1_Impact.png");
            yield return new WaitForSeconds(2);
            Aim(second.transform.position);yield return null;
            Keys(Key.Digit2);yield return new WaitForSeconds(.08f);Keys();
            yield return new WaitForSeconds(3.2f);
            Record("H2 three collisions",second.HitCount==3 && second.Current==second.maximum-skills.lanceDamage*3,"health="+second.Current+" hits="+second.HitCount+" total impacts="+skills.Impacts);
            ScreenCapture.CaptureScreenshot(Folder+"/03_H2_Impacts.png");
            yield return new WaitForSeconds(.8f);
            Aim(GameObject.Find("Blanco_3").transform.position);yield return null;
            Keys(Key.Digit3);yield return new WaitForSeconds(3.1f);
            Record("H3 hold pose",skills.IsAimingBow && animator.speed==0,"aiming="+skills.IsAimingBow+" speed="+animator.speed);
            ScreenCapture.CaptureScreenshot(Folder+"/04_H3_Hold.png");
            var third=GameObject.Find("Blanco_3").GetComponent<FrostboundHealth>();
            Keys();yield return new WaitForSeconds(.9f);
            Record("H3 release collision",third.HitCount==1 && third.Current==third.maximum-skills.arrowDamage,"health="+third.Current+" hits="+third.HitCount+" impact="+skills.LastImpact);
            ScreenCapture.CaptureScreenshot(Folder+"/05_H3_Explosion.png");
            yield return new WaitForSeconds(2);
            Record("Bow cleanup",GameObject.Find("IceBow_Equipped")==null && GameObject.Find("IceArrow_Nocked")==null,"bow and nocked arrow removed after dissolution");
            // Explicitly launch towards the covered target: collision must stop at the nearer wall.
            int impacts=skills.Impacts;skills.Launch(skills.arrowPrefab,transform.position+Vector3.up*1.3f,covered.transform.position,skills.arrowSpeed,skills.arrowDamage,skills.arrowRadius,true);
            yield return new WaitForSeconds(1);
            Record("Wall blocks projectile",covered.HitCount==0 && skills.Impacts==impacts+1 && skills.LastImpact.z<5.4f,"covered health="+covered.Current+" impact="+skills.LastImpact);
            float healthBefore=first.Current;int hitsBefore=first.HitCount;
            int affected=FrostboundHealth.Area(first.transform.position,2,10,transform,first.team,false,~0);
            Record("Friendly fire disabled",first.Current==healthBefore && first.HitCount==hitsBefore && affected==0,"friendly team rejected");
            // A miss cannot create damage on projectile expiry.
            impacts=skills.Impacts;skills.Launch(skills.arrowPrefab,transform.position+Vector3.up*5,transform.position+Vector3.up*30,skills.arrowSpeed,skills.arrowDamage,skills.arrowRadius,true);
            yield return new WaitForSeconds(5.2f);
            Record("Miss has no damage",skills.Impacts==impacts,"impacts="+skills.Impacts);
            Keys(Key.R);yield return new WaitForSeconds(.08f);Keys();yield return null;
            Record("Reset targets input",first.Current==first.maximum && second.Current==second.maximum && third.Current==third.maximum,"R restored all targets");
            ResetPosition();orbit.enabled=true;Cursor.lockState=CursorLockMode.Locked;
            report.complete=true;Save();Debug.Log("FROSTBOUND_VERIFICATION_COMPLETE");
        }
        void ResetPosition(){var controller=GetComponent<CharacterController>();controller.enabled=false;transform.position=start;transform.rotation=Quaternion.identity;controller.enabled=true;}
        void OnDisable(){if(Keyboard.current!=null)Keys();}
    }
}
