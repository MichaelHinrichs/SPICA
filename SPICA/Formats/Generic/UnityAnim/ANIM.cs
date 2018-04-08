using System;
using System.Collections.Generic;
using System.Globalization;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using System.IO;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading;
using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Math3D;

namespace SPICA.Formats.Generic.UnityAnim
{
    public class ANIM
    {
        private ANIMAnimationClip Clip;
        private H3DMaterialAnim MatAnim;
        private H3DAnimation SklAnim;
        private H3DDict<H3DBone> Skeleton;
        private int FramesCount;

        public ANIM(H3D Scene, int MdlIndex = -1, int AnimIndex = -1)
        {
            MatAnim = Scene.MaterialAnimations[AnimIndex];
            SklAnim = Scene.SkeletalAnimations[AnimIndex];
            Skeleton = Scene.Models[MdlIndex].Skeleton;

            FramesCount = (int) SklAnim.FramesCount + 1;
            Clip = new ANIMAnimationClip(SklAnim.FramesCount / 30f);

            foreach (H3DAnimationElement Elem in SklAnim.Elements)
            {
                if (Elem.PrimitiveType != H3DPrimitiveType.Transform &&
                    Elem.PrimitiveType != H3DPrimitiveType.QuatTransform) continue;

                AddTranslationCurve(Elem);
                AddScaleCurves(Elem);
                AddRotationCurve(Elem);
            }
            
            Clip.positionCurves.ForEach(CalculateSlopes);
            Clip.scaleCurves.ForEach(CalculateSlopes);
            Clip.eulerCurves.ForEach(x => CorrectRotation(x));
            Clip.eulerCurves.ForEach(CalculateSlopes);
        }

        private string GetBonePath(string BoneName)
        {
            H3DBone Bone = Skeleton.FirstOrDefault(x => x.Name == BoneName);
            if (Bone == null) return "";

            string ret = Bone.Name;
            while (Bone.ParentIndex != -1)
            {
                Bone = Skeleton[Bone.ParentIndex];
                ret = Bone.Name + "/" + ret;
            }

            return ret;
        }

        private void AddTranslationCurve(H3DAnimationElement Elem)
        {
            H3DBone SklBone = Skeleton.FirstOrDefault(x => x.Name == Elem.Name);
            ANIMCurve<Vector3> curves = new ANIMCurve<Vector3>(GetBonePath(Elem.Name));

            if (SklBone == null) return;

            for (int Frame = 0; Frame < FramesCount; Frame++)
            {
                switch (Elem.Content)
                {
                    case H3DAnimTransform Transform:
                        curves.Add(
                            Frame / 30f,
                            new Vector3(
                                -(Transform.TranslationX.Exists //X
                                    ? Transform.TranslationX.GetFrameValue(Frame)
                                    : SklBone.Translation.X),
                                Transform.TranslationY.Exists //Y
                                    ? Transform.TranslationY.GetFrameValue(Frame)
                                    : SklBone.Translation.Y,
                                Transform.TranslationZ.Exists //Z
                                    ? Transform.TranslationZ.GetFrameValue(Frame)
                                    : SklBone.Translation.Z)
                        );
                        break;
                    case H3DAnimQuatTransform QuatTransform:
                        Vector3 vec = QuatTransform.GetTranslationValue(Frame);
                        vec.X *= -1;
                        curves.Add(Frame / 30f, vec);
                        break;
                }
            }

            if (curves.keyFrames.Count > 0)
                Clip.positionCurves.Add(curves);
        }

