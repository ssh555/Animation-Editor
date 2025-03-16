using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ML.Engine.Animation
{
    [CreateAssetMenu(menuName = "ML/Animation/Mixer/Boolean Mixer Transition", order = 1)]
    public class BooleanMixerTransitionAsset : AnimationAssetBase
    {
        public bool Boolean;
        public AnimationAssetBase falseTransition;
        public AnimationAssetBase trueTransition;

        public override ITransition GetTransition()
        {
            return Boolean ? trueTransition?.GetTransition() : falseTransition?.GetTransition();
        }

        public override ITransition GetPreviewTransition()
        {
            return Boolean ? trueTransition?.GetPreviewTransition() : falseTransition?.GetPreviewTransition();
        }
    }
}

