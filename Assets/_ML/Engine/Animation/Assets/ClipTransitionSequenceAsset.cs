using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ML.Engine.Animation
{
    [CreateAssetMenu(menuName = "ML/Animation/Clip/Clip Transition Sequence", order = 1)]
    public class ClipTransitionSequenceAsset : AnimationAssetBase
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<ClipTransitionSequenceAsset, ClipTransitionSequence, ClipState>,
            ClipState.ITransition
        { }

        public ClipTransitionSequence transition;
        public override ITransition GetTransition()
        {
            return transition;
        }

        public override ITransition GetPreviewTransition()
        {
            return transition;
        }
    }

}
