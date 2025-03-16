using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ML.Engine.Animation
{
    [Serializable, CreateAssetMenu(menuName = "ML/Animation/Clip/Clip Transition", order = 1)]
    public class ClipTransitionAsset : AnimationAssetBase, IAssetHasEvents
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<ClipTransitionAsset, ClipTransition, ClipState>,
            ClipState.ITransition
        { }

        public ClipTransition transition;
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
                return transition.Clip == null ? 24 : transition.Clip.frameRate;
            }
        }

        public override float Length
        {
            get
            {
                return transition.Clip == null ? 0.01f : transition.Clip.length;
            }
        }


        [SerializeField]
        protected IAssetHasEvents.AssetEvent _EndEvent;
        public IAssetHasEvents.AssetEvent EndEvent => _EndEvent;

        #endregion

    }

}
