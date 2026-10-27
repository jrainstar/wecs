---
name: gameplay
description: WECS-BATTLE 客户端 Gameplay 模块（Assets/Scripts/GameLogic/Gameplay）的架构导航与开发规范。当任务涉及 Stage 逻辑层、Behavior/BehaviorInfo、RIL 渲染指令、Translator 翻译器、View 层 World/RILBucket/Agent/Enchant、Director 导演、InputKit 输入、FP 定点数或在该模块内新增/修改功能时使用本技能，避免全仓库检索。
---

# Gameplay 模块指南

## 路径速查（不要全局搜索，直接读这些）

所有路径相对于 `Assets/Scripts/GameLogic/Gameplay/`。

| 关注点 | 文件 |
|---|---|
| 引擎根组件 | `Common/Core/Engine.cs`（`eventor` / `random` / `ticker` / `u3dkit`） |
| 组件基类 | `Common/Core/Comp.cs`（`AddComp<T>` / `GetComp<T>` / `Create` / `Destroy`） |
| 事件派发 | `Common/Kits/EventorKit.cs`（`Listen` / `UnListen` / `Tell`） |
| 帧驱动 | `Common/Kits/TickerKit.cs`（`TickEvent` 渲染帧、`FixedTickEvent` 逻辑帧） |
| Unity 工具 | `Common/Kits/U3DKit.cs` + `Common/Kits/U3DKit/InputKit.cs` + `Common/Kits/U3DKit/Input/Keyboard.cs` |
| 全部常量 | `Common/Defines/`（`RIL_DEFINE` `INPUT_DEFINE` `TAG_DEFINE` `ACTOR_DEFINE` `CAPACITY_DEFINE` `STATE_DEFINE` `ANIM_DEFINE` `ATTRIBUTE_DEFINE`） |
| 定点数学 | `Common/MathF/`（`FP` `FPVector2/3/4` `FPMath` `FPQuaternion` `IntVector2/3`） |
| 逻辑容器 | `Logic/Core/Stage.cs` |
| 行为基类 | `Logic/Core/Behavior.cs`、`Logic/Core/BehaviorInfo.cs` |
| 行为实现 | `Logic/Behaviors/`、`Logic/Behaviors/Stage/` |
| 行为数据 | `Logic/BehaviorInfos/`、`Logic/BehaviorInfos/Stage/` |
| 渲染指令 | `Logic/RIL/`（`IRIL` 定义在 `Logic/RIL/Common/IRIL.cs`） |
| 翻译器 | `Logic/Translators/`（基类 `Logic/Translators/Common/Translator.cs`） |
| 预制创建器 | `Logic/Prefabs/`（基类 `Logic/Prefabs/Common/Prefab.cs`，如 `HeroPrefab.cs`） |
| 输入指令 | `Logic/Commands/` |
| 导演 | `Director/Common/BaseDirector.cs`、`Director/LocalDirector.cs` |
| 渲染容器 | `View/Core/World.cs`、`View/Core/Agent.cs` |
| 状态桶 | `View/Resolvers/Common/RILBucket.cs`、`View/Resolvers/Common/AgentEnchant.cs` |
| 具体赋能 | `View/Resolvers/Enchants/`（`NodeEnchant` `ModelEnchant` `AnimationEnchant`） |
| 代理实现 | `View/Agents/`（`NodeAgent.cs` `ModelAgent.cs`） |
| 转换扩展 | `View/Extensions/VectorExtension.cs`（`FPVector3.ToVector3()`、`ToFPVector3()`） |
| 测试入口 | `Test/WECS_0.cs` |

## 三层架构

```
Engine (Common)
 ├─ ticker  → TickEvent(渲染帧) / FixedTickEvent(逻辑帧)
 └─ u3dkit.input (InputKit) ← keyboard
        │
Director (BaseDirector / LocalDirector)
 ├─ 逻辑：Stage.Step()         ← FixedTickEvent
 ├─ 渲染：world.rilbucket.SetRIL() ← TickEvent
 └─ 桥接：stage.onril → queue → World
        │
Logic (Stage)                        View (World)
 Behavior + BehaviorInfo              RILBucket (rildict/rilqueue)
      │  Translator                     │  AgentEnchant
      └──→ IRIL ──onril──→ queue ──→ SetRIL ──→ Agent (NodeAgent / ModelAgent)
```

