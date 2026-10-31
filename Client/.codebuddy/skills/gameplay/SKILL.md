---
name: gameplay
description: WECS-BATTLE 客户端 Gameplay 模块（Assets/Scripts/GameLogic/Gameplay）的架构导航与开发规范。当任务涉及 Stage 逻辑层、Behavior/BehaviorInfo、Timeline 时间轴、Executor 指令执行器、RIL 渲染指令、Translator 翻译器、View 层 World/RILBucket/Agent/Enchant/Eyes 镜头、Director 导演、InputKit 输入、FP 定点数，或在该模块内新增/修改功能时使用本技能，避免全仓库检索。
---

# Gameplay 模块指南

## 路径速查（不要全局搜索，直接读这些）

所有路径相对于 `Assets/Scripts/GameLogic/Gameplay/`。

### Common（公共层）

| 关注点 | 文件 |
|---|---|
| 引擎根组件 | `Common/Core/Engine.cs`（`config` / `gameres` / `eventor` / `random` / `ticker` / `u3dkit`） |
| 组件基类 | `Common/Core/Comp.cs`（`AddComp<T>` / `GetComp<T>` / `Create` / `Destroy`） |
| 事件派发 | `Common/Kits/EventorKit.cs`（`Listen` / `UnListen` / `Tell`） |
| 帧驱动 | `Common/Kits/TickerKit.cs`（`TickEvent`/`LateTickEvent` 渲染帧，`FixedTickEvent`/`FixedLateTickEvent` 逻辑帧，`Timing` 计时器） |
| 配置表 | `Common/Kits/ConfigKit.cs`（`location.Tables`，`Float2Int=1000` / `Int2Float=0.001`） |
| Unity 工具 | `Common/Kits/U3DKit.cs`（`GetNode` / `SeekNode`） |
| 输入 | `Common/Kits/U3DKit/InputKit.cs` + `Common/Kits/U3DKit/Input/Keyboard.cs` |
| 资源 | `Common/Kits/GameRes/GameResKit.cs` + `Common/Kits/GameRes/Location.cs`（各类路径常量 + `LoadModelSync` / `LoadAnimationConfigSync`） |
| 随机器 | `Common/Kits/RandomKit.cs` |
| 对象池 | `Common/ObjectCache.cs`（纯 C# 对象，`Ensure<T>` / `Set`）、`Common/ObjectPool.cs`（含 GameObject，`Get<T>(key)` / `Set(obj,key)`） |
| RIL 对象池 | `Common/RILCache.cs` |
| 全部常量 | `Common/Defines/`（`RIL_DEFINE` `INPUT_DEFINE` `TAG_DEFINE` `ACTOR_DEFINE` `CAPACITY_DEFINE` `STATE_DEFINE` `ANIM_DEFINE` `ATTRIBUTE_DEFINE` `GAME_DEFINE`） |
| 定点数学 | `Common/MathF/`（`FP` `FPVector2/3/4` `FPMath` `FPQuaternion` `FPRandom` `IntVector2/3`） |
| 扩展 | `Common/Extensions/FPExtension.cs` |

### Logic（逻辑层）

| 关注点 | 文件 |
|---|---|
| 逻辑容器 | `Logic/Core/Stage.cs`（含 `StageCache`、`Stage.Add/Seek/Get/Rmv*`、`Step`、`Snapshot`/`Restore`、`SetInput`） |
| 行为基类 | `Logic/Core/Behavior.cs`、`Logic/Core/BehaviorInfo.cs` |
| Tick 时序 | `Logic/Defines/TICK_DEFINE.cs`（`TICK_TYPE_LIST` 决定逻辑帧的执行顺序） |
| 行为实现 | `Logic/Behaviors/`（`Gamepad` `Movement` `Facade` `StateMachine` `Tag` `Config`） |
| Stage 行为 | `Logic/Behaviors/Stage/`（`Eventor` `Seat` `Random` `RILSync` `Timeline`） |
| 角色行为 | `Logic/Behaviors/Role/SkillLauncher.cs` |
| 行为数据 | `Logic/BehaviorInfos/`（`FacadeInfo` `GamepadInfo` `MovementInfo` `SpatialInfo` `StateMachineInfo` `TagInfo` `TickerInfo`） |
| Stage 行为数据 | `Logic/BehaviorInfos/Stage/`（`StageInfo` `EventorInfo` `RandomInfo` `SeatInfo` `TimelineInfo`） |
| 角色行为数据 | `Logic/BehaviorInfos/Role/`（`SkillInfo` `SkillLauncherInfo`） |
| **Timeline 时间轴** | `Logic/Behaviors/Stage/Timeline.cs`、`Logic/BehaviorInfos/Stage/TimelineInfo.cs` |
| **Timeline 指令执行器** | `Logic/Timelines/Executor.cs`、`Logic/Timelines/Events/`（`PrintExecutor`） |
| 渲染指令 | `Logic/RIL/`（基类 `Logic/RIL/Common/IRIL.cs`；`DIFF/`、`EVENT/` 子目录） |
| 翻译器 | `Logic/Translators/`（基类 `Logic/Translators/Common/Translator.cs`） |
| 预制创建器 | `Logic/Prefabs/`（基类 `Logic/Prefabs/Common/Prefab.cs`，`HeroPrefab.cs`） |
| 输入指令 | `Logic/Commands/Common/Command.cs`、`Solider.cs` |
| 构建数据 | `Logic/Common/BuildDatas/`（`BuildData` `StageData` `PlayerData`） |

