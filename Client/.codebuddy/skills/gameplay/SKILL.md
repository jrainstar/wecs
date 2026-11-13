---
name: gameplay
description: WECS-BATTLE 客户端 Gameplay 模块（Assets/Scripts/GameLogic/Gameplay）的架构导航与开发规范。当任务涉及 Stage 逻辑层、Behavior/BehaviorInfo、Timeline 时间轴、Executor 指令执行器、Payload 负载、Aoe/Effect/Detection/DamageCalc/AttributeCalc、RIL 渲染指令、Translator 翻译器、View 层 World/RILBucket/Agent/Binder/Merger/Eyes 镜头、Director 导演、InputKit 输入、FP 定点数，或在该模块内新增/修改功能时使用本技能，避免全仓库检索。
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
| 配置表 | `Common/Kits/ConfigKit.cs`（`location.Tables`，`Float2Int=1000` / `Int2Float=0.001`）、`Logic/Behaviors/Stage/Config.cs`（`stage.cfg.location`） |
| Unity 工具 | `Common/Kits/U3DKit.cs`（`GetNode` / `SeekNode`） |
| 输入 | `Common/Kits/U3DKit/InputKit.cs` + `Common/Kits/U3DKit/Input/Keyboard.cs` |
| 资源 | `Common/Kits/GameRes/GameResKit.cs` + `Common/Kits/GameRes/Location.cs`（各类路径常量 + `LoadModelSync` / `LoadAnimationConfigSync`）、`Common/Kits/ResourceUtil.cs` |
| 随机器 | `Common/Kits/RandomKit.cs` |
| 对象池 | `Common/ObjectCache.cs`（纯 C# 对象，`Ensure<T>` / `Set`）、`Common/ObjectPool.cs`（含 GameObject，`Get<T>(key)` / `Set(obj,key)`） |
| RIL 对象池 | `Common/RILCache.cs` |
| 全部常量 | `Common/Defines/`（`RIL_DEFINE` `INPUT_DEFINE` `TAG_DEFINE` `ACTOR_DEFINE` `CAPACITY_DEFINE` `STATE_DEFINE` `ANIM_DEFINE` `ATTRIBUTE_DEFINE` `COLLISION_DEFINE` `DAMAGE_DEFINE` `EMITTER_DEFINE` `SKILL_DEFINE` `GAME_DEFINE`） |
| 定点数学 | `Common/MathF/`（`FP` `FPVector2/3/4` `FPMath` `FPQuaternion` `FPRandom` `IntVector2/3` `FPMatrix*`） |
| 动画数据 | `Common/Datas/Animation/`（`AnimationTransformKeyframeData` / `Exporter` / `PlayerTest`） |
| 扩展 | `Common/Extensions/FPExtension.cs` |

### Logic（逻辑层）

| 关注点 | 文件 |
|---|---|
| 逻辑容器 | `Logic/Core/Stage.cs`（含 `StageCache`、`Stage.Add/Seek/Get/Rmv*`、`Spawn`、`Step`、`Snapshot`/`Restore`、`SetInput`、`Diff`） |
| 行为基类 | `Logic/Core/Behavior.cs`、`Logic/Core/BehaviorInfo.cs` |
| Tick 时序 | `Logic/Defines/TICK_DEFINE.cs`（`TICK_TYPE_LIST` 决定逻辑帧的执行顺序） |
| 通用行为 | `Logic/Behaviors/`（`Gamepad` `Detection` `Movement` `Facade` `StateMachine` `Tag` `AttributeCalc` `Effect` `Aoe`） |
| Stage 行为 | `Logic/Behaviors/Stage/`（`Config` `Eventor` `Seat` `Random` `Timeline` `DamageCalc` `Payload` `RILSync`） |
| 角色行为 | `Logic/Behaviors/Role/SkillLauncher.cs` |
| 行为数据 | `Logic/BehaviorInfos/`（`FacadeInfo` `GamepadInfo` `MovementInfo` `SpatialInfo` `StateMachineInfo` `TagInfo` `TickerInfo` `AoeInfo` `EffectInfo` `ColliderInfo` `AttributeCalcInfo`） |
| Stage 行为数据 | `Logic/BehaviorInfos/Stage/`（`StageInfo` `EventorInfo` `RandomInfo` `SeatInfo` `TimelineInfo`） |
| 角色行为数据 | `Logic/BehaviorInfos/Role/`（`SkillInfo` `SkillLauncherInfo`） |
| **Timeline 时间轴** | `Logic/Behaviors/Stage/Timeline.cs`、`Logic/BehaviorInfos/Stage/TimelineInfo.cs` |
| **指令执行器** | `Logic/Executors/Executor.cs`、`Logic/Executors/Events/`（`PrintExecutor` `AnimationExecutor` `EmitterExecutor` `VisualExecutor` `DamageExecutor`） |
| **负载派发** | `Logic/Behaviors/Stage/Payload.cs` |
| 渲染指令 | `Logic/Instructs/RIL/`（基类 `Logic/Instructs/RIL/Common/IRIL.cs`；`DIFF/`、`EVENT/` 子目录） |
| 翻译器 | `Logic/Translators/`（基类 `Logic/Translators/Common/Translator.cs`） |
| 预制创建器 | `Logic/Prefabs/`（基类 `Common/Prefab.cs`、`Common/PrefabInfo.cs`；`HeroPrefab` `WallPrefab` `SkillPrefab` `AoePrefab`） |
| 预制数据 | `Logic/Prefabs/Datas/SpatialData.cs` |
| 输入指令 | `Logic/Commands/Common/Command.cs`、`Solider.cs` |
| 构建数据 | `Logic/Common/BuildDatas/`（`BuildData` `StageData` `PlayerData` `SceneData`） |
| 碰撞形状 | `Logic/Common/Shapes/`（`DetectionShape` `Transform`） |

