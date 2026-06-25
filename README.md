# Top Dog — Unity 6

活跃开发仓库。libGDX 版已封存于 [`e:\game_dev\top_dog`](../top_dog)（见 `ARCHIVED.md`）。

## 要求

| 项 | 版本 |
|----|------|
| Unity Editor | **6.5 (6000.5.x)** 或 **6.3 LTS (6000.3.x)** — 见下方版本说明 |
| .NET SDK | 8.0+（Core 单元测试） |
| 渲染 | URP 2D |
| UI | UI Toolkit (UXML/USS) |
| AI | `com.unity.ai.assistant` + **Unity MCP → Cursor** |

## 快速开始

### 1. 安装 Unity 6.3

Unity Hub → Installs → 本机已装 **6000.5.0f1**；用其打开 `TopDog.Unity`。

### 2. 打开工程

> **重要：必须打开子文件夹 `TopDog.Unity`，不要打开上一级 `top_dog_unity`。**
>
> 若 Hub 里添加的是 `e:\game_dev\top_dog_unity`，Console 会出现  
> `Cannot open scene at path TopDog.Unity/Assets/Scenes/Boot.unity` — 说明工程根目录选错了。

Hub → Add → **`e:\game_dev\top_dog_unity\TopDog.Unity`**

首次打开会解析 URP 与 AI 包（需联网）。

打开后若 Project 里看不到新场景/脚本：**菜单 TopDog → Refresh Project Assets**（或 Unity 自带 **Assets → Refresh**，快捷键 **Ctrl+R**）。  
`AssetDatabase.Refresh()` 仅在 Editor 脚本里调用；本仓库在 `TopDogAssetRefresh.cs` 与 MCP 工具链里会自动触发。

### 3. 同步 content

```powershell
.\scripts\sync_content_from_libgdx.ps1
```

### 4. Core 单元测试（无需 Unity Editor）

```powershell
dotnet test e:\game_dev\top_dog_unity\TopDog.sln
```

### 5. Unity MCP + Cursor（Gate A0）

见 [`docs/UNITY_MCP_SETUP.md`](docs/UNITY_MCP_SETUP.md)。

## Gate 是什么？

**Gate** 不是 Unity 术语，是 [`unity_full_port` 移植计划](c:\Users\WXH\.cursor\plans\unity_full_port_177af3be.plan.md) 里自定义的**阶段验收关卡**（类似里程碑 / quality gate）：

| Gate | 含义 |
|------|------|
| **A0** | Unity MCP + Cursor 工具链可用 |
| **A** | Core 基础（SaveCodec、地图加载）+ NUnit 与 Java 测试对齐 |
| **B** | 仿真核心 `SimulationCore` + 全部 Java 单元测试移植 |
| **C** | UI Toolkit 客户端（主菜单 → 大厅 → 战役壳）可玩 |
| **D** | 星图 / 战术 URP 渲染 |
| **E/F** | LAN 联机 + Windows 构建 + parity 验收 |

每过一个 Gate = 该阶段「能演示 / 能测 / 能合并」的最低标准。

## Unity 6.5 vs 6.3 LTS（官方口径）

依据 [Unity 6.3 LTS 公告](https://unity.com/blog/unity-6-3-lts-is-now-available)、[What's New 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html)、[What's New 6.5](https://docs.unity3d.com/6000.5/Documentation/Manual/WhatsNewUnity65.html)、[Upgrade to 6.5](https://docs.unity3d.com/6000.5/Documentation/Manual/UpgradeGuideUnity65.html)：

| | **6.3 LTS** (`6000.3`) | **6.5 Supported** (`6000.5`) |
|--|------------------------|------------------------------|
| **定位** | 长期支持版，适合锁定生产 / 即将上线 | Supported Update（原 Tech Stream 路线），Hub 常推荐给**新工程** |
| **支持周期** | 约 2 年 LTS（Enterprise 可 +1 年） | 支持到下一版发布；最终汇入 **6.7 LTS** |
| **稳定性** | 最高，补丁以 bugfix 为主 | 同样经 QA，但 API 演进更快 |
| **与本项目相关** | URP 2D、UI Toolkit、Unity AI/MCP 均可用 | 同上 + **编程破坏性更大**（见下） |

**6.5 相对 6.3 的关键差异（影响代码）：**

- `InstanceID` → `EntityId`（8 字节）；旧 `GetInstanceID()` 等标记 obsolete 且**按 error 编译**
- IMGUI `TreeView` / `TreeViewItem` 非泛型版 obsolete → 导致旧版 `com.unity.collab-proxy` 在 6.5 下编译失败（本项目已移除 collab-proxy，用 Git）
- 2D `LowLevelPhysics2D` 在 6.5 重命名为 `PhysicsCore2D`（`Unity.U2D.Physics`）
- Built-In RP 已 deprecated（我们使用 URP，无影响）
- VR Module 在 6.5 移除（manifest 勿再引用 `com.unity.modules.vr`）

**本仓库建议：** 你已装 **6000.5.0f1** 可继续用；若求稳可换 **6000.3 LTS**。无论哪版，工程必须打开 **`TopDog.Unity` 子目录**。

**编译 Safe Mode 常见原因：** `TopDog.Core` 使用 C# 10 语法，需在 asmdef 旁有 `csc.rsp`（`-langversion:10`）+ `GlobalUsings.cs`；勿把 `dotnet build` 生成的 `Assets/Scripts/Core/obj/` 留在 Unity 里。Unity **6.5** 勿锁定旧版 `com.unity.inputsystem@1.11`（TreeView CS0619）；当前 UI 阶段未用 Input System，已从 manifest 移除，Gate D 再加回 **1.19.0+**。

## 仓库结构

```text
top_dog_unity/
├── src/TopDog.Core/          # 纯 C# 仿真核心（零 UnityEngine）
├── tests/TopDog.Core.Tests/  # NUnit，golden JSON 守门
├── TopDog.Unity/             # Unity 客户端工程
├── content/                  # 游戏数据（同步自 libGDX）
├── docs/                     # 设计文档（含 GitHub 存档说明 → GITHUB_ARCHIVE.md）
└── scripts/                  # 同步与构建脚本
```

## 地图

不实现 Unity 地图编辑器。地图由 **Bug6** → `top_dog/tools/import_eve_constellation.py` → `content/map/`。

## 开发约定

- **Core** 禁止引用 `UnityEngine`
- **UI** 一律 UXML/USS + C# Controller
- 改 UI 前先改 `docs/` 专文（`DOC_WORKFLOW.md`）
