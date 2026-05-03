# 模块可用性验证任务清单 (Manual Dispatch Guide)

本文档整理了当前项目需要验证的模块及其对应的测试任务。如果您发现自动化队员（Worker Agents）未能及时领取任务，请手动将以下指令发送给对应的 Agent。

---

## 任务 1：养成系统后端逻辑审计与修复
**目标角色**：`backend_dev` (后端开发)
**优先级**：High
**任务 ID**: `#76452ddc`

### 任务描述
对照 `Simulation_Game_Design.md` 审核 `scripts/Game/` 目录下的实现。
重点检查：
1. `GameManager` 的初始化流程是否健壮。
2. `TrainingModule`, `RestModule`, `EventModule` 是否实现了基本逻辑（如属性增减）。
3. 检查是否存在未处理的异常，特别是 Save/Load 路径和 NullReference。

### 验收标准
提交一份审核报告文档，修复发现的明显 Bug。

---

## 任务 2：养成系统测试用例编写与执行
**目标角色**：`qa_tester` (质量保证)
**优先级**：High
**任务 ID**: `#c8947a6a`

### 任务描述
为养成系统编写自动化或半自动化测试用例。
1. 创建 `scripts/Tests/` 目录。
2. 编写 `SimulationTest.cs`，继承自 `Node`。
3. 实现以下测试：
   - 启动新游戏并验证初始数值。
   - 模拟执行 10 个回合，验证回合推进和自动存档是否正常。
   - 验证存档读取后数值是否一致。
   - 验证 `TrainingModule` 导致属性变化。

### 验收标准
可以在 Godot 中运行该测试场景（可创建 `TestRunner.tscn`），并输出成功的日志。

---

## 任务 3：核心系统可用性验证
**目标角色**：`qa_tester` (质量保证)
**优先级**：Normal
**任务 ID**: `#48febc7a`

### 任务描述
验证核心工具类的可用性。
1. 验证 `FileIOManager` 的 JSON 和 Binary 读写。
2. 验证 `SettingsManager` 是否能正确持久化设置。
3. 验证 `ResponsiveManager` 在模拟分辨率切换时是否触发事件。

### 验收标准
提交一份核心系统可用性报告。

---

## 任务 4：编辑器节点数据同步检查
**目标角色**：`backend_dev` (后端开发)
**优先级**：Normal
**任务 ID**: `#3f49d62b`

### 任务描述
检查 `scripts/Editor/Nodes/` 下的所有节点类。
1. 确认每个节点都正确实现了 `SyncFromView` 模式。
2. 验证数据修改能正确反映到 `StoryNodeManager` 中。

### 验收标准
确保所有节点数据同步逻辑无遗漏。