### 逻辑层要点

- `Stage` 本身也是一个特殊 Actor，`stage.sa == ulong.MaxValue`；`StageInfo` 挂在 `sa` 上。
- 行为按 `Actor` 组织：`Stage.AddBehavior<T>(actor)`、`Stage.SeekBehavior<T>(actor, out var b)`、`Stage.GetBehavior<T>(actor, true)`（force）。
- 数据按类型组织：`Stage.AddBehaviorInfo<T>(actor)`、`Stage.SeekBehaviorInfo<T>(actor, out var info)`、`RmvBehaviorInfo`。数据集中在 `StageInfo.behaviorinfos`（`Dictionary<Type, List<BehaviorInfo>>`）。
- `Behavior<T>` 会自动保证对应的 `BehaviorInfo` 存在（`OnAddBehaviorInfo` / `OnRemoveBehaviorInfo`），通过 `info` 属性快捷访问。
- 行为回调：`OnAssemble` / `OnDisassemble` / `OnAddBehaviorInfo` / `OnTick(FP tick)` / `OnEndTick`。`OnEndTick` 是全部 Actor 逻辑帧末统一调用，适合清理一次性状态（例如 `Gamepad` 清 `release` 标记）。
- 逻辑层必须使用 `FP` / `FPVector2` / `FPVector3` 定点数，禁止 `float` / `Vector3`，保证确定性和快照回滚。
- 新增 Actor 类型：继承 `Prefab<T>`，实现 `type`（`ACTOR_DEFINE`）和 `OnProcessing(actor, info)`，在 `Stage` 创建流程里注册。

### 渲染指令（RIL）链路

1. 逻辑层 `Translator<T, E>` 在 `OnEndTick → RILSync.Translate()` 时对比 `BehaviorInfo` 哈希，变化才生成 `E : IRIL`，`ril.Ready(actor, hashcode)` 后 `stage.rilsync.Send(ril)`。
2. `LocalDirector.OnRIL` 加锁入队，`OnTick` 出队到 `world.rilbucket.SetRIL(ril)`。
3. `RILBucket` 按 `RIL_DEFINE.*`（`ushort`）分发给注册的 `AgentEnchant`，Enchant 负责 `EnsureAgent` / `RmvAgent`。
4. `Agent.WatchRIL<T>(OnRILxxx)` 关心单类型，`World.OnTick` 对未 `Arrived` 的 Agent 调 `Chase`，`OnArrived()` 返回 true 后进入 `Arrived` 不再逐帧调用。

**新增一条 RIL 的完整清单**：

- `Common/Defines/RIL_DEFINE.cs` 加 `ushort` 常量
- `Logic/RIL/RIL_XXX.cs` 实现 `IRIL`（`Ready` / `Reset` / `Clone` / `actor` / `hashcode`）
- `Logic/BehaviorInfos/XXXInfo.cs` 实现 `BehaviorInfo`（`OnReady` / `OnReset` / `OnClone`，必要时重写 `OnCalcHashCode`）
- `Logic/Translators/XXXTranslator.cs` 继承 `Translator<XXXTranslator, RIL_XXX>`，实现 `id`、`OnCalcHashCode`、`OnRIL`；`once` 为 true 表示只发一次
- 在 `RILSync.ManualTranslators()` 里 `Translator<XXXTranslator, XXXInfo>()`
- `View/Resolvers/Enchants/XXXEnchant.cs` 继承 `AgentEnchant<RIL_XXX>`，实现 `OnRIL`
- 在 `RILBucket.Enchants()` 里 `Enchant<XXXEnchant>(RIL_DEFINE.XXX)`
- `View/Agents/XXXAgent.cs` 继承 `Agent`，`OnReady` 里 `WatchRIL<RIL_XXX>`，用 `world.rilbucket.SeekRIL<RIL_XXX>(actor, out var ril)` 读当前值

### 渲染层要点

