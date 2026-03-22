# 项目脚本文件用途总结

## scripts/CharacterSprite.cs
控制角色立绘显示的 UI 组件，处理缩放、翻转、拖拽偏移以及从本地动态加载解析各种格式图片。

## scripts/Core/AppSettings.cs
定义全局应用设置的数据模型（深色模式、主音量、嵌入式窗口、安全区偏移等）。

## scripts/Core/ErrorNotifier.cs
全局错误通知的 UI 层，提供屏幕底部的 Toast 提示以及模态错误对话框功能。

## scripts/Core/FileIOManager.cs
封装跨平台原生的文件系统对话框（保存、读取、选择文件夹）调用，简化神底层 IO 交互。

## scripts/Core/GlobalGameState.cs
单例游戏状态管理器，用于在剧情运行时存储、修改和读取各种全局变量与数值（如角色的好感度等）。

## scripts/Core/ResponsiveManager.cs
全局响应式布局管理器，时刻监听屏幕尺寸变化并分发横竖屏（Landscape/Portrait）和安全区变化事件。

## scripts/Core/SafeAreaAdapter.cs
安全区适配容器组件，依据全局设置自动动态调整自身的内边距（Margin）以避开移动端刘海或圆角。

## scripts/Core/SettingsManager.cs
负责管理和持久化应用设置（保存读取 `settings.json`），实时向外层下发窗口和鼠标光标等系统设置指令。

## scripts/Core/SettingsOverlay.UI.cs
设置界面覆盖层的 UI 页面初始化部分分部类，负责创建界面排版、滑动条、以及预览安全区的效果。

## scripts/Core/SettingsOverlay.cs
设置界面覆盖层的核心逻辑处理部分，捕获用户设置调整事件并将更改流同步至 `SettingsManager`。

## scripts/Editor/AudioLibrary.cs
静态辅助库，负责扫描并获取项目包内所有存在的特定音频文件，返回列表供编辑节点生成选项用。

## scripts/Editor/BackgroundLibrary.cs
静态辅助库，检索当前工程背景图片资源目录用于供给用户或编辑节点进行下拉框选项填充。

## scripts/Editor/CharacterEditorUI.cs
角色编辑器界面的综合独立 UI 逻辑，负责管理角色列表（添加、删除），处理详情修改（名称，差分表情及首要立绘配置）。

## scripts/Editor/CharacterManager.cs
管理该项目内存运行周期内的所有角色数据（`CharacterData`），承担对特定路径下项目的反序列化、对象存盘和增删拦截功能。

## scripts/Editor/EditorScreen.cs
编织项目核心编辑区（图表式剧本编写）界面的类，统御上方全局菜单栏操作及图节点拖拽放置、连线等 Undo/Redo 环境。

## scripts/Editor/Nodes/BackgroundNode.cs
背景节点层定义基类派生实现及对应界面排版的装配逻辑。负责提供背景图下拉操作、转换类型配置与保存的持久态数据结构。

## scripts/Editor/Nodes/BaseNode.cs
所有衍生故事节点类的统领基类。含有基础全局节点坐标变量设置，以及对具体序列节点种类的 JSON 多态特例处理引导方法和基本的窗口控制。

## scripts/Editor/Nodes/BranchNode.cs
复杂节点之一，实现了判断逻辑分叉点。绑定特定的 `VariableId` 判断当前数值从而导向代表成功或失败的后备节点通道。

## scripts/Editor/Nodes/ChoiceNode.cs
玩家选项输入拦截节点与相关编辑显示，它配置有多态增加子选项列表能力并承接特殊的图形滤镜展示字段与对应的存储逻辑。

## scripts/Editor/Nodes/DialogueNode.cs
典型的“文本+说话人”展示对话框节点封装呈现。涵盖台词文本写入板、对应人物挂载接口、表情映射匹配逻辑选项、和台词语音资源绑定等。

## scripts/Editor/Nodes/EndNode.cs
指代全项目最终逻辑退出的终止点组件。允许设定为简单的结束界面过渡，或者定向启动特定独立场景以完成多幕穿插游戏设定。

## scripts/Editor/Nodes/MusicNode.cs
主抓多媒体视听播放节点，供创作者设定即将流经该时间节点时加载启用的本地原生音乐数据与对应音量幅度微调控制以及试听逻辑搭建。

## scripts/Editor/Nodes/NarrativeNode.cs
主打叙事剧情向使用的节点组件。用于排除独立角色的“纯净文字陈述场”，并附带设定专用于交代气氛转换时的深色蒙板及滤镜重塑调整功能。

## scripts/Editor/Nodes/SpriteNode.cs
细致主导屏幕角色演出的呈现组件。支持编辑人员选定某存在角色在规定版面位置展示何种差分姿势样态，实现包括出现、消失、及立绘变更的操作行为处理。

## scripts/Editor/Nodes/StartNode.cs
无明确具体操作性数据的空壳起点定基组件。代表某逻辑执行链起点（在树形关系网和图节点内提供确信的锚点参考来源）。

## scripts/Editor/Nodes/ValueNode.cs
承载核心变动指令数据交互任务组件，可令所指向游戏运行时在经历它时向指定参数加减对应的资源指标和分数数据累计（如金币/体力）。

