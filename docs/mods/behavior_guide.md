# EraDream 纯数据驱动行为包（behavior.json）开发指南

本指南面向模组与扩展包开发者，详细介绍在彻底移除 DLL 逻辑后，如何通过纯数据驱动的 `behavior.json` 文件扩展游戏玩法、自定义训练条目、动态注入交互菜单、绑定生命周期事件以及读写各种角色和训练员的属性。

---

## 1. 混合二合一扩展包结构

系统目前支持角色包与行为包“二合一”。无论您声明的 `type` 为何，都可以同时包含角色声明、图像音频以及玩法逻辑配置。一个标准的混合包结构如下：

```
your_extension/
├── manifest.json            # 扩展包基本元数据声明
├── Logic/
│   └── behavior.json        # 玩法逻辑、触发规则及动态菜单配置
└── Assets/
    ├── Sprites/             # 角色立绘与 UI 贴图
    └── Audio/               # 语音和效果音
```

在 `manifest.json` 中，你可以将 `type` 设为 `Mixed` 或 `Gameplay` 来启用对应的逻辑：
```json
{
  "id": "my_mixed_mod",
  "name": "极速特训混合包",
  "version": "1.0.0",
  "type": "Mixed",
  "description": "提供新的自定义马娘并新增独特的训练规则"
}
```

### 角色配置文件二合一合并与选填化
为简化配置，您无需必须创建 `simulation.json`，可以直接以内嵌 `"simulation"` 属性节点的形式，将马娘养成属性与初始数值定义写入 `actor_config.json` 文件。
并且，养成属性中的所有字段（五维、好感度、心情、精力等）均为**完全选填**。若某些属性缺省未填，系统会自动安全地采用游戏合理的 Fallback 默认值（五维默认 100，好感度默认 0，精力默认 100）。

---

## 2. 自定义训练条目（Trainings）

系统现在支持以数据配置数组的形式添加全新且任意数量的自定义训练条目，而不再局限于内置项目：

* **数据字段说明**：
  * `id` (`string`)：训练项目唯一标识符。
  * `name` (`string`)：显示在 UI 上的训练名称。
  * `description` (`string`)：训练的描述说明。
  * `stamina_cost` (`int`)：马娘行动体力的消耗（正数表示消耗，负数表示恢复）。
  * `energy_cost` (`int`)：可选，马娘或训练员在陪伴训练时额外扣减的精力值。
  * `min_stamina` (`int`)：可选，能够进入本项训练所需的马娘最低体力门槛要求。
  * `stats_rewards` (`Dictionary<string, int>`)：训练成功后所奖励的基础属性映射，包含 `"Uma.Speed"`, `"Uma.Stamina"`, `"Uma.Power"`, `"Uma.Guts"`, `"Uma.Intelligence"`, `"Uma.SkillPoints"`, `"Player.Energy"` 等核心属性名称。
  * `custom_stats_rewards` (`Dictionary<string, int>`)：可选，用于修改自定义玩法属性数值（例如 `"sirius:fatigue"`）。
  * `override` (`bool`)：设为 `true` 可以直接覆写或替换游戏原生的同名内置训练项目。

* **配置示例**：
  ```json
  "trainings": [
    {
      "id": "boost_camp_training",
      "name": "⛺ 特别合宿特训",
      "description": "极高消耗，带来全维属性飞跃的魔鬼拉练",
      "stamina_cost": 35,
      "energy_cost": 15,
      "min_stamina": 40,
      "stats_rewards": {
        "Uma.Speed": 25,
        "Uma.Stamina": 15,
        "Uma.Power": 10,
        "Uma.SkillPoints": 5
      },
      "custom_stats_rewards": {
        "sirius:fatigue": 8
      }
    }
  ]
  ```

---

## 3. 数据函数与属性读取列表（Properties）

当在行为规则中定义 `conditions` 时，您可以通过 `property` 属性读取游戏当前的状态。可读取的数据函数列表如下：

| 数据属性名 | 返回类型 | 说明 |
| :--- | :--- | :--- |
| **`Game.CurrentTurn`** | `int` | 当前的游戏回合数（从第 1 回合开始） |
| **`Player.Stamina`** | `int` | 训练员当前的体力值 |
| **`Player.Energy`** | `int` | 训练员当前的精力值 |
| **`Player.Money`** | `int` | 训练员拥有的金币数 |
| **`Uma.Speed`** | `int` | 当前签约马娘的速度属性值 |
| **`Uma.Stamina`** | `int` | 当前签约马娘的耐力属性值 |
| **`Uma.Power`** | `int` | 当前签约马娘的力量属性值 |
| **`Uma.Guts`** | `int` | 当前签约马娘的根性属性值 |
| **`Uma.Intelligence`** | `int` | 当前签约马娘的智力属性值 |
| **`Uma.SkillPoints`** | `int` | 当前签约马娘拥有的技能点数 |
| **`Uma.Mood`** | `int` | 当前签约马娘的心情值（绝不佳=10, 不佳=35, 普通=75, 良好=110, 极佳=140） |
| **`Uma.ActionStamina`**| `int` | 当前签约马娘用于行动的体力值 |
| **`Uma.Energy`** | `int` | 当前签约马娘的精力值 |
| **`Uma.CustomStats:[属性名]`** | `int` | 动态读取马娘的自定义玩法属性（例如：`Uma.CustomStats:sirius:fatigue`） |
| **`Variable:[变量名]`** | `string`/`int`| 访问当前剧本或模组中注册的全局临时自定义变量（例如：`Variable:first_meet_done`） |

