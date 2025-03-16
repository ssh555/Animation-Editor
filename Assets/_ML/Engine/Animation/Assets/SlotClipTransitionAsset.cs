using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace ML.Engine.Animation
{
    [Serializable, CreateAssetMenu(menuName = "ML/Animation/Clip/Slot Transition", order = 1)]
    public class SlotClipTransitionAsset : AnimationAssetBase, IAssetHasEvents
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<SlotClipTransitionAsset, SlotClipTransition, SlotClipState>,
            SlotClipState.ITransition
        { }

        public SlotClipTransition transition;

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

    [Serializable]
    public class SlotClipTransition : ClipTransition, SlotClipState.ITransition
    {
        [SerializeField]
        private AvatarMask _slot;
        public ref AvatarMask Slot => ref _slot;

        SlotClipState ITransition<SlotClipState>.State => this.State as SlotClipState;

        public override void Apply(AnimancerState state)
        {
            base.Apply(state);
            BaseState.Layer.SetMask(Slot);
        }

        public override ClipState CreateState()
        {
            return (this as ITransition<SlotClipState>).CreateState();
        }

        SlotClipState ITransition<SlotClipState>.CreateState()
        {
#if UNITY_ASSERTIONS
            if (Clip == null)
                throw new ArgumentException(
                    $"Unable to create {nameof(ClipState)} because the {nameof(ClipTransition)}.{nameof(Clip)} is null.");
#endif

            var state = new SlotClipState(Clip);
            return state;
        }
    }

    [Serializable]
    public class SlotClipState : ClipState
    {
        public new interface ITransition : ITransition<SlotClipState> { }
        public SlotClipState(AnimationClip _clip) : base(_clip)
        {
        }

        protected override void OnStartFade()
        {
            base.OnStartFade();
            Layer.SetMask(null);
        }
    }

}