### Director / View

| 关注点 | 文件 |
|---|---|
| 导演 | `Director/Common/BaseDirector.cs`、`Director/LocalDirector.cs` |
| 渲染容器 | `View/Core/World.cs`、`View/Core/Agent.cs` |
| 状态桶 | `View/Resolvers/Common/RILBucket.cs` |
| Agent 绑定（Binder） | `View/Resolvers/Common/AgentBinder.cs` + `View/Resolvers/Enchants/`（`NodeBinder` `ModelBinder` `AnimationBinder` `EffectBinder`） |
| RIL 合并器 | `View/Resolvers/Common/RILMerger.cs` + `View/Resolvers/Cross/ActorsCross.cs`（`ActorMerger`） |
| RIL 事件处理器 | `View/Resolvers/Common/RILEventHandler.cs` + `View/Resolvers/Salutes/DamageSalute.cs` |
| 代理实现 | `View/Agents/`（`NodeAgent.cs` `ModelAgent.cs` `AnimationAgent.cs` `EffectAgent.cs`） |
| 特效播放 | `View/Common/Particle/EffectController.cs` |
| **镜头** | `View/Cameras/Eyes.cs` |
| 动画配置 | `View/Common/Animation/AnimationConfig.cs`、`AnimationDropdown.cs` |
| 编辑器辅助 | `View/Common/EditorConfig.cs`、`EditorRes.cs`、`View/Common/Debug/ColliderDrawer.cs` |
| 转换扩展 | `View/Extensions/VectorExtension.cs`（`FPVector3.ToVector3()`、`IntVector3.ToFPVector3()`） |
| 测试入口 | `X/WECS_0.cs` |

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
      │  Translator                     │  AgentBinder / RILMerger / RILEventHandler
      └──→ IRIL ──onril──→ queue ──→ SetRIL ──→ Agent (Node/Model/Animation/Effect)
      Timeline + Executor ──→ 驱动 Behavior
      Payload  + Executor ──→ 一次性效果（AOE 命中 / 伤害）
