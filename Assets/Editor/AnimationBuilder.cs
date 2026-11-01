using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MyUmrah.EditorTools
{
    /// <summary>Generates real AnimationClips + an AnimatorController for the procedural humanoid (limb joints ArmL/ArmR/LegL/LegR).</summary>
    public static class AnimationBuilder
    {
        const string Dir = "Assets/Animations";

        static AnimationCurve Curve(bool linear, params float[] tv)
        {
            var keys = new Keyframe[tv.Length / 2];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(tv[2 * i], tv[2 * i + 1]);
            var c = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                if (linear)
                {
                    AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Linear);
                }
                else c.SmoothTangents(i, 0f);
            }
            return c;
        }

        static AnimationClip Clip(string name, bool loop, params (string path, AnimationCurve curve)[] curves)
        {
            string path = Dir + "/" + name + ".anim";
            AssetDatabase.DeleteAsset(path);
            var clip = new AnimationClip { name = name, frameRate = 30f };
            foreach (var c in curves) clip.SetCurve(c.path, typeof(Transform), "localEulerAnglesRaw.x", c.curve);
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        public static AnimatorController Build()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Animations");

            var idle = Clip("Humanoid_Idle", true,
                ("ArmL", Curve(false, 0f, 0f, 1.5f, 3f, 3f, 0f)),
                ("ArmR", Curve(false, 0f, 0f, 1.5f, -3f, 3f, 0f)));
            var walk = Clip("Humanoid_Walk", true,
                ("LegL", Curve(false, 0f, 0f, 0.25f, 30f, 0.5f, 0f, 0.75f, -30f, 1f, 0f)),
                ("LegR", Curve(false, 0f, 0f, 0.25f, -30f, 0.5f, 0f, 0.75f, 30f, 1f, 0f)),
                ("ArmL", Curve(false, 0f, 0f, 0.25f, -25f, 0.5f, 0f, 0.75f, 25f, 1f, 0f)),
                ("ArmR", Curve(false, 0f, 0f, 0.25f, 25f, 0.5f, 0f, 0.75f, -25f, 1f, 0f)));
            var run = Clip("Humanoid_Run", true,
                ("LegL", Curve(false, 0f, 0f, 0.15f, 55f, 0.3f, 0f, 0.45f, -55f, 0.6f, 0f)),
                ("LegR", Curve(false, 0f, 0f, 0.15f, -55f, 0.3f, 0f, 0.45f, 55f, 0.6f, 0f)),
                ("ArmL", Curve(false, 0f, 0f, 0.15f, -65f, 0.3f, 0f, 0.45f, 65f, 0.6f, 0f)),
                ("ArmR", Curve(false, 0f, 0f, 0.15f, 65f, 0.3f, 0f, 0.45f, -65f, 0.6f, 0f)));
            var jump = Clip("Humanoid_Jump", false,
                ("ArmL", Curve(false, 0f, 0f, 0.2f, -150f, 0.7f, 0f)),
                ("ArmR", Curve(false, 0f, 0f, 0.2f, -150f, 0.7f, 0f)),
                ("LegL", Curve(false, 0f, 0f, 0.2f, -40f, 0.5f, -20f, 0.7f, 0f)),
                ("LegR", Curve(false, 0f, 0f, 0.2f, 20f, 0.7f, 0f)));
            var swim = Clip("Humanoid_Swim", true,
                ("ArmL", Curve(true, 0f, 0f, 1.2f, -360f)),
                ("ArmR", Curve(true, 0f, -180f, 1.2f, -540f)),
                ("LegL", Curve(false, 0f, 15f, 0.15f, -15f, 0.3f, 15f, 0.45f, -15f, 0.6f, 15f, 0.75f, -15f, 0.9f, 15f, 1.05f, -15f, 1.2f, 15f)),
                ("LegR", Curve(false, 0f, -15f, 0.15f, 15f, 0.3f, -15f, 0.45f, 15f, 0.6f, -15f, 0.75f, 15f, 0.9f, -15f, 1.05f, 15f, 1.2f, -15f)));
            var sit = Clip("Humanoid_Sit", true,
                ("LegL", Curve(false, 0f, -85f, 1f, -85f)),
                ("LegR", Curve(false, 0f, -85f, 1f, -85f)),
                ("ArmL", Curve(false, 0f, -30f, 1f, -30f)),
                ("ArmR", Curve(false, 0f, -30f, 1f, -30f)));

            string cpath = Dir + "/Humanoid.controller";
            AssetDatabase.DeleteAsset(cpath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Swimming", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Sitting", AnimatorControllerParameterType.Bool);

            var sm = ctrl.layers[0].stateMachine;
            var loco = sm.AddState("Locomotion");
            var tree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            tree.AddChild(idle, 0f); tree.AddChild(walk, 1f); tree.AddChild(run, 2f);
            loco.motion = tree;
            sm.defaultState = loco;

            var sJump = sm.AddState("Jump"); sJump.motion = jump;
            var sSwim = sm.AddState("Swim"); sSwim.motion = swim;
            var sSit = sm.AddState("Sit"); sSit.motion = sit;

            var tj = sm.AddAnyStateTransition(sJump);
            tj.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            tj.hasExitTime = false; tj.duration = 0.08f; tj.canTransitionToSelf = false;
            var tjb = sJump.AddTransition(loco);
            tjb.hasExitTime = true; tjb.exitTime = 0.9f; tjb.duration = 0.1f;

            var ts = sm.AddAnyStateTransition(sSwim);
            ts.AddCondition(AnimatorConditionMode.If, 0f, "Swimming");
            ts.hasExitTime = false; ts.duration = 0.15f; ts.canTransitionToSelf = false;
            var tsb = sSwim.AddTransition(loco);
            tsb.AddCondition(AnimatorConditionMode.IfNot, 0f, "Swimming");
            tsb.hasExitTime = false; tsb.duration = 0.15f;

            var tt = sm.AddAnyStateTransition(sSit);
            tt.AddCondition(AnimatorConditionMode.If, 0f, "Sitting");
            tt.hasExitTime = false; tt.duration = 0.1f; tt.canTransitionToSelf = false;
            var ttb = sSit.AddTransition(loco);
            ttb.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sitting");
            ttb.hasExitTime = false; ttb.duration = 0.1f;

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }
    }
}