### Director / View

| 关注点 | 文件 |
|---|---|
| 导演 | `Director/Common/BaseDirector.cs`、`Director/LocalDirector.cs` |
| 渲染容器 | `View/Core/World.cs`、`View/Core/Agent.cs` |
| 状态桶 | `View/Resolvers/Common/RILBucket.cs` |
| Agent 赋能 | `View/Resolvers/Common/AgentEnchant.cs` + `View/Resolvers/Enchants/`（`NodeEnchant` `ModelEnchant` `AnimationEnchant`） |
| RIL 合并器 | `View/Resolvers/Common/RILCross.cs` + `View/Resolvers/Cross/ActorsCross.cs` |
| RIL 事件处理器 | `View/Resolvers/Common/RILSalute.cs` |
| 代理实现 | `View/Agents/`（`NodeAgent.cs` `ModelAgent.cs` `AnimationAgent.cs`） |
| **镜头** | `View/Cameras/Eyes.cs` |
| 动画配置 | `View/Common/Animation/AnimationConfig.cs`、`AnimationDropdown.cs` |
| 编辑器辅助 | `View/Common/EditorConfig.cs`、`EditorRes.cs` |
| 转换扩展 | `View/Extensions/VectorExtension.cs`（`FPVector3.ToVector3()`、`IntVector3.ToFPVector3()`） |
| 测试入口 | `Test/WECS_0.cs` |

## 三层架构

```
Engine (Common)
 ├─ ticker  → TickEvent(渲染帧) / FixedTickEvent(逻辑帧)
 ├─ config  → ConfigKit.location.Tables（Luban 配置表）
 └─ u3dkit.input (InputKit) ← Keyboard
        │
Director (BaseDirector / LocalDirector)
 ├─ 逻辑：Stage.Step()             ← FixedTickEvent → OnStep
 ├─ 渲染：world.rilbucket.SetRIL() ← TickEvent → OnTick
 ├─ 桥接：stage.onril / ondiff / onevent → 三个队列 → World
 └─ 镜头：world.eyes.yaw 把摇杆方向转世界方向
        │
Logic (Stage)                        View (World)
 Behavior + BehaviorInfo              RILBucket (rildict / rilidsdict)
      │  Translator                     │  AgentEnchant / RILCross / RILSalute
      └──→ IRIL ──onril──→ queue ──→ SetRIL ──→ Agent (Node/Model/Animation)
      Timeline + Executor ──→ 驱动 Behavior
```

### 逻辑层要点