---

## 4. 数值修改动作（ChangeStat）

规则触发后，可通过执行类型为 `ChangeStat` 的 `Action` 来改变前述所有属性的值。变更支持**绝对值指定**和**增量加减**。

* **支持变更的字段**：
  * 训练员属性：`Player.Stamina`, `Player.Energy`, `Player.Money`
  * 基础马娘属性：`Uma.Speed`, `Uma.Stamina`, `Uma.Power`, `Uma.Guts`, `Uma.Intelligence`, `Uma.SkillPoints`, `Uma.Mood`
  * 马娘扩展状态：`Uma.ActionStamina`, `Uma.Energy`
  * 玩法自定义属性：可以直接书写 `Uma.CustomStats:[您的自定义字段]`，也可使用前缀如 `sirius:fatigue`，系统将直接映射至马娘的自定义属性数据块。
  * 全局模组变量：`Variable:[变量名]`

* **示例**（同时提升马娘的速度并消耗马娘体力）：
  ```json
  "action": {
    "type": "ChangeStat",
    "path": "Uma.Speed",
    "value_change": 15
  }
  ```

---

## 5. 动态交互菜单（Menus）

除了新增训练项目，您还可以为已有的菜单页面动态挂载特定的按钮：

```json
  "menus": [
    {
      "menuId": "Training",
      "options": [
        {
          "id": "dynamic_meditation",
          "name": "🧘 静心冥想",
          "conditions": [
            {
              "property": "Uma.Energy",
              "operator": "<=",
              "value": "80"
            }
          ],
          "action": {
            "type": "ChangeStat",
            "path": "Uma.Energy",
            "value_change": 25
          }
        }
      ]
    }
  ]
```

---

## 6. 生命周期事件监听（Hooks）

您可以将规则绑定在特定的游戏事件点，当事件触发且条件满足时，执行对应行为。

* **支持的生命周期 Hooks**：
  * **`OnScenarioStart`**：进入剧本，新游戏正式启动时触发。
  * **`OnTurnStart`**：每回合的开端，在所有回合判定逻辑执行前触发。
  * **`OnTurnEnd`**：每回合的结尾，在判定并切入新一回合前触发。
  * **`OnContract`**：任意马娘成功与训练员签约时触发。
  * **`OnContract_[UmaId]`**：特定 ID 的马娘签约成功时触发，可用于触发专属招募剧情或注入初始属性（例如绑定 `OnContract_sirius_symboli` 触发专属入队大礼包）。
  * **`OnTraining`**：执行任意训练动作（包括内置和自定义训练）成功时触发。
  * **`OnTraining_[TrainingId]`**：执行指定 ID 的训练时触发。
  * **`OnOuting`**：执行外出游玩选项时触发。
  * **`OnRaceStart`** / **`OnRaceEnd`**：赛事开始和赛事结算时触发。

---

## 7. behavior.json 完整模组配置示例

下面提供一个标准的、开箱即用的 behavior 数据集。它演示了新赛马赛事注册、自定义特训项目、动态训练子选项、角色签约 Hook 的协同工作方式：

```json
{
  "rules": [
    {
      "id": "contract_gift_speed",
      "hook": "OnContract_sirius_symboli",
      "conditions": [],
      "probability": 1.0,
      "action": {
        "type": "ChangeStat",
        "path": "Uma.Speed",
        "value_change": 30
      }
    },
    {
      "id": "low_stamina_warning",
      "hook": "OnTurnStart",
      "conditions": [
        {
          "property": "Uma.ActionStamina",
          "operator": "<",
          "value": "20"
        }
      ],
      "probability": 0.8,
      "action": {
        "type": "BriefStory",
        "path": "提醒：马娘当前行动力较低，训练容易失败，建议休息。"
      }
    }
  ],
  "items": [
    {
      "id": "speed_booster",
      "name": "高效训练绑腿",
      "type": "Consumable",
      "description": "使用后直接为马娘增加 10 点速度值",
      "maxStack": 99,
      "price": 200
    }
  ],
  "trainings": [
    {
      "id": "boost_run",
      "name": "⛺ 越野山地拉练",
      "description": "在崎岖山道中磨砺五维，获得全方位的速度与耐力提升",
      "stamina_cost": 25,
      "energy_cost": 15,
      "min_stamina": 30,
      "stats_rewards": {
        "Uma.Speed": 15,
        "Uma.Stamina": 12,
        "Uma.Power": 8,
        "Uma.SkillPoints": 4
      }
    }
  ],
  "menus": [
    {
      "menuId": "Training",
      "options": [
        {
          "id": "dynamic_meditation",
          "name": "🧘 静心冥想",
          "conditions": [
            {
              "property": "Uma.Energy",
              "operator": "<=",
              "value": "80"
            }
          ],
          "action": {
            "type": "ChangeStat",
            "path": "Uma.Energy",
            "value_change": 25
          }
        }
      ]
    }
  ],
  "races": [
    {
      "id": "spring_cup_2026",
      "name": "春季新星对抗赛",
      "description": "只面向速度在 120 以上马娘举办的新人交流赛事",
      "turn": 12,
      "minSpeed": 120,
      "rewardStat": "Uma.SkillPoints",
      "rewardValue": 50,
      "override": false
    }
  ]
}
```