```

### 逻辑层要点

- `Stage` 本身也是一个特殊 Actor，`stage.sa == ulong.MaxValue`；`StageInfo` 挂在 `sa` 上（`GetBehaviorInfo<StageInfo>(sa)` 有特判）。
- `Stage` 内置行为：`Tag`(自动) / `Config` / `Eventor` / `Seat` / `Random` / `Detection` / `Timeline` / `DamageCalc` / `Payload` / `RILSync`，快捷属性 `stage.cfg` `stage.eventor` `stage.seat` `stage.random` `stage.detection` `stage.timeline` `stage.damage` `stage.payload` `stage.rilsync`。
- 行为按 `Actor` 组织：`Stage.AddBehavior<T>(actor)`、`Stage.SeekBehavior<T>(actor, out var b)`、`Stage.GetBehavior<T>(actor, true)`（force 包含已标记删除的）。
- 数据按类型组织：`Stage.AddBehaviorInfo<T>(actor)`、`Stage.SeekBehaviorInfo<T>(actor, out var info)`、`RmvBehaviorInfo`。数据集中在 `StageInfo.behaviorinfos`（`Dictionary<Type, List<BehaviorInfo>>`），每个 Actor 只能有一个同类 BehaviorInfo。
- `Behavior<T>` 会自动保证对应的 `BehaviorInfo` 存在（`OnAddBehaviorInfo` / `OnRemoveBehaviorInfo`），通过 `info` 属性快捷访问（force=true）。
- 行为回调：`OnAssemble` / `OnDisassemble` / `OnAddBehaviorInfo` / `OnRemoveBehaviorInfo` / `OnTick(FP tick)` / `OnEndTick`。
  - `OnTick` 拿到的 `tick` 已经乘过 `TickerInfo.timescale`，**不要重复乘算**。
  - `OnEndTick` 在全部 Actor 逻辑帧末统一调用，适合清理一次性状态（例如 `Gamepad` 清 `release` 标记）。
- 删除是**延迟**的：`RmvActor` / `RmvBehavior` / `RmvBehaviorInfo` 只标记 `active=false` 并入队，`Stage.Step` 末尾 `Recycle()` 真正回收。因此同帧内旧对象仍在缓存里，`Play` 之类接口要用 `stage.cache.Valid(actor)` / `GetBehaviorInfo` 做判空。
- 逻辑层必须使用 `FP` / `FPVector2` / `FPVector3` 定点数，禁止 `float` / `Vector3`，保证确定性和快照回滚。
- 新增 Actor 类型：继承 `Prefab<T>`，实现 `type`（`ACTOR_DEFINE`）和 `OnProcessing(actor, info)`，在 `Stage.Prefabs()` 里 `Prefab<T, TInfo>()` 注册。当前注册：`WallPrefab` / `HeroPrefab` / `SkillPrefab` / `AoePrefab`。
- 新增 Behavior 若要参与逐帧驱动，**必须**登记到 `TICK_DEFINE.TICK_TYPE_LIST`（按顺序），否则 `OnTick` / `OnEndTick` 永不调用。
- 当前 `TICK_TYPE_LIST`：`Gamepad → Detection → Movement → Timeline → Aoe → SkillLauncher → Facade → Effect → RILSync`。`AttributeCalc`（查询时按 dirty 重算）、`Payload`、`DamageCalc` 不参与逐帧驱动。

### Timeline 时间轴

时间轴是一条**挂在某个 Actor 上的配置驱动流程**（技能表现、序列动画、定时事件），配置来自 `GameConf.Gameplay.TbTimelineData`。

- 数据：`TimelineInfo : BehaviorInfo` —— `id`（配置 ID）、`caster`（释放者）、`timescale`（时间缩放）、`timeelapsed`（已运行时间）、`chargeelapsed`（蓄力累计）、`charging`（蓄力中）、`lockmove`（施法期间禁止移动）、`motionapply`（是否应用动画关键帧位移）。
- 行为：`Timeline : Behavior`，是 **Stage 级** 行为，`OnTick` 遍历 `stage.SeekBehaviorInfos(out List<TimelineInfo>)` 逐条 `Execute`。
- 播放：`stage.timeline.Play(actor, timelineId, timescale, caster)`。
  - 同一 Actor 同一时间只允许一条时间轴；已有 `TimelineInfo` 时返回 `null`（不打断、不替换）。
  - `timescale <= FP.Zero` 视为暂停，不推进。
- 执行规则（`Timeline.Execute`）：
  - 本帧窗口 `[begin, end)`，其中 `begin = timeelapsed`，`delta = tick * timescale`，`end = begin + delta`。
  - 到达 `MaxTime` 的**结束帧**右端闭合：窗口为 `[begin, end]`，保证 `MaxTime` 上的节点一定触发。
  - `charging == true` 时累加 `chargeelapsed`（只增不减，`GoTo` 回跳不清零）。
  - 循环：本帧跨过 `GoTo.From` 则把 `timeelapsed` 设为 `GoTo.To` 并 `return`（本帧不再前进）。
  - 到达 `MaxTime` 则 `stage.RmvBehaviorInfo(timelineinfo)` 结束；结束时不还原动画，交给 `Movement` / `StateMachine` 后续切换。
  - 若期间被外部重播（`timeelapsed` 被重置），保留新值，不覆盖。
- 事件派发：`HandleEvent(GameEvent, caster, target)` → `executordict[事件运行时类型]` → `Executor.HandleEvent`。**未注册执行器的事件会被静默忽略**。
- 执行器无状态、被所有时间轴共享，**当前宿主通过 `stage.timeline.current` 获取**（派发期间由 `Timeline` 设置），不要在执行器里跨事件保存 Actor。

**Executor 体系**（`Logic/Executors/Executor.cs`）：

- `Executor`（抽象）：`Load(Stage)` / `Unload()` / `HandleEvent(GameEvent, caster, target)` / `HandleEvent(GameEvent)`，上下文 `caster` / `target` 每次派发覆盖。
- `Executor<T>`：类型转换后调用 `HandleEvent(T)`。
- 现有实现（`Logic/Executors/Events/`）：
  - `PrintExecutor`（`PrintEvent`）
  - `AnimationExecutor`（`AnimationEvent`）：切动画名/循环/速度，并写 `TimelineInfo.lockmove` / `motionapply`
  - `EmitterExecutor`（`EmitterEvent`）：`stage.Spawn(AoePrefabInfo)` 生成飞行 AOE
  - `VisualExecutor`（`EffectEvent`）：给 `caster` 挂 `Effect` 行为播放特效
  - `DamageExecutor`（`DamageEvent`）：`stage.damage.Apply(e, caster, target)`，仅在 `Payload` 注册
- 注册位置：`Timeline.EnsureExecutors()`（`OnAssemble` 创建，`OnDisassemble` 回收）与 `Payload.EnsureExecutors()`，写法：

```csharp
void Executor<E, T>() where E : GameEvent where T : Executor<E>, new()
{
    var executor = ObjectCache.Ensure<T>().Load(stage);
    executordict.Add(typeof(E), executor);
}
Executor<PrintEvent, PrintExecutor>();
```

- 配置侧事件类型（`GameConf/Gen/Gameplay/`）：`GameEvent`(abstract，按 `$type` 反序列化) / `PrintEvent` / `AnimationEvent` / `EmitterEvent` / `EffectEvent` / `DamageEvent`。
  - `AnimationEvent`：`Name` / `Loop` / `Speed` / `CanMove` / `ApplyMotion`。
  - `EffectEvent`：`Fx` / `Anchor` / `Follow` / `Offset` / `Rotate` / `Scale` / `Duration`。
  - `EmitterEvent`：`Aoe` / `Track` / `Speed` / `Offset` / `Delay`。
  - `DamageEvent`：伤害公式参数 + `HitCount` / `CanCrit` / `Type`，由 `DamageCalc` 解释。

**新增一个事件执行器的清单**：

1. 配置侧（Luban）已有/新增 `GameEvent` 子类，并确认 `GameConf.Gen.Gameplay.GameEvent.DeserializeGameEvent` 的 switch 里有该类型。
2. `Logic/Executors/Events/XXXExecutor.cs`：`class XXXExecutor : Executor<XXXEvent>`，实现 `HandleEvent(XXXEvent e)`；只读 `stage` / `caster` / `target`，不保存跨帧状态（执行器是单例，被所有派发方共享）。
3. 按用途注册：时间轴事件加到 `Timeline.EnsureExecutors()`，一次性命中类事件加到 `Payload.EnsureExecutors()`。
4. 需要宿主 Actor 时，时间轴用 `stage.timeline.current.actor`，一次性效果用 `caster` / `target`。

### Payload 负载

- `Payload : Behavior`（Stage 级）：`PayloadData` 是一组 `GameEvent`，由触发方（AOE 重叠、子弹命中）主动调用，`Play(id, caster, target)` 逐个交给注册的执行器。
- 与 Timeline 的区别：Timeline 是"随时间推进"的流程，Payload 是"某次命中/触发"产生的一次性效果，带明确的 `caster` / `target`。
- 当前只注册了 `DamageExecutor`；未注册的事件类型静默忽略。

### 技能（Role）

- `SkillLauncher : Behavior<SkillLauncherInfo>`：`Load(uint)` / `Load(uint, int slot)` / `Unload(uint)` / `Launch(uint)` / `Break()` / `CanLaunch(...)` / `IsCasting()` / `QueryCooldown(uint)` / `QuerySlot(int)`。
- 槽位：`SKILL_DEFINE.SLOT_COUNT` 个槽位，按键 `SKILL_DEFINE.SLOT_INPUTS`（`BA`~`BD`），`OnTick` 自动轮询按键并尝试 `Launch`（press 语义，重入由 `CanLaunch` 拦下）。
- `SkillLauncherInfo`：`skill`（当前技能）、`casting`、`timeline`（技能管线 ActorID）、`slots`、`loadedskills` / `loadedskilldict`（技能 → 技能 Actor）。
- 释放流程：`CanLaunch` 校验（冷却/配置/状态/条件/消耗）→ 切 `STATE_DEFINE.CASTING` → 校验消耗 → 进冷却 → `Timeline.Play(actor, skillData.Timeline, ...)`；时间轴播完由 `IsCasting` 收尾并刷新状态机动画。
- 技能 Actor 由 `SkillPrefab` 创建（`SkillPrefabInfo { owner, skill }`）。
- 消耗/条件目前是 TODO（`RoleResourceCost` / `RoleResourceCond` 只校验不扣减）；补功能时优先复用 Timeline，不要再造一套时间驱动。

### 战斗与表现子系统

- **数值 `AttributeCalc`**（`Logic/Behaviors/AttributeCalc.cs`）：每个单位一套。`Get(key)` / `GetBase` / `SetBase` / `AddBase` / `Add`（加法修正）/ `AddPercent` / `SetOverride` / `ClearOverride` / `ClearModifier` / `ReCalc`。公式：`最终覆盖 > (基础 + 加法) * (1 + 百分比)`，按 `dirty` 惰性重算，不登记 Tick。
- **碰撞 `Detection`**（Stage 级）：`OverlapBox/Sphere/Capsule/Cylinder`、`Raycast`、`Linecast`、`Overlap(ColliderInfo)`、`SetColliderInfo(collider, id)`、`CalcAABB`。`ColliderInfo` 支持 Box/Sphere/Capsule/Cylinder + `layer`（`COLLISION_DEFINE.QUERY`）+ `mass`（负数表示不可推动）。
- **伤害 `DamageCalc`**（Stage 级）：`Calc(e, caster, target[, out crit])` 只算不扣血，`Apply(...)` 按 `HitCount` 多段结算、扣 `ATTRIBUTE_DEFINE.HP`，并发送 `RIL_EVENT_DAMAGE` 跳字事件。减伤曲线 `def / (def + DEFENSE_BASE) * rate`，随机走 `stage.random`。
- **AOE `Aoe`**（单位级）：AOE 自身即飞行体，出生在轨道起点，按 `TbEmitterTrackData`（直线/追踪/环绕/回旋镖）与 `TbEmitterSpeedData`（进度-倍率）推进，`AoeData.Duration` 到点销毁。支持 `delay` 发射延迟、`Create/Enter/Leave/Tick/Destroy` 触发，触发时 `stage.payload.Play(PayloadId, caster, target)`。碰撞层 `COLLISION_DEFINE.LAYER_AOE`，`mass = -1`。
- **特效 `Effect`**（单位级）：逻辑层只维护 `effectdict` 与 `elapsed`，`Play(...)` 返回特效编号，到点自动移除；渲染由 `RIL_FACADE_EFFECT` + `EffectAgent` + `EffectController` 按绝对时间采样。未配时长时用 `Effect.DEFAULT_DURATION = 2s`。

### 渲染指令（RIL）链路

1. 逻辑层 `Translator<T, E>` 在 `OnEndTick → RILSync.Translate()` 时对比 `BehaviorInfo` 哈希，变化才生成 `E : IRIL`，`ril.Ready(actor, hashcode)` 后 `stage.rilsync.Send(ril)`。
2. `LocalDirector.OnRIL` 加锁入队，`OnTick` 出队到 `world.rilbucket.SetRIL(ril)`。
3. `RILBucket.SetRIL` 若 `hashcode` 与旧值相同则直接丢弃；否则替换并 `RILDispatch`。
4. `RILDispatch` 先分发给 `RIL_DEFINE` 对应的 `AgentBinder` 列表，再分发给该 Actor 上所有 `Agent.DoRIL`。
5. `Agent.WatchRIL<T>(OnRILxxx)` 关心单类型，`World.OnTick` 对未 `Arrived` 的 Agent 调 `Chase`，`OnArrived()` 返回 true 后进入 `Arrived` 不再逐帧调用。

`RILSync` 的三条出口（都在 `Translate()` 末尾统一 flush）：

| 通道 | 逻辑层发送 | 渲染层处理 |
|---|---|---|
| `IRIL`（状态） | `stage.rilsync.Send(ril)` | `RILBucket.SetRIL` |
| `IRIL_DIFF`（增量） | `stage.Diff(diff)`（内部 `rilsync.Send(diff)`，`frame <= 1` 时丢弃） | `RILBucket.SetDiff` → `RILMerger.HasNew/HasDel` |
| `IRIL_EVENT`（一次性） | `stage.rilsync.Send(e)` | `RILBucket.SetEvent` → `RILEventHandler.Handle` |

`RIL_LOSS`（`RIL_DEFINE.LOSS = 0`）由 `RILSync.SendLossRIL()` 在 BehaviorInfo 被移除时自动发出，携带 `loss = 被移除翻译器的 id`，`RILBucket` 收到后清掉对应状态并通知 `AgentBinder.OnLossRIL`。`TAG_DEFINE.ACTOR_TYPE == ACTOR_DEFINE.FLOW` 的 Actor 不参与翻译与 Loss。

**新增一条 RIL 的完整清单**：

1. `Common/Defines/RIL_DEFINE.cs` 加 `ushort` 常量。
2. `Logic/Instructs/RIL/RIL_XXX.cs` 实现 `IRIL`（`id` / `OnReady` / `OnReset`；引用类型字段在 `OnReady` 里 `RILCache.Ensure`，`OnReset` 里 `Clear + RILCache.Set`）。
3. `Logic/BehaviorInfos/XXXInfo.cs` 实现 `BehaviorInfo`（`OnReady` / `OnReset` / `OnClone`）。
4. `Logic/Translators/XXXTranslator.cs` 继承 `Translator<XXXInfo, RIL_XXX>`，实现 `id`、`OnCalcHashCode`、`OnRIL`；`protected override bool once => true` 表示只发一次。
5. 在 `RILSync.ManualTranslators()` 里 `Translator<XXXTranslator, XXXInfo>()`。
6. 若为状态型 RIL：`View/Resolvers/Enchants/XXXBinder.cs` 继承 `AgentBinder<RIL_XXX>`，实现 `OnRIL` / `OnLossRIL`（内部一般 `world.EnsureAgent<XXXAgent>` / `RmvAgent`）；在 `RILBucket.Enchants()` 里 `Enchant<XXXBinder>(RIL_DEFINE.XXX)`（同 id 可注册多个）。
7. 若为增量型：`View/Resolvers/Cross/XXXMerger.cs` 继承 `RILMerger<RIL_XXX, RIL_DIFF_XXX>`，实现 `OnHasNew` / `OnHasDel`；在 `RILBucket.Cross()` 里 `Cross<XXXMerger>(RIL_DEFINE.XXX)`。`IRIL_DIFF` 必须是纯值类型。
8. 若为事件型：`View/Resolvers/Salutes/XXXSalute.cs` 继承 `RILEventHandler<RIL_EVENT_XXX>`，实现 `OnSalute`；在 `RILBucket.Salutes()` 里 `Salute<XXXSalute>(RIL_DEFINE.EVENT_XXX)`。
9. 需要渲染载体时：`View/Agents/XXXAgent.cs` 继承 `Agent`，`OnReady` 里 `WatchRIL<RIL_XXX>`，用 `world.rilbucket.SeekRIL<RIL_XXX>(actor, out var ril)` 读当前值。

**现有 RIL 一览**（`Logic/Instructs/RIL/`）：

| id | 类型 | 翻译器 | 渲染侧 |
|---|---|---|---|
| 0 `LOSS` | `RIL_LOSS` | —（RILSync 自动发） | `RILBucket.LossRIL` |
| 1 `STAGE` | `RIL_STAGE` | `StageTranslator` | 仅存桶 |
| 2 `TICKER` | `RIL_TICKER` | `TickerTranslator` | `World.OnTick` 读 `timescale` 影响 `Chase` |
| 3 `SEAT` | `RIL_SEAT` | `SeatTranslator` | `World.self` 查座位 |
| 4 `TAG` | 常量已定义，无实现 | — | — |
| 5 `SPATIAL` | `RIL_SPATIAL` | `SpatialTranslator` | `NodeBinder` → `NodeAgent` |
| 6 `STATE_MACHINE` | `RIL_STATE_MACHINE` | `StateMachineTranslator` | 仅存桶 |
| 7 `ATTRIBUTE` | `RIL_ATTRIBUTE` | 无（待接） | — |
| 8 `ACTOR` | `RIL_ACTOR`（文件 `RIL_ACTORS.cs`） | `ActorTranslator`（`once`） | `ActorMerger` 维护 actor 列表 |
| 9 `MOTION` | 常量已定义，无实现 | — | — |
| 10 `FACADE_MODEL` | `RIL_FACADE_MODEL`（文件 `RIL_FACADE.cs`） | `FacadeModelTranslator` | `ModelBinder` → `ModelAgent` |
| 11 `FACADE_ANIMATION` | `RIL_FACADE_ANIMATION` | `FacadeAnimationTranslator` | `AnimationBinder` → `AnimationAgent` |
| 12 `FACADE_EFFECT` | `RIL_FACADE_EFFECT` | `FacadeEffectTranslator` | `EffectBinder` → `EffectAgent` |

事件（`Logic/Instructs/RIL/EVENT/`）：`RIL_EVENT_DAMAGE`(1)（`DamageSalute` 跳字）/ `RIL_EVENT_CURE`(2)（无处理器），`RIL_DEFINE.EVENT_*` 定义；增量（`Logic/Instructs/RIL/DIFF/`）：`RIL_DIFF_ACTOR`。

### 渲染层要点

- `World : Comp`：`root`（GameObject 容器）、`eventor`、`ticker`、`rilbucket`、`eyes`、`selfseat`、`self`（座位→Actor）、`SwitchSeat`；`OnTick` 遍历 `agentdict` 驱动 `Chase`。**遍历期间禁止增删 Agent**，实现方式是把 `(agent, timescale)` 先收集到临时列表。
- Agent 增删统一走 `World.EnsureAgent<T>(actor)` / `GetAgent<T>` / `GetAgents(actor)` / `RmvAgent(actor|agent)`；Binder 只负责在 RIL 到来时决定创建还是回收。
- `Agent` 生命周期：`Ready(actor, world)` → `OnReady` → `Flash`；`Reset` → `OnReset`；`Chase(tick, timescale)` → `OnChase` + `OnArrived`。
  - `Ready` 里会自动 `Flash()`，`Reset` 里会回收所有 `Invoker`，`OnReset` 不要再遍历 `rilactions`。
  - `timescale` 来自该 Actor 的 `RIL_TICKER.timescale`（未设置时为 1）。
- `Agent` 必须可被 `ObjectPool.Ensure<T>()` 复用（无参构造 + `Reset` 里彻底清字段），`WatchRIL` 在 `OnReady` 里重建。
- 位置/旋转/缩放的唯一驱动点是 `NodeAgent.ApplySpatial()`（由 `OnArrived` / `OnFlash` 调用）；`ModelAgent` 只负责在 `NodeAgent.go` 下挂模型，`AnimationAgent` 只写 `AnimancerState.Time`，`EffectAgent` 只驱动 `EffectController`，都**不直接写 Transform**。
- `NodeAgent` 用与帧率无关的指数阻尼（`SPEED = 12`）把 25fps 的逻辑位移摊平到渲染帧；`settled == false`（首帧/瞬移）时直接贴合。
- `AnimationAgent`：`animstate.Speed = 0` 关掉 Animancer 自动推进，由 `tick * timescale` 手动推 `Time`，`tarduration` 来自 `RIL_FACADE_ANIMATION.animelapsed`；混合时间按 `AnimationConfig` 的 `mixduration` / 前置混合动画计算。
- `EffectAgent`：按 `RIL_FACADE_EFFECT.effectdict` 对比增删节点，渲染帧自增采样时间，交给 `EffectController` 绝对时间采样；`lastelapsed` 用于检测逻辑时间倒退（回滚）。
- RIL 数据读取统一用 `world.rilbucket.SeekRIL<T>(actor, out var ril)`，不要缓存 RIL 实例（实例会被 `RILCache` 复用回收）。
- `GameObject` 一律走 `ObjectPool.Get<GameObject>(key)` / `ObjectPool.Set(go, key)`，并挂到静态 root（`Node` / `ModelPool` / `EffectPool`）下做隔离。

### 镜头 Eyes

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
LocalDirector.SyncInput (FixedTickEvent) → ToWorldDire(eyes.yaw)
   ↓ INPUT_DEFINE.SYNC_INPUTS 批量 stage.SetInput(world.selfseat, type, press, dire)
Seat 座位 → ActorID
Stage → Gamepad.SetInput → GamepadInfo.inputdict → Behavior 里 gamepad.GetInput
```