- `Stage` 本身也是一个特殊 Actor，`stage.sa == ulong.MaxValue`；`StageInfo` 挂在 `sa` 上（`GetBehaviorInfo<StageInfo>(sa)` 有特判）。
- `Stage` 内置行为：`Tag`(自动) / `Config` / `Eventor` / `Seat` / `Random` / `RILSync`，快捷属性 `stage.cfg` `stage.eventor` `stage.seat` `stage.random` `stage.rilsync`。
- 行为按 `Actor` 组织：`Stage.AddBehavior<T>(actor)`、`Stage.SeekBehavior<T>(actor, out var b)`、`Stage.GetBehavior<T>(actor, true)`（force 包含已标记删除的）。
- 数据按类型组织：`Stage.AddBehaviorInfo<T>(actor)`、`Stage.SeekBehaviorInfo<T>(actor, out var info)`、`RmvBehaviorInfo`。数据集中在 `StageInfo.behaviorinfos`（`Dictionary<Type, List<BehaviorInfo>>`），每个 Actor 只能有一个同类 BehaviorInfo。
- `Behavior<T>` 会自动保证对应的 `BehaviorInfo` 存在（`OnAddBehaviorInfo` / `OnRemoveBehaviorInfo`），通过 `info` 属性快捷访问（force=true）。
- 行为回调：`OnAssemble` / `OnDisassemble` / `OnAddBehaviorInfo` / `OnRemoveBehaviorInfo` / `OnTick(FP tick)` / `OnEndTick`。
  - `OnTick` 拿到的 `tick` 已经乘过 `TickerInfo.timescale`，**不要重复乘算**。
  - `OnEndTick` 在全部 Actor 逻辑帧末统一调用，适合清理一次性状态（例如 `Gamepad` 清 `release` 标记）。
- 删除是**延迟**的：`RmvActor` / `RmvBehavior` / `RmvBehaviorInfo` 只标记 `active=false` 并入队，`Stage.Step` 末尾 `Recycle()` 真正回收。因此同帧内旧对象仍在缓存里，`Play` 之类接口要用 `stage.cache.Valid(actor)` / `GetBehaviorInfo` 做判空。
- 逻辑层必须使用 `FP` / `FPVector2` / `FPVector3` 定点数，禁止 `float` / `Vector3`，保证确定性和快照回滚。
- 新增 Actor 类型：继承 `Prefab<T>`，实现 `type`（`ACTOR_DEFINE`）和 `OnProcessing(actor, info)`，在 `Stage.Prefabs()` 里 `Prefab<T, TInfo>()` 注册。
- 新增 Behavior 若要参与逐帧驱动，**必须**登记到 `TICK_DEFINE.TICK_TYPE_LIST`（按顺序），否则 `OnTick` / `OnEndTick` 永不调用。

### Timeline 时间轴（新增模块）

时间轴是一条**挂在某个 Actor 上的配置驱动流程**（技能表现、序列动画、定时事件），配置来自 `GameConf.Gameplay.TbTimelineData`。

- 数据：`TimelineInfo : BehaviorInfo` —— `id`（配置 ID）、`caster`（释放者）、`timescale`（时间缩放）、`timeelapsed`（已运行时间）、`chargeelapsed`（蓄力累计）、`charging`（蓄力中）。
- 行为：`Timeline : Behavior<TimelineInfo>`，是 **Stage 级** 行为，`OnTick` 遍历 `stage.SeekBehaviorInfos(out List<TimelineInfo>)` 逐条 `Execute`。
- 播放：`stage.GetBehavior<Timeline>(stage.sa).Play(actor, timelineId, timescale, caster)`。
  - 同一 Actor 同一时间只允许一条时间轴；已有 `TimelineInfo` 时返回 `null`（不打断、不替换）。
  - `timescale <= FP.Zero` 视为暂停，不推进。
- 执行规则（`Timeline.Execute`）：
  - 本帧窗口 `[begin, end)`，其中 `begin = timeelapsed`，`delta = tick * timescale`，`end = begin + delta`。
  - 到达 `MaxTime` 的**结束帧**右端闭合：窗口为 `[begin, end]`，保证 `MaxTime` 上的节点一定触发。
  - `charging == true` 时累加 `chargeelapsed`（只增不减，`GoTo` 回跳不清零）。
  - 循环：`GoTo.From` 落在本帧窗口内则把 `timeelapsed` 设为 `GoTo.To` 并 `return`（本帧不再前进）。
  - 到达 `MaxTime` 则 `stage.RmvBehaviorInfo(timelineinfo)` 结束。
  - 若期间被外部重播（`timeelapsed` 被重置），保留新值，不覆盖。
- 事件派发：`HandleEvent(TimelineEvent)` → `executordict[事件运行时类型]` → `Executor.HandleEvent`。**未注册执行器的事件会被静默忽略**。

**Executor 体系**（`Logic/Timelines/Executor.cs`）：

