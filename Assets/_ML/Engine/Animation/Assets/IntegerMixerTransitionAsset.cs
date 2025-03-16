using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ML.Engine.Animation
{
    [CreateAssetMenu(menuName = "ML/Animation/Mixer/Integer Mixer Transition", order = 1)]
    public class IntegerMixerTransitionAsset : AnimationAssetBase
    {
        public int DefaultValue;
        public bool IsRandom;


        public AnimationAssetBase[] transitions;
        public override ITransition GetTransition()
        {
            ClampDefaultValue();
            if (transitions.Length > 0)
            {
                int index = IsRandom ? UnityEngine.Random.Range(0, transitions.Length) : DefaultValue;
                return transitions[index].GetTransition();
            }
            else
            {
                return null;
            }
        }
        public override ITransition GetPreviewTransition()
        {
            ClampDefaultValue();
            if (transitions.Length > 0)
            {
                return transitions[DefaultValue].GetPreviewTransition();
            }
            else
            {
                return null;
            }
        }

        public void ClampDefaultValue()
        {
            DefaultValue = Mathf.Clamp(DefaultValue, 0, transitions.Length);
        }

    }

}
