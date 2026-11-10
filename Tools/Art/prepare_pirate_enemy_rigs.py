"""Reconstruct local anatomical skin weights in background Blender.
Run: blender -b --python Tools/Art/prepare_pirate_enemy_rigs.py
Preserves source geometry, UVs and the Mixamo animation skeletons.
"""
from pathlib import Path
import json
import bpy
import numpy as np
ROOT=Path(__file__).resolve().parents[2]/'Assets/_PirateLegends/Art/Characters/Enemies'


def smoothstep(lo,hi,value):
    t=np.clip((value-lo)/(hi-lo),0,1)
    return t*t*(3-2*t)


def anatomical_weights(mesh,rig):
    names=[b.name for b in rig.data.bones]
    ids={n.split(':')[-1]:i for i,n in enumerate(names)}
    heads={n.split(':')[-1]:np.array(rig.matrix_world@rig.data.bones[n].head_local) for n in names}
    points=np.array([mesh.matrix_world@v.co for v in mesh.data.vertices])
    x,y,z=points.T
    weights=np.zeros((len(points),len(names)))
    confidence=np.zeros(len(points))
    torso=['Hips','Spine','Spine1','Spine2','Neck','Head']
    weights[:,ids['Hips']]=1
    for parent,child in zip(torso,torso[1:]):
        width=.055 if child in ['Neck','Head'] else .10
        blend=smoothstep(heads[child][2]-width,heads[child][2]+width,z)
        amount=weights[:,ids[parent]]*blend
        weights[:,ids[parent]]-=amount
        weights[:,ids[child]]+=amount
    for side,sign in [('Left',1),('Right',-1)]:
        hip=heads[side+'UpLeg'];knee=heads[side+'Leg'];ankle=heads[side+'Foot']
        # Rigid sole; smooth ankle, knee and hip articulation.
        k=smoothstep(knee[2]-.13,knee[2]+.13,z)
        f=smoothstep(ankle[2]+.025,ankle[2]+.16,z)
        h=smoothstep(hip[2]-.07,hip[2]+.17,z)
        leg=np.zeros_like(weights)
        leg[:,ids[side+'Foot']]=(1-f)*(1-h)
        leg[:,ids[side+'Leg']]=f*(1-k)*(1-h)
        leg[:,ids[side+'UpLeg']]=f*k*(1-h)
        leg[:,ids['Hips']]=h
        region=(x*sign>=0)&(z<hip[2]+.17)
        weights[region]=leg[region]
        confidence[region&(z<ankle[2]+.025)]=1
        shoulder=heads[side+'Arm'];elbow=heads[side+'ForeArm'];wrist=heads[side+'Hand']
        outward=x*sign
        s=smoothstep(abs(shoulder[0])-.11,abs(shoulder[0])+.08,outward)
        e=smoothstep(abs(elbow[0])-.10,abs(elbow[0])+.10,outward)
        w=smoothstep(abs(wrist[0])-.055,abs(wrist[0])+.055,outward)
        arm=np.zeros_like(weights)
        arm[:,ids[side+'Shoulder']]=1-s
        arm[:,ids[side+'Arm']]=s*(1-e)
        arm[:,ids[side+'ForeArm']]=s*e*(1-w)
        arm[:,ids[side+'Hand']]=s*e*w
        entry=smoothstep(abs(shoulder[0])-.19,abs(shoulder[0])+.015,outward)
        height=smoothstep(shoulder[2]-.32,shoulder[2]-.14,z)
        blend=(entry*height*(outward>0))[:,None]
        weights=weights*(1-blend)+arm*blend
        confidence[(outward>abs(wrist[0])+.055)&(height>.99)]=1
        # Restrict digit weights to the corresponding finger chain. This retains
        # the source punch/grip articulation without pulling the palm into an arm.
        fingers=[]
        for digit in ['Thumb','Index','Middle','Ring','Pinky']:
            chain=[side+'Hand'+digit+str(i) for i in [1,2,3]]
            if not all(b in ids for b in chain):continue
            distances=[]
            for b in chain:
                bone=rig.data.bones[names[ids[b]]]
                start=heads[b];end=np.array(rig.matrix_world@bone.tail_local)
                axis=end-start
                t=np.clip((points-start)@axis/max(axis@axis,1e-12),0,1)
                distances.append(np.linalg.norm(points-start-t[:,None]*axis,axis=1))
            fingers.append((chain,np.min(distances,axis=0)))
        if fingers:
            closest=np.argmin(np.array([d for _,d in fingers]),axis=0)
            for digit,(chain,_) in enumerate(fingers):
                base=abs(heads[chain[0]][0])
                region=(closest==digit)&(outward>base-.015)&(height>.99)
                finger=np.zeros_like(weights)
                finger[:,ids[chain[0]]]=1
                for parent,child in zip(chain,chain[1:]):
                    pivot=abs(heads[child][0])
                    transition=smoothstep(pivot-.012,pivot+.012,outward)
                    amount=finger[:,ids[parent]]*transition
                    finger[:,ids[parent]]-=amount
                    finger[:,ids[child]]+=amount
                entry=smoothstep(base-.01,base+.025,outward)
                weights[region]=weights[region]*(1-entry[region,None])+finger[region]*entry[region,None]
    confidence[z>heads['Head'][2]+.065]=1
    # Weld UV duplicates before regularization, retaining rigid regions.
    keys=np.round(points/.00001).astype(np.int64)
    _,welded=np.unique(keys,axis=0,return_inverse=True)
    count=np.bincount(welded)
    grouped=np.zeros((len(count),len(names)))
    np.add.at(grouped,welded,weights)
    grouped/=count[:,None]
    frozen=np.zeros(len(count));np.maximum.at(frozen,welded,confidence)
    mesh.data.calc_loop_triangles()
    triangles=np.array([t.vertices for t in mesh.data.loop_triangles])
    edges=np.concatenate([triangles[:,[0,1]],triangles[:,[1,2]],triangles[:,[2,0]]])
    edges=np.unique(np.sort(welded[edges],axis=1),axis=0)
    edges=edges[edges[:,0]!=edges[:,1]]
    degrees=np.bincount(edges.flatten(),minlength=len(count)).clip(1)
    initial=grouped.copy()
    for _ in range(12):
        adjacent=np.zeros_like(grouped)
        np.add.at(adjacent,edges[:,0],grouped[edges[:,1]])
        np.add.at(adjacent,edges[:,1],grouped[edges[:,0]])
        candidate=.65*initial+.35*adjacent/degrees[:,None]
        grouped=np.where(frozen[:,None]>.5,initial,candidate)
    weights=grouped[welded]
    largest=np.argsort(weights,axis=1)[:,-4:]
    retained=np.zeros_like(weights)
    np.put_along_axis(retained,largest,np.take_along_axis(weights,largest,axis=1),axis=1)
    weights=retained/retained.sum(axis=1,keepdims=True)
    for n in names:mesh.vertex_groups.new(name=n)
    for i,row in enumerate(weights):
        for j in np.flatnonzero(row>1e-6):mesh.vertex_groups[int(j)].add([i],float(row[j]),'REPLACE')
    return {'vertices':len(points),'unweighted':int(np.count_nonzero(weights.sum(axis=1)<.999)),
            'maxInfluences':int((weights>1e-6).sum(axis=1).max()),
            'maxNormalizationError':float(np.abs(weights.sum(axis=1)-1).max())}


def build(name,idle):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    folder=ROOT/name
    bpy.ops.import_scene.fbx(filepath=str(folder/'Models/Meshy_AI_model.fbx'))
    mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.ops.import_scene.fbx(filepath=str(folder/'Animations'/(idle+'.fbx')))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    rig.animation_data_clear();rig.data.pose_position='REST'
    for obj in [mesh,rig]:obj.scale*=20
    bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    report=anatomical_weights(mesh,rig)
    modifier=mesh.modifiers.new('Armature','ARMATURE');modifier.object=rig
    mesh.parent=rig;mesh.matrix_parent_inverse=rig.matrix_world.inverted()
    rig.data.pose_position='POSE'
    bpy.ops.export_scene.fbx(filepath=str(folder/'Models'/(name+'_Rigged.fbx')),
        use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,
        bake_anim=False,path_mode='STRIP',axis_forward='-Z',axis_up='Y')
    print('SKIN_REPORT '+name+' '+json.dumps(report),flush=True)


if __name__=='__main__':
    for name,idle in [('CorsarioEspadachin','iddle'),('CorsarioLuchador','Idle')]:build(name,idle)