- `InputKit` 用 `TickEvent` 刷新，`LocalDirector.OnStep` 用 `FixedTickEvent` 取用，天然是"取最新值"语义。
- 新增按键：`INPUT_DEFINE` 加常量 → `InputKit.Bind()` 加读取器 → 需要同步到逻辑层的加入 `INPUT_DEFINE.SYNC_INPUTS` → 逻辑行为里 `gamepad.GetInput(INPUT_DEFINE.XXX)`。
- 技能按键不在 `SYNC_INPUTS` 里单独处理，而是通过 `SKILL_DEFINE.SLOT_INPUTS` 映射到槽位，由 `SkillLauncher` 轮询。
- `Gamepad.SetInput` 通过"上一帧 press 且本次未 press"推导 `release`；`OnEndTick` 统一清 `release`。
- `Keyboard` 类名与 `UnityEngine.InputSystem.Keyboard` 冲突，内部一律使用全限定名。
- 坐标顺序：摇杆 `IntVector2(x, y)` → 世界 `new FPVector3(dire.x, 0, dire.y)`；`Movement` 内 `FPMath.Atan2(dire.x, dire.z)` 求朝向。

## 快照 / 回滚

- `director.Snapshot()` → `world.Snapshot()`（记录快照后新建的 Agent）+ `stage.Snapshot()`（`StageInfo.Clone()` 深拷贝所有 BehaviorInfo）。
- `director.Restore()` → `world.rilbucket.LossAllRIL()` + 清空三个队列 + `world.Restore()`；`OnStep` 下一帧检测到 `restoreing` 才真正 `stage.Restore()`。
- `stage.Restore()` 会重建全部缓存、重新 `AddBehavior`、最后 `rilsync.Translate()` 全量重推 RIL。
- **新增 `BehaviorInfo` 必须实现 `OnClone` 并逐字段拷贝**，否则回滚后丢字段（`AoeInfo` / `AttributeCalcInfo` / `ColliderInfo` / `EffectInfo` / `TimelineInfo` 都要照此检查）。