- `Executor`（抽象）：`Load(Stage)` / `Unload()` / `HandleEvent(TimelineEvent)`。
- `Executor<T>`：类型转换后调用 `HandleEvent(T)`。
- 现有实现：`Logic/Timelines/Events/PrintExecutor.cs`（对应 `PrintEvent`）。
- 注册位置：`Timeline.EnsureExecutors()`（`OnAssemble` 时创建，`OnDisassemble` 回收），写法：

```csharp
void Executor<E, T>() where E : TimelineEvent where T : Executor<E>, new()
{
    var executor = ObjectCache.Ensure<T>().Load(stage);
    executordict.Add(typeof(E), executor);
}
Executor<PrintEvent, PrintExecutor>();
```

- 配置侧事件类型（`GameConf/Gen/Gameplay/`）：`TimelineEvent`(abstract，按 `$type` 反序列化) / `PrintEvent` / `AnimationEvent`。
  - `PrintEvent`：`Type`(0=Log,1=Warning,2=Error) + `Print` —— **已有执行器**。
  - `AnimationEvent`：`Name` / `Loop` / `Speed` —— **尚未实现执行器**，`HandleEvent` 会直接丢弃；要做动画事件就在这里补 `AnimationExecutor` 并注册。

**新增一个 Timeline 事件执行器的清单**：

1. 配置侧（Luban）已有/新增 `TimelineEvent` 子类，并确认 `GameConf.Gen.Gameplay.TimelineEvent.DeserializeTimelineEvent` 的 switch 里有该类型。
2. `Logic/Timelines/Events/XXXExecutor.cs`：`class XXXExecutor : Executor<XXXEvent>`，实现 `HandleEvent(XXXEvent e)`；只读 `stage` 拿 Actor/Behavior，不保存跨帧状态（执行器是单例，被所有时间轴共享）。
3. 在 `Timeline.EnsureExecutors()` 里追加一行 `Executor<XXXEvent, XXXExecutor>();`。
4. 若事件需要作用于某个 Actor，用 `TimelineInfo.caster` 或事件自带的 actor 字段定位；执行器本身没有 actor。

### 技能（Role，半成品）

- `SkillLauncher : Behavior<SkillLauncherInfo>`：`Load(uint)` / `Unload(uint)` / `Launch(uint)` 均为空实现。
- `SkillLauncherInfo`：`skill`（当前技能）、`casting`、`timeline`（技能管线 ActorID）、`loadedskills` / `loadedskilldict`（技能 → 技能 Actor）。
- `SkillInfo`：`id` + `cooldown`（float）。
- 设计意图：技能通过 `SkillData.Timeline` 指向一条 `TimelineData`，`Launch` 时 `Timeline.Play(caster, skillData.Timeline, ...)`。补功能时优先复用 Timeline，不要再造一套时间驱动。

### 渲染指令（RIL）链路

1. 逻辑层 `Translator<T, E>` 在 `OnEndTick → RILSync.Translate()` 时对比 `BehaviorInfo` 哈希，变化才生成 `E : IRIL`，`ril.Ready(actor, hashcode)` 后 `stage.rilsync.Send(ril)`。
2. `LocalDirector.OnRIL` 加锁入队，`OnTick` 出队到 `world.rilbucket.SetRIL(ril)`。
3. `RILBucket.SetRIL` 若 `hashcode` 与旧值相同则直接丢弃；否则替换并 `RILDispatch`。
4. `RILDispatch` 先分发给 `RIL_DEFINE` 对应的 `AgentEnchant` 列表，再分发给该 Actor 上所有 `Agent.DoRIL`。
5. `Agent.WatchRIL<T>(OnRILxxx)` 关心单类型，`World.OnTick` 对未 `Arrived` 的 Agent 调 `Chase`，`OnArrived()` 返回 true 后进入 `Arrived` 不再逐帧调用。

`RILSync` 的三条出口（都在 `Translate()` 末尾统一 flush）：

| 通道 | 逻辑层发送 | 渲染层处理 |
|---|---|---|
| `IRIL`（状态） | `stage.rilsync.Send(ril)` | `RILBucket.SetRIL` |
| `IRIL_DIFF`（增量） | `stage.Diff(diff)`（内部 `rilsync.Send(diff)`，`frame <= 1` 时丢弃） | `RILBucket.SetDiff` → `RILCross.HasNew/HasDel` |
| `IRIL_EVENT`（一次性） | `stage.rilsync.Send(e)` | `RILBucket.SetEvent` → `RILSalute.Salute` |

