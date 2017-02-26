﻿using SPICA.Formats.CtrH3D.Animation;

using System;

namespace SPICA.Renderer.Animation
{
    public class AnimControl
    {
        public float Frame;
        public float Step;

        protected H3DAnimation BaseAnimation;

        protected AnimState State;

        public bool HasData { get { return BaseAnimation != null; } }
        public bool IsLooping { get; set; }

        public AnimControl()
        {
            Step = 1;
        }

        public void SetAnimation(H3DAnimation BaseAnimation)
        {
            this.BaseAnimation = BaseAnimation;

            if (BaseAnimation == null)
            {
                Stop();

                return;
            }

            IsLooping = (BaseAnimation.AnimationFlags & H3DAnimationFlags.IsLooping) != 0;

            if (State == AnimState.Playing)
            {
                if (Step < 0)
                    Frame = BaseAnimation.FramesCount;
                else
                    Frame = 0;
            }
            else
            {
                Stop();
            }
        }

        public void AdvanceFrame()
        {
            if (BaseAnimation != null &&
                BaseAnimation.FramesCount >= Math.Abs(Step) &&
                State == AnimState.Playing)
            {
                Frame += Step;

                if (Frame < 0)
                {
                    Frame += BaseAnimation.FramesCount;
                }
                else if (Frame >= BaseAnimation.FramesCount)
                {
                    Frame -= BaseAnimation.FramesCount;
                }
            }
        }

        public void SlowDown()
        {
            if (State == AnimState.Playing && Math.Abs(Step) > 0.125f) Step *= 0.5f;
        }

        public void SpeedUp()
        {
            if (State == AnimState.Playing && Math.Abs(Step) < 8) Step *= 2;
        }

        public void Play(float Step)
        {
            this.Step = Step;

            Play();
        }

        public void Play()
        {
            State = AnimState.Playing;
        }

        public void Pause()
        {
            State = AnimState.Paused;
        }

        public void Stop()
        {
            State = AnimState.Stopped;

            Frame = 0;
        }
    }
}