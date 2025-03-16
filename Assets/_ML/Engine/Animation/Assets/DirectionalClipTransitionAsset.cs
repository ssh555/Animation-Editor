using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ML.Engine.Animation
{
    [CreateAssetMenu(menuName = "ML/Animation/Clip/Directional Clip Transition", order = 1)]
    public class DirectionalClipTransitionAsset : AnimationAssetBase
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<DirectionalClipTransitionAsset, ClipTransition, ClipState>,
            ClipState.ITransition
        { }

        public DirectionalClipTransition transition;
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