- `World : Comp`：`eventor`、`ticker`、`rilbucket`、`selfseat`、`self`；`OnTick` 遍历 `agentdict` 驱动 `Chase`。在遍历期间禁止增删 Agent，需要时先收集到临时列表。
- `Agent` 生命周期：`Ready(actor, world)` → `OnReady` → `Flash`；`Reset` → `OnReset`；`Chase` → `OnChase` + `OnArrived`。注意 `Ready` 里会自动 `Flash()`，`Reset` 里会回收所有 `Invoker`，`OnReset` 不要再遍历 `rilactions`。
- `Agent` 必须可被 `ObjectPool.Ensure<T>()` 复用（无参构造 + `Reset` 里彻底清字段），`WatchRIL` 在 `OnReady` 里重建。
- 位置/旋转/缩放的唯一驱动点是 `NodeAgent.ApplySpatial()`（由 `OnArrived` / `OnFlash` 调用）；`ModelAgent` 只负责在 `NodeAgent.go` 下挂模型，不直接写 Transform。
- RIL 数据读取统一用 `world.rilbucket.SeekRIL<T>(actor, out var ril)`，不要缓存 RIL 实例。
- `GameObject` 一律走 `ObjectPool.Get<GameObject>(key)` / `ObjectPool.Set(go, key)`，并挂到静态 root（如 `Node` / `ModelPool`）下做隔离。

### 输入链路

```
Keyboard (UnityEngine.InputSystem, 每帧读设备)
   ↓ joystick / look / GetKey
InputKit.Bind() 注册 输入类型 → 读取器；OnTick 刷新 inputdict
   ↓ GetInput(INPUT_DEFINE.XXX) → (bool press, IntVector2 dire)
LocalDirector.OnStep (FixedTickEvent) → stage.SetInput(world.selfseat, type, press, dire)
   ↓
Stage → Gamepad.SetInput → GamepadInfo.inputdict → Behavior.GetInput
```

- `InputKit` 用 `TickEvent` 刷新，`LocalDirector.OnStep` 用 `FixedTickEvent` 取用，天然存在取最新值的语义；需要严格一致时把 `InputKit` 改成监听 `FixedTickEvent`。
- 新增按键：在 `INPUT_DEFINE` 加常量 → `InputKit.Bind()` 加读取器 → `Gamepad.ClearReleaseTokenAll()` 补一行 → 逻辑行为里 `gamepad.GetInput(INPUT_DEFINE.XXX)`。
- `Keyboard` 类名与 `UnityEngine.InputSystem.Keyboard` 冲突，内部一律使用全限定名。
- 坐标顺序：屏幕/摇杆 `IntVector2(x, y)` 映射到世界为 `new FPVector3(dire.x, 0, dire.y)`。

## 常见坑

- 逻辑层 `Movement` 之类只在 `joystick.press` 为 true 时移动；`press` 由 `dire != Vector2.zero` 推导。全程不动先查 `InputKit.inputdict` 是否有值，再查 `stage.SetInput` 的座位号是否为 `world.selfseat`（不是 `stage.sa`）。
- `Stage.SetInput(seat, ...)` 与 `Stage.SeekBehavior` 用的是 ActorID；座位号需经 `Seat` 映射到 ActorID（`RIL_SEAT.seatdict`）。
- `RILSync` 的哈希缓存 `Query(actor, id)` 返回 -1 表示未发送过；修改 `BehaviorInfo` 字段后会自然触发重发，不需要手动调用 Send。
- 渲染层只在 `rendering == true`（`stage.state == StageState.Ticking` 且不在恢复中）时被驱动，逻辑暂停时画面不会更新。
- `FP` 与 `float` 互转：`fp.AsFloat()` / `(FP)someInt`，常量 `FP.One` / `FP.Zero`；`IntVector` 与 `FPVector` 的换算是 `* ConfigKit.Float2Int`（1000 倍）。
- `Common/MathF/` 下三个 `*Lut.cs` 为大型查表文件，禁止读取或检索。
- 快照/回滚：`stage.Snapshot()` 深拷贝 `BehaviorInfo`（`OnClone`），新增数据必须实现 `OnClone`，否则回滚会丢字段。