`RIL_LOSS`（`RIL_DEFINE.LOSS = 0`）由 `RILSync.SendLossRIL()` 在 BehaviorInfo 被移除时自动发出，携带 `loss = 被移除翻译器的 id`，`RILBucket` 收到后清掉对应状态并通知 `AgentEnchant.OnLossRIL`。

**新增一条 RIL 的完整清单**：

1. `Common/Defines/RIL_DEFINE.cs` 加 `ushort` 常量。
2. `Logic/RIL/RIL_XXX.cs` 实现 `IRIL`（`id` / `OnReady` / `OnReset`；引用类型字段在 `OnReady` 里 `RILCache.Ensure`，`OnReset` 里 `Clear + RILCache.Set`）。
3. `Logic/BehaviorInfos/XXXInfo.cs` 实现 `BehaviorInfo`（`OnReady` / `OnReset` / `OnClone`）。
4. `Logic/Translators/XXXTranslator.cs` 继承 `Translator<XXXInfo, RIL_XXX>`，实现 `id`、`OnCalcHashCode`、`OnRIL`；`protected override bool once => true` 表示只发一次。
5. 在 `RILSync.ManualTranslators()` 里 `Translator<XXXTranslator, XXXInfo>()`。
6. 若为状态型 RIL：`View/Resolvers/Enchants/XXXEnchant.cs` 继承 `AgentEnchant<RIL_XXX>`，实现 `OnRIL` / `OnLossRIL`；在 `RILBucket.Enchants()` 里 `Enchant<XXXEnchant>(RIL_DEFINE.XXX)`（同 id 可注册多个）。
7. 若为增量型：`View/Resolvers/Cross/XXXCross.cs` 继承 `RILCross<RIL_XXX, RIL_DIFF_XXX>`；在 `RILBucket.Cross()` 里 `Cross<XXXCross>(RIL_DEFINE.XXX)`。`IRIL_DIFF` 必须是纯值类型。
8. 若为事件型：`View/Resolvers/Salutes/XXXSalute.cs` 继承 `RILSalute<RIL_EVENT_XXX>`；在 `RILBucket.Salutes()` 里 `Salute<XXXSalute>(RIL_DEFINE.EVENT_XXX)`。
9. 需要渲染载体时：`View/Agents/XXXAgent.cs` 继承 `Agent`，`OnReady` 里 `WatchRIL<RIL_XXX>`，用 `world.rilbucket.SeekRIL<RIL_XXX>(actor, out var ril)` 读当前值。

**现有 RIL 一览**（`Logic/RIL/`）：

| id | 类型 | 翻译器 | 渲染侧 |
|---|---|---|---|
| 0 `LOSS` | `RIL_LOSS` | —（RILSync 自动发） | `RILBucket.LossRIL` |
| 1 `STAGE` | `RIL_STAGE` | `StageTranslator` | 仅存桶 |
| 2 `TICKER` | `RIL_TICKER` | `TickerTranslator` | `World.OnTick` 读 `timescale` 影响 `Chase` |
| 3 `SEAT` | `RIL_SEAT` | `SeatTranslator` | `World.self` 查座位 |
| 4 `TAG` | 常量已定义，无实现 | — | — |
| 5 `SPATIAL` | `RIL_SPATIAL` | `SpatialTranslator` | `NodeEnchant` → `NodeAgent` |
| 6 `STATE_MACHINE` | `RIL_STATE_MACHINE` | `StateMachineTranslator` | 仅存桶 |
| 7 `ATTRIBUTE` | `RIL_ATTRIBUTE` | 无（待接） | — |
| 8 `ACTOR` | `RIL_ACTOR` | `ActorTranslator`（`once`） | `ActorCross` 维护 actor 列表 |
| 9 `MOTION` | 常量已定义，无实现 | — | — |
| 10 `FACADE_MODEL` | `RIL_FACADE_MODEL` | `FacadeModelTranslator` | `ModelEnchant` → `ModelAgent` |
| 11 `FACADE_ANIMATION` | `RIL_FACADE_ANIMATION` | `FacadeAnimationTranslator` | `AnimationEnchant` → `AnimationAgent` |
| 12 `FACADE_EFFECT` | `RIL_FACADE_EFFECT`（整文件被注释） | — | 待实现 |

事件（`Logic/RIL/EVENT/`）：`RIL_EVENT_DAMAGE`(1) / `RIL_EVENT_CURE`(2)，`RIL_DEFINE.EVENT_*` 定义；增量（`Logic/RIL/DIFF/`）：`RIL_DIFF_ACTOR`。

