# EraDream 扩展包系统规格说明书 (Extension Specification)

## 1. 概述
`EraDream` 采用模块化的扩展包系统，允许玩家和创作者通过 `.umaext` 文件添加新角色、媒体资源以及自定义养成玩法。

## 2. 核心原则：包类型隔离
系统严格区分两类扩展包，以确保稳定性和安全性：

| 特性 | 马娘资源包 (Character Pack) | 玩法剧本扩展包 (Gameplay Pack) |
| :--- | :--- | :--- |
| **用途** | 添加新角色立绘、基础数值、专属剧情 | 修改养成公式、新增数值系统、自定义 UI |
| **代码注入** | **禁止** (逻辑目录将被忽略) | **允许** (支持实现 `IUmaPlugin` 的 DLL) |
| **资源挂载** | 支持 (Assets 目录) | 支持 (Assets 目录) |
| ** manifest 类型** | `character` | `gameplay` |

---

## 3. 命名空间与数值 ID 规范
为了防止多个扩展包之间的数值冲突，所有新增的属性、状态或状态机 ID 必须遵循以下规范：
*   **格式**：`[命名空间]:[属性ID]`
*   **示例**：`sirius:fatigue` (西里乌斯的疲劳度), `official:fan_count` (官方粉丝数)
*   **要求**：命名空间通常为作者名或扩展包的唯一 ID。禁止直接使用 `speed`, `power` 等基础属性名作为扩展 ID。

---

## 4. 目录结构规范
解压后的 `.umaext` 包（或开发中的文件夹）必须符合以下结构：

```text
📦 Extension_Root/
 ┣ 📜 manifest.json         # 核心清单文件
 ┣ 📂 Data/                 # 静态数据定义
 ┃ ┣ 📜 stats.json          # 角色初始数值 (Character Pack 专用)
 ┃ ┗ 📜 events.json         # 剧本事件树定义
 ┣ 📂 Assets/               # 多媒体资源
 ┃ ┣ 📂 Sprites/            # 立绘 (ImageTexture)
 ┃ ┣ 📂 Backgrounds/        # 背景图/视频
 ┃ ┗ 📂 Audio/              # BGM/SE/语音
 ┣ 📂 Logic/                # 逻辑注入 (仅 Gameplay Pack 生效)
 ┃ ┣ 📜 ModEntry.dll        # 实现了 IUmaPlugin 接口的编译代码
 ┃ ┗ 📜 dependencies.json   # DLL 依赖声明
 ┗ 📜 README.md             # 创作者提供的说明文档
```

### 4.1 manifest.json 样例
```json
{
  "id": "com.example.new_uma",
  "name": "传说中的马娘包",
  "author": "ExampleAuthor",
  "version": "1.0.0",
  "type": "character",
  "description": "添加了一个具有全新数值逻辑的角色。",
  "min_game_version": "0.5.0"
}
```

---

## 5. 开发接口 (API)
### 5.1 逻辑钩子 (`IUmaPlugin`)
玩法包可以通过实现以下接口介入游戏流程：
*   `OnLoad()`: 注册自定义属性 ID。
*   `OnScenarioStart()`: 初始化剧本变量。
*   `OnTurnStart(int turn)`: 每个回合开始时的逻辑。
*   `OnTurnEnd(int turn)`: 每个回合结算时的逻辑。

### 5.2 属性注册
```csharp
// 示例：在 OnLoad 中注册新数值
RegistryAPI.RegisterStat("my_mod:loyalty");
```

---

## 6. 加载流程
1.  **扫描期**：扫描 `user://extensions/` 目录下的 `.umaext`。
2.  **解析期**：读取 `manifest.json` 并验证版本兼容性。
3.  **激活期**：
    *   若是 `character`：仅解压并挂载资源路径。
    *   若是 `gameplay`：解压资源，并尝试反射加载 `Logic/` 下的 DLL。
