using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ML.Engine.Animation
{
    [CreateAssetMenu(menuName = "ML/Animation/Mixer/1D Mixer Transition", order = 1)]
    public class Mixer1DTransitionAsset : AnimationAssetBase, IAssetHasEvents
    {
        [Serializable]
        public new class UnShared :
            UnShared<Mixer1DTransitionAsset, LinearMixerTransition, LinearMixerState>,
            LinearMixerState.ITransition
        { }


        public LinearMixerTransition transition;
        public override ITransition GetTransition()
        {
            var events = ((IAssetHasEvents)this).GetEventsOptional(transition.Speed >= 0);
            transition.Events.CopyFrom(events);

            return transition;
        }

        public override ITransition GetPreviewTransition()
        {
            return transition;
        }

        #region IAssetHasEvents
        [SerializeField]
        protected List<IAssetHasEvents.AssetEvent> _Events;
        public List<IAssetHasEvents.AssetEvent> Events => _Events;

        public override float FrameRate
        {
            get
            {
                if (transition.Animations == null)
                    return 0;

                var framerate = 0f;

                for (int i = transition.Animations.Length - 1; i >= 0; i--)
                {
                    if (!TryGetFrameRate(transition.Animations[i], out var frame))
                        continue;

                    if (framerate < frame)
                        framerate = frame;
                }

                return Mathf.Max(framerate, 24);
            }
        }

        public override float Length
        {
            get
            {
                if (transition.Animations == null)
                    return 0;

                var duration = 0f;

                for (int i = transition.Animations.Length - 1; i >= 0; i--)
                {
                    if (!AnimancerUtilities.TryGetLength(transition.Animations[i], out var length))
                        continue;

                    if (duration < length)
                        duration = length;
                }

                return Mathf.Max(duration, 0.01f);
            }
        }

        [SerializeField]
        protected IAssetHasEvents.AssetEvent _EndEvent;
        public IAssetHasEvents.AssetEvent EndEvent => _EndEvent;

        #endregion

    }

}