## scripts/Editor/ProjectManager.cs
核心的工程大局维护管理者。主打实现 Godot 应用层面对于用户自主工程环境创建读写管理，同时担纲把工程封箱成外发资源压缩包或 `.era` 分发专包的核心封装动作。

## scripts/Editor/ResourceManagerUI.cs
作为中转，以通用图形界面提示形态为依托允许开发用户简便快捷的将零散存储的背景图、有声物料及立绘图形移交到工程管控系统资源文件夹之内。

## scripts/Editor/SpriteLibrary.cs
基于特定工作目录过滤获取合法图形图像并格式化呈现到下拉界面的纯享数据检索及适配服务层。

## scripts/Editor/StoryData.cs
早期框架底座性质的原始剧情串联规范。含有用于旧版本兼容的数据映射或轻度串流的数据体统筹对象模型规划声明集合。

## scripts/Editor/StoryNodeManager.cs
主导图节点与源数据绑定及内存镜像对象转换存盘功能的核心模块管理中枢。承接处理连线从属关系的捕捉还原再造。

## scripts/Editor/StoryPreviewUI.cs
快速热更及项目试玩界面的控制宿主逻辑容器。开辟专门即用即弃试作独立窗口载体验证该作品的实际播控视觉呈现。

## scripts/LoadingScreen.cs
承载了跨越多重大场景之间的视党缓解任务过场页，融合了极具特色的弹幕排版移动特效动画来缓冲实际资源装载带来的卡顿。

## scripts/MainMenuScreen.cs
框架初期的路由中枢菜单类。依托于弹性 UI 排列布局将所有分流能力：游戏启始装载项、图编辑进入入口、以及偏好调解版块整合暴露出来供交互。

## scripts/StoryPlayerEngine.cs
扮演上帝视角的剧情执行跑道控制器总集引擎，严格承接并翻译用户图表里搭建的重重时空网格关系并有序依次进行对应立绘切换调配及交互等实质反馈。

## scripts/StorySelectorScreen.cs
专向性资源检索与调度展示画面功能集类，通过遍历探测当前运行目录的有效封包，完成列表构造供游客手动指定心仪扩展剧情开启游玩动作。

## scripts/WelcomeScreen.cs
位于开机动画后的静态展示界面过渡拦截层，侦听各类基础确认操作，旨在向目标进入应用人员展现良好的第一幕感知形象。

---

## 模块分析及架构总结

本项目是一个以 Godot 4 (C#) 为基础设施的现代视觉小说剧情编辑和引擎框架，其脚本大致可划分为以下几个核心功能模块群：

### 1. 全局核心底座 (Core Module - `scripts/Core/`)
此模块包含了框架的基础设施组件。如 `SettingsManager` 结合 `AppSettings` 处理程序的持久化偏好设置；`ResponsiveManager` 和 `SafeAreaAdapter` 解决了跨平台横竖屏切换与现代移动端各种屏幕的安全区适配问题；`ErrorNotifier` 提供全局报警与 Toast 提示，整体模块职责边界清晰，采取 Autoload 或全局静态协助模式。

### 2. 节点及编辑器框架 (Editor Module - `scripts/Editor/` & `Nodes/`)
构成整个工程工作量最大的一部分，全面依靠 Godot 内置的 `GraphEdit` 进行拖拽式的逻辑树开发编排。
*   **节点抽象及表现树**：派生于 `BaseNode.cs`，从逻辑数据结构分离了 UI(`GraphNode`) 与数据载体，通过 C# JSON 继承映射实现了对于对话(`DialogueNode`)、分支(`BranchNode`)、渲染(`SpriteNode`) 和媒体配置等各异可视化指令块的安全存取展示。
*   **管理与序列化网关**：`ProjectManager`、`CharacterManager` 以及 `StoryNodeManager` 分组负责整体项目的项目结构构建、可控参与角色数据库的管理、以及图状流图转树形序列图的编排及文件持久化控制。
*   **资源检索调度模块**：以静态提取文件方式提供便利检索包装的 `AudioLibrary` 及 `SpriteLibrary` 服务支持相关节点呈现有效合规物料下拉提示支持。

### 3. 应用场景运行路由流转 (Screen/Scene Flow - `scripts/`)
涵盖了作为系统门面的中继功能逻辑页：如 `WelcomeScreen` 第一站接触交互拦截验证跳转主系统，业务承接集散中心 `MainMenuScreen` 分发操作导向，极有技术特色的弹幕过渡加载效果屏 `LoadingScreen` 实现无缝暗缝隐藏渲染迟滞效果；辅以 `StorySelectorScreen` 让引擎兼具剧本阅读客户端的能力去分发消费外部输入剧情集。

### 4. 运行时演绎引擎中心 (Runtime Engine - `scripts/StoryPlayerEngine.cs` & `CharacterSprite.cs`)
在全面剥离编辑创作外壳后提供视觉小说播放体验的具体展现处理驱动机。此脚本是真正游戏体验的核心运转枢纽，其利用遍历阅读先期解析完毕的对象化故事集合树 (`List<BaseNodeData>`)，自动流转解析指令、智能排布并控制渲染诸如文本的打印级动画抛出、选项对话的弹出回收、背景立绘及环境滤镜配置的增删覆盖应用等各类复合互动业务，完成了编辑结构数据形态到最终体验呈现画面的华丽动态展现。