## 常见坑

- 逻辑层 `Movement` 只在 `joystick.press` 为 true 时移动；`press` 由 `dire != Vector2.zero` 推导。全程不动先查 `InputKit.inputdict` 是否有值，再查 `stage.SetInput` 的座位号是否为 `world.selfseat`（不是 `stage.sa`）。
- `Stage.SetInput(seat, ...)` 与 `Stage.SeekBehavior` 用 ActorID；座位号需经 `Seat` 映射到 ActorID。
- `RILSync` 的哈希缓存 `Query(actor, id)` 返回 -1 表示未发送过；修改 `BehaviorInfo` 字段后自然触发重发，不需要手动 `Send`。`RILSync.Send(IRIL_DIFF)` 已标记 `[Obsolete]`，增量统一走 `Stage.Diff`。
- `Stage.Diff(IRIL_DIFF)` 在 `frame <= 1` 时直接丢弃——初始化帧的增量不会到达渲染层。
- 渲染层只在 `rendering == true`（`stage.state == StageState.Ticking` 且不在恢复中）时被驱动，逻辑暂停时画面不更新。
- `TICK_DEFINE.TICK_TYPE_LIST` 顺序见上（`Gamepad → Detection → Movement → Timeline → Aoe → SkillLauncher → Facade → Effect → RILSync`）。`Timeline` 已登记，新增会逐帧驱动的行为必须补进去。
- `Timeline.Play` 不会打断已存在的时间轴；同帧内刚播完（已 `RmvBehaviorInfo` 但未 `Recycle`）的也视为存在，需等下一帧。
- `FP` 与 `float` 互转：`fp.AsFloat()` / `(FP)someInt` / `(FP)someFloat`，常量 `FP.One` / `FP.Zero`；`IntVector` 与 `FPVector` 换算是 `* ConfigKit.Float2Int`（1000 倍），逻辑层转渲染用 `* stage.cfg.fp2int`。
- `ModelAgent` 在 `facademodel.model > 0` **或** `facademodel.resource > 0` 时创建（`ModelBinder`）；`HeroPrefab` 里 `facade.SetModel(info.hero)` 的 hero ID 必须 > 0，AOE 走 `facade.SetResource(...)`。
- 命名遗留（文件夹未跟着改名）：`View/Resolvers/Enchants/` 里的类都叫 `XxxBinder`，`Cross/` 里是 `ActorMerger`，`Salutes/` 里是 `XxxSalute`；`RILBucket` 的注册方法名仍是 `Cross()` / `Salutes()` / `Enchants()`，字典字段仍是 `crossdict` / `salutedict` / `enchantdict`。
- `HeroPrefab.cs` 里仍有一条注释写着旧名 `ModelEnchant`，实际类是 `ModelBinder`，不要被误导。
- `Common/MathF/` 下的 `*Lut.cs` 是大型查表文件，禁止读取或检索。
- `Logic/Behaviors/Stage/Config.cs` 的 `OnAssemble` 直接赋 `ConfigManager.Instance.Tables`；WebGL 下 `ConfigKit.OnCreate` 走异步字节加载分支。排查配置为空时先看 `Engine.config`。
- `View/Core/Batch.cs` 整文件被注释，`World.Batches()` 为空方法，属于预留位。
