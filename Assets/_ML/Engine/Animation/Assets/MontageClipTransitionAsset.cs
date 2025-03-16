using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ML.Engine.Animation
{
    [Serializable, CreateAssetMenu(menuName = "ML/Animation/Clip/Montage Transition", order = 1)]
    public class MontageClipTransitionAsset : AnimationAssetBase, IAssetHasEvents
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<MontageClipTransitionAsset, ClipTransition, ClipState>,
            ClipState.ITransition
        { }

        public ClipTransition transition;

        /// <summary>
        /// 使用的Montage的名字，若无匹配，则播放整个Clip
        /// 使用Play播放之前需要指定播放的Montage名字
        /// </summary>
        public string DefaultName;

        public List<MontageName> montageNames;

        public override ITransition GetTransition()
        {
            var events = ((IAssetHasEvents)this).GetEventsOptional(transition.Speed >= 0);
            transition.Events.CopyFrom(events);
            return GetMontageTransition();
        }

        public override ITransition GetPreviewTransition()
        {
            return GetMontageTransition();
        }

        public void SortMontage()
        {
            // 按NormalizedTime升序排序
            montageNames.Sort((x, y) => x.NormalizedTime.CompareTo(y.NormalizedTime));
        }

        protected ITransition GetMontageTransition()
        {
            SortMontage();

            if (montageNames != null)
            {
                int i = 0;
                for (i = 0; i < montageNames.Count; ++i)
                {
                    if (montageNames[i].Name == DefaultName)
                    {
                        if (i != montageNames.Count - 1)
                        {
                            transition.NormalizedStartTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? montageNames[i].NormalizedTime : 1 - montageNames[i].NormalizedTime;
                            transition.Events.NormalizedEndTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? montageNames[i + 1].NormalizedTime : 1 - montageNames[i + 1].NormalizedTime;
                            break;
                        }
                        else
                        {
                            transition.NormalizedStartTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? montageNames[i].NormalizedTime : 1 - montageNames[i].NormalizedTime;
                            transition.Events.NormalizedEndTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? 1 : 0;
                            break;
                        }
                    }
                }
                if(i == montageNames.Count)
                {
                    transition.NormalizedStartTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? 0 : 1;
                    transition.Events.NormalizedEndTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? 1 : 0;
                }
            }
            else
            {
                transition.NormalizedStartTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? 0 : 1;
                transition.Events.NormalizedEndTime = float.IsNaN(transition.Speed) || transition.Speed >= 0 ? 1 : 0;
            }
            transition.Events.OnEnd += OnEnd;
            return transition;
        }

        protected void OnEnd()
        {
            transition.BaseState.IsPlaying = false;
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

        [Serializable]
        public class MontageName
        {
            public string Name;
            public float NormalizedTime;
        }
    }

}
