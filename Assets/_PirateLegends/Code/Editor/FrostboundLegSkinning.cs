using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Correct the imported boot weights without changing the source FBX, animation
// clips, cloth particles, or prefab overrides. Reimporting reapplies the repair.
public sealed class FrostboundLegSkinning : AssetPostprocessor
{
    public const string ModelPath = "Assets/_PirateLegends/Art/Characters/iceCharacter/Models/Frostbound_Unity_Cloth.fbx";

    public override uint GetVersion() { return 1; }

    void OnPostprocessModel(GameObject root)
    {
        if (assetPath != ModelPath) return;
        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (renderer.name == "Frostbound_Cuerpo") Repair(renderer);
    }

    public static int Repair(SkinnedMeshRenderer renderer)
    {
        var mesh = renderer.sharedMesh;
        var vertices = mesh.vertices;
        var weights = mesh.boneWeights;
        var bind = mesh.bindposes;
        int changed = 0;
        foreach (string side in new[] { "Left", "Right" })
        {
            int thigh = Find(renderer, side + "UpLeg");
            int shin = Find(renderer, side + "Leg");
            int foot = Find(renderer, side + "Foot");
            int toe = Find(renderer, side + "ToeBase");
            if (thigh < 0 || shin < 0 || foot < 0 || toe < 0)
                throw new InvalidOperationException("Frostbound: missing leg bones.");
            Vector3 knee = bind[shin].inverse.MultiplyPoint3x4(Vector3.zero);
            Vector3 ankle = bind[foot].inverse.MultiplyPoint3x4(Vector3.zero);
            Vector3 toes = bind[toe].inverse.MultiplyPoint3x4(Vector3.zero);
            Vector3 up = (knee - ankle).normalized;
            float length = Vector3.Distance(knee, ankle);
            Vector3 forward = Vector3.ProjectOnPlane(toes - ankle, up);
            float footLength = forward.magnitude;
            forward.Normalize();
            for (int i = 0; i < vertices.Length; i++)
            {
                var original = weights[i];
                float membership = Weight(original, thigh) + Weight(original, shin)
                    + Weight(original, foot) + Weight(original, toe);
                if (membership < .6f) continue;
                float height = Vector3.Dot(vertices[i] - ankle, up) / length;
                if (height >= 1.12f) continue;
                // Exclude hanging coat pieces beside/behind the boot.
                Vector3 axisPoint = ankle + up * Mathf.Clamp(height, 0, 1) * length;
                if (Vector3.Distance(vertices[i], axisPoint) > length * .48f) continue;

                float shinWeight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.08f, .28f, height));
                float toeWeight = (1 - shinWeight)
                    * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, 1.15f,
                        Vector3.Dot(vertices[i] - ankle, forward) / footLength)) * .65f;
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1.12f, height));
                var combined = new Dictionary<int, float>();
                Add(combined, original.boneIndex0, original.weight0 * (1 - blend));
                Add(combined, original.boneIndex1, original.weight1 * (1 - blend));
                Add(combined, original.boneIndex2, original.weight2 * (1 - blend));
                Add(combined, original.boneIndex3, original.weight3 * (1 - blend));
                Add(combined, shin, shinWeight * blend);
                Add(combined, foot, (1 - shinWeight - toeWeight) * blend);
                Add(combined, toe, toeWeight * blend);
                var sorted = new List<KeyValuePair<int, float>>(combined);
                sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
                while (sorted.Count < 4) sorted.Add(new KeyValuePair<int, float>(0, 0));
                float sum = sorted[0].Value + sorted[1].Value + sorted[2].Value + sorted[3].Value;
                weights[i] = new BoneWeight {
                    boneIndex0 = sorted[0].Key, weight0 = sorted[0].Value / sum,
                    boneIndex1 = sorted[1].Key, weight1 = sorted[1].Value / sum,
                    boneIndex2 = sorted[2].Key, weight2 = sorted[2].Value / sum,
                    boneIndex3 = sorted[3].Key, weight3 = sorted[3].Value / sum
                };
                changed++;
            }
        }
        mesh.boneWeights = weights;
        return changed;
    }

    static int Find(SkinnedMeshRenderer renderer, string suffix)
    {
        return Array.FindIndex(renderer.bones, b => b != null && b.name == "mixamorig:" + suffix);
    }

    static float Weight(BoneWeight w, int bone)
    {
        return (w.boneIndex0 == bone ? w.weight0 : 0)
            + (w.boneIndex1 == bone ? w.weight1 : 0)
            + (w.boneIndex2 == bone ? w.weight2 : 0)
            + (w.boneIndex3 == bone ? w.weight3 : 0);
    }

    static void Add(Dictionary<int, float> weights, int bone, float amount)
    {
        float existing;
        weights.TryGetValue(bone, out existing);
        weights[bone] = existing + amount;
    }
}
