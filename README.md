一. 动画配置窗口总览
    1. AnimationAsset
    2. AnimationWindow
    3. PreviewWindow
    4. TrackWindow
    5. DetailWindow
    6. 保存

二. 动画资产配置
    Section
        功能说明
            功能
            创建
        资产配置 -> AnimationWindow
            1. 配置
            2. 预览
        代码使用
            共享|不共享
    1. Clip Transition -> 单个动画ClipTransiton
    2. Clip Transition Sequence -> 逐个播放的单个动画Transition
    3. ManualMixerTransition -> 手动混合
    4. 1D MixerTransition -> 线性混合(1D)
    5. 2D Mixer Transition -> 2D混合
    6. Montage -> 实际为动画分段
    7. Animation Slot -> 实际为AvatarMask
    8. BooleanMixerTransition -> 布尔混合
    9. IntegerMixerTransition -> 整型混合

三. 动画组件
    Section
        功能描述[官方API链接]
        代码使用
        注意事项
    1. AnimancerComponent
    2. NamedAnimancerComponent
    3. HybridAnimancerComponent
    4. SoloAnimation
    5. Animation Finite State Machine

四. 扩展
    1. AnimancerComponent扩展
    2. ITransition扩展
        Section
            ITransiton接口
            对应配置资产
            对应State
            Play使用
    3. AnimationAsset扩展
        1. Event
        2. Track
        3. AnimationAsset


# Animation-Editor