### 渲染层要点

- `World : Comp`：`root`（GameObject 容器）、`eventor`、`ticker`、`rilbucket`、`eyes`、`selfseat`、`self`（座位→Actor）、`SwitchSeat`；`OnTick` 遍历 `agentdict` 驱动 `Chase`。**遍历期间禁止增删 Agent**，实现方式是把 `(agent, timescale)` 先收集到临时列表。
- `Agent` 生命周期：`Ready(actor, world)` → `OnReady` → `Flash`；`Reset` → `OnReset`；`Chase(tick, timescale)` → `OnChase` + `OnArrived`。
  - `Ready` 里会自动 `Flash()`，`Reset` 里会回收所有 `Invoker`，`OnReset` 不要再遍历 `rilactions`。
  - `timescale` 来自该 Actor 的 `RIL_TICKER.timescale`（未设置时为 1）。
- `Agent` 必须可被 `ObjectPool.Ensure<T>()` 复用（无参构造 + `Reset` 里彻底清字段），`WatchRIL` 在 `OnReady` 里重建。
- 位置/旋转/缩放的唯一驱动点是 `NodeAgent.ApplySpatial()`（由 `OnArrived` / `OnFlash` 调用）；`ModelAgent` 只负责在 `NodeAgent.go` 下挂模型，`AnimationAgent` 只写 `AnimancerState.Time`，都**不直接写 Transform**。
- `NodeAgent` 用与帧率无关的指数阻尼（`SPEED = 12`）把 25fps 的逻辑位移摊平到渲染帧；`settled == false`（首帧/瞬移）时直接贴合。
- `AnimationAgent`：`animstate.Speed = 0` 关掉 Animancer 自动推进，由 `tick * timescale` 手动推 `Time`，`tarduration` 来自 `RIL_FACADE_ANIMATION.animelapsed`；混合时间按 `AnimationConfig` 的 `mixduration` / 前置混合动画计算。
- RIL 数据读取统一用 `world.rilbucket.SeekRIL<T>(actor, out var ril)`，不要缓存 RIL 实例（实例会被 `RILCache` 复用回收）。
- `GameObject` 一律走 `ObjectPool.Get<GameObject>(key)` / `ObjectPool.Set(go, key)`，并挂到静态 root（`Node` / `ModelPool`）下做隔离。

### 镜头 Eyes（新增模块）

`View/Cameras/Eyes.cs`，`World` 创建时 `AddComp<Eyes>().Initialize(this).Create()`。

- 使用 **CinemachineFreeLook + CinemachineCameraOffset**，代码动态创建 `CMFreeLock` GameObject，不用 InputAction 资产。
- 输入：内部私有 `AxisInput : AxisState.IInputAxisProvider` 直接把 `Keyboard`（鼠标 `look` / `wheel`）喂给 CM 的 `m_XAxis`(0) / `m_YAxis`(1) / 推拉(2)。
- 跟随目标：`OnTick` 里 `cm.Follow = cm.LookAt = world.GetAgent<NodeAgent>(world.self).go.transform`。
- 缩放：滚轮增量累加到 `wheel`，一次性消耗；`offset.z` 钳制在 `[ZOOM_MIN=-7.5, ZOOM_MAX=0]`，用 `1 - exp(-ZOOM_DAMPING * tick)` 阻尼。
- **`eyes.yaw` 是渲染层向逻辑层回传的唯一数据**：取 `camera.transform.eulerAngles.y`。不要用 `cm.m_XAxis.Value`——CM 在 `LateUpdate` 才刷新该值，逻辑 `Step`（`FixedUpdate`）读到的是过期数据。
- `LocalDirector.OnStep` 用 `eyes.yaw` 把摇杆的相机相对方向转成世界方向（`ToWorldDire`），再由 `Stage.SetInput` 下发。`rendering == false` 时跳过转换。

## 输入链路

```
Keyboard (UnityEngine.InputSystem, 每帧读设备)
   ↓ joystick / look / wheel / GetKey
InputKit.Bind() 注册 输入类型 → 读取器；OnTick(TickEvent) 刷新 inputdict
   ↓ GetInput(INPUT_DEFINE.XXX) → (bool press, IntVector2 dire)
LocalDirector.OnStep (FixedTickEvent) → ToWorldDire(eyes.yaw) → stage.SetInput(world.selfseat, type, press, dire)
   ↓ Seat 座位 → ActorID
Stage → Gamepad.SetInput → GamepadInfo.inputdict → Behavior 里 gamepad.GetInput
```