        private void AddRotationCurve(H3DAnimationElement Elem)
        {
            ANIMCurve<Vector3> curves = new ANIMCurve<Vector3>(GetBonePath(Elem.Name));
         
            for (int Frame = 0; Frame < FramesCount; Frame++)
            {
                switch (Elem.Content)
                {
                    case H3DAnimTransform Transform:
                        curves.Add(Frame / 30f,
                            new Vector3(
                                Transform.RotationX.GetFrameValue(Frame),
                                Transform.RotationY.GetFrameValue(Frame),
                                Transform.RotationZ.GetFrameValue(Frame)
                            )
                        );
                        break;
                    case H3DAnimQuatTransform QuatTransform:
                        Vector3 vec = QuatTransform.GetRotationValue(Frame).ToEuler();
                        vec.Y *= -1;
                        vec.Z *= -1;
                        curves.Add(Frame / 30f, vec * (float) (180 / Math.PI));
                        break;
                }
                
                
            }

            if (curves.keyFrames.Count > 0)
                Clip.eulerCurves.Add(curves);
        }

        private void AddScaleCurves(H3DAnimationElement Elem)
        {
            H3DBone SklBone = Skeleton.FirstOrDefault(x => x.Name == Elem.Name);
            H3DBone Parent;
            H3DAnimationElement PElem;

            if (SklBone != null && SklBone.ParentIndex != -1)
            {
                Parent = Skeleton[SklBone.ParentIndex];
                PElem = SklAnim.Elements.FirstOrDefault(x => x.Name == Parent.Name);
            }
            else
            {
                return;
            }


            ANIMCurve<Vector3> curves = new ANIMCurve<Vector3>(GetBonePath(Elem.Name));

            for (int Frame = 0; Frame < FramesCount; Frame++)
            {
                Vector3 InvScale = Vector3.One;

                switch (Elem.Content)
                {
                    case H3DAnimTransform Transform:
                        //Compensate parent bone scale (basically, don't inherit scales)
                        if (Parent != null && (SklBone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0)
                        {
                            if (PElem != null)
                            {
                                H3DAnimTransform PTrans = (H3DAnimTransform) PElem.Content;

                                InvScale /= new Vector3(
                                    PTrans.ScaleX.Exists ? PTrans.ScaleX.GetFrameValue(Frame) : Parent.Scale.X,
                                    PTrans.ScaleY.Exists ? PTrans.ScaleY.GetFrameValue(Frame) : Parent.Scale.Y,
                                    PTrans.ScaleZ.Exists ? PTrans.ScaleZ.GetFrameValue(Frame) : Parent.Scale.Z);
                            }
                            else
                            {
                                InvScale /= Parent.Scale;
                            }
                        }

                        curves.Add(Frame / 30f,
                            InvScale * new Vector3(
                                Transform.ScaleX.Exists //X
                                    ? Transform.ScaleX.GetFrameValue(Frame)
                                    : SklBone.Scale.X,
                                Transform.ScaleY.Exists //Y
                                    ? Transform.ScaleY.GetFrameValue(Frame)
                                    : SklBone.Scale.Y,
                                Transform.ScaleZ.Exists //Z
                                    ? Transform.ScaleZ.GetFrameValue(Frame)
                                    : SklBone.Scale.Z)
                        );
                        break;
                    case H3DAnimQuatTransform QuatTransform:
                        //Compensate parent bone scale (basically, don't inherit scales)
                        if (Parent != null && (SklBone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0)
                        {
                            if (PElem != null)
                                InvScale /= ((H3DAnimQuatTransform) PElem.Content).GetScaleValue(Frame);
                            else
                                InvScale /= Parent.Scale;
                        }

                        curves.Add(Frame / 30f, InvScale * QuatTransform.GetScaleValue(Frame));
                        break;
                }
            }

            if (curves.keyFrames.Count > 0)
                Clip.scaleCurves.Add(curves);
        }

        private void CalculateSlopes(ANIMCurve<Vector3> curves)
        {
            List<ANIMKeyFrame<Vector3>> keyFrames = curves.keyFrames;

            for (var i = 0; i < keyFrames.Count; i++)
            {
                ANIMKeyFrame<Vector3> prevKeyFrame = keyFrames[(i-1 + keyFrames.Count) % keyFrames.Count];
                ANIMKeyFrame<Vector3> keyFrame = keyFrames[i];
                ANIMKeyFrame<Vector3> nextKeyFrame = keyFrames[(i+1) % keyFrames.Count];
                
                keyFrame.inSlope = (keyFrame.value - prevKeyFrame.value)/(keyFrame.time - prevKeyFrame.time);
                keyFrame.outSlope = (nextKeyFrame.value - keyFrame.value)/(nextKeyFrame.time - keyFrame.time);
            }
        }

        private void CorrectRotation(ANIMCurve<Vector3> curves, float threshold = 250)
        {
            List<ANIMKeyFrame<Vector3>> keyFrames = curves.keyFrames;
            
            for (var i = 0; i < keyFrames.Count; i++)
            {
                ANIMKeyFrame<Vector3> prevKeyFrame = keyFrames[(i-1 + keyFrames.Count) % keyFrames.Count];
                ANIMKeyFrame<Vector3> keyFrame = keyFrames[i];

                if (prevKeyFrame.value.X - keyFrame.value.X > threshold)
                    keyFrame.value.X += 360;
                else if (prevKeyFrame.value.X - keyFrame.value.X < -threshold)
                    keyFrame.value.X -= 360;
                
                if (prevKeyFrame.value.Y - keyFrame.value.Y > threshold)
                    keyFrame.value.Y += 360;
                else if (prevKeyFrame.value.Y - keyFrame.value.Y < -threshold)
                    keyFrame.value.Y -= 360;
                
                if (prevKeyFrame.value.Z - keyFrame.value.Z > threshold)
                    keyFrame.value.Z += 360;
                else if (prevKeyFrame.value.Z - keyFrame.value.Z < -threshold)
                    keyFrame.value.Z -= 360;
                
            }
        }


        public void Save(string FileName)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            StringBuilder SB = new StringBuilder();
            SB.AppendLine("%YAML 1.1");
            SB.AppendLine("%TAG !u! tag:unity3d.com,2011:");
            SB.AppendLine("--- !u!74 &7400000");
            SB.AppendLine("AnimationClip:");

            SB.AppendLine($"  m_Name: {Clip.name}");
            SB.AppendLine($"  m_SampleRate: 30");
            SB.AppendLine($"  m_AnimationClipSettings: {{m_StartTime: 0, m_StopTime: {Clip.duration}}}");

            SB.AppendLine("  m_PositionCurves:");
            AppendVec3Curve(SB, Clip.positionCurves, 4);

            SB.AppendLine("  m_ScaleCurves:");
            AppendVec3Curve(SB, Clip.scaleCurves, 4);

            SB.AppendLine("  m_EulerCurves:");
            AppendVec3Curve(SB, Clip.eulerCurves, 0);
            
            File.WriteAllText(FileName, SB.ToString());
        }

        private void AppendVec3Curve(StringBuilder SB, List<ANIMCurve<Vector3>> curves, int rotationOrder)
        {
            foreach (ANIMCurve<Vector3> curve in curves)
            {
                SB.AppendLine($"  - path: {curve.path}");
                SB.AppendLine($"    attribute: {curve.attribute}");
                SB.AppendLine($"    curve:");
                SB.AppendLine($"      m_RotationOrder: {rotationOrder}");
                SB.AppendLine($"      m_Curve:");
                foreach (ANIMKeyFrame<Vector3> keyFrame in curve.keyFrames)
                {
                    SB.AppendLine($"      - time: {keyFrame.time}");
                    SB.AppendLine($"        value: {{x: {keyFrame.value.X}, y: {keyFrame.value.Y}, z: {keyFrame.value.Z}}}");
                    SB.AppendLine($"        inSlope: {{x: {keyFrame.inSlope.X}, y: {keyFrame.inSlope.Y}, z: {keyFrame.inSlope.Z}}}");
                    SB.AppendLine($"        outSlope: {{x: {keyFrame.outSlope.X}, y: {keyFrame.outSlope.Y}, z: {keyFrame.outSlope.Z}}}");
                }
            }
        }
    }
}