- `InputKit` 用 `TickEvent` 刷新，`LocalDirector.OnStep` 用 `FixedTickEvent` 取用，天然是"取最新值"语义。
- 新增按键：`INPUT_DEFINE` 加常量 → `InputKit.Bind()` 加读取器 → `Gamepad.ClearReleaseTokenAll()` 补一行 → 逻辑行为里 `gamepad.GetInput(INPUT_DEFINE.XXX)`。
- `Gamepad.SetInput` 通过"上一帧 press 且本次未 press"推导 `release`；`OnEndTick` 统一清 `release`。
- `Keyboard` 类名与 `UnityEngine.InputSystem.Keyboard` 冲突，内部一律使用全限定名。
- 坐标顺序：摇杆 `IntVector2(x, y)` → 世界 `new FPVector3(dire.x, 0, dire.y)`；`Movement` 内 `FPMath.Atan2(dire.x, dire.z)` 求朝向。

## 快照 / 回滚

- `director.Snapshot()` → `world.Snapshot()`（记录快照后新建的 Agent）+ `stage.Snapshot()`（`StageInfo.Clone()` 深拷贝所有 BehaviorInfo）。
- `director.Restore()` → `world.rilbucket.LossAllRIL()` + 清空三个队列 + `world.Restore()`；`OnStep` 下一帧检测到 `restoreing` 才真正 `stage.Restore()`。
- `stage.Restore()` 会重建全部缓存、重新 `AddBehavior`、最后 `rilsync.Translate()` 全量重推 RIL。
- **新增 `BehaviorInfo` 必须实现 `OnClone` 并逐字段拷贝**，否则回滚后丢字段。

## 常见坑

- 逻辑层 `Movement` 只在 `joystick.press` 为 true 时移动；`press` 由 `dire != Vector2.zero` 推导。全程不动先查 `InputKit.inputdict` 是否有值，再查 `stage.SetInput` 的座位号是否为 `world.selfseat`（不是 `stage.sa`）。
- `Stage.SetInput(seat, ...)` 与 `Stage.SeekBehavior` 用 ActorID；座位号需经 `Seat` 映射到 ActorID。
- `RILSync` 的哈希缓存 `Query(actor, id)` 返回 -1 表示未发送过；修改 `BehaviorInfo` 字段后自然触发重发，不需要手动 `Send`。
- `Stage.Diff(IRIL_DIFF)` 在 `frame <= 1` 时直接丢弃——初始化帧的增量不会到达渲染层。
- 渲染层只在 `rendering == true`（`stage.state == StageState.Ticking` 且不在恢复中）时被驱动，逻辑暂停时画面不更新。
- `TICK_DEFINE.TICK_TYPE_LIST` 决定 `OnTick` / `OnEndTick` 的执行顺序，当前为 `Gamepad → Movement → Facade → RILSync`。**`Timeline` 尚未登记在列表里**，若要让它逐帧驱动必须补进去（建议放在 `Gamepad` 之后、`Movement` 之前，或按需放在 `RILSync` 之前）。
- `Timeline.Play` 不会打断已存在的时间轴；同帧内刚播完（已 `RmvBehaviorInfo` 但未 `Recycle`）的也视为存在，需等下一帧。
- `FP` 与 `float` 互转：`fp.AsFloat()` / `(FP)someInt` / `(FP)someFloat`，常量 `FP.One` / `FP.Zero`；`IntVector` 与 `FPVector` 换算是 `* ConfigKit.Float2Int`（1000 倍），逻辑层转渲染用 `* stage.cfg.fp2int`。
- `ModelAgent` 只在 `facademodel.model > 0` 时创建；`HeroPrefab` 里 `facade.SetModel(info.hero)` 的 hero ID 必须 > 0。
- `Common/MathF/` 下的 `*Lut.cs` 是大型查表文件，禁止读取或检索。
- `Config.OnAssemble` 里 `location` 的赋值目前是注释状态（`ConfigKit.location` 才是真实配置源），排查配置为空时先看 `Engine.config`。
- `View/Core/Batch.cs` 整文件被注释，`World.Batches()` 为空方法，属于预留位。
