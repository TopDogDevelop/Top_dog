# 实时交战宇宙背景（第二银河天空盒）



来源：`e:\sg\decrypted\第二银河图片` 中主宇宙 Cubemap 六面贴图。



| 目录 | 用途 |

|------|------|

| `Main/` | 主宇宙 5 套（U/O/R/S/N），**进入实时交战时随机抽选** |

| `Reserve/` | 虫洞 / ProjectX / 星云等特殊空间，**仅作备用素材**，不参与随机 |



## 看到背景的全流程（逐项自检）



| # | 环节 | 代码 / 资产 |

|---|------|-------------|

| 1 | 进入 **实时交战** 且为 **战斗视野**（非星图） | `combatRealtimeActive` · `CombatViewModeState != StarMap` |

| 2 | 为当前战场随机套系 | `CombatSpaceBackgroundState.EnsureForBattlefield` |

| 3 | 六面 PNG 存在 | `Assets/Art/CombatBackgrounds/Main/{set}/*±X/Y/Z.png` |

| 4 | Cubemap 加载成功 | `CombatBackgroundCatalog.LoadCubemap` |

| 5 | 背景相机启用 | `CombatSpaceBackgroundCameraHost.SetActive(true)` |

| 6 | 视口尺寸有效 | `tactical-viewport-host.worldBound` ≥ 64×64 |

| 7 | RenderTexture 创建 | 相机 `targetTexture` → `art-viewport-bg` UITK `Image` |

| 8 | 渲染模式（三级回退） | ① `Skybox/Cubemap` RT → ② `TopDog/CombatSkyboxInterior` 内翻球 RT → ③ `equirect.png` UITK 平移 |
| 9 | RT 内容探测 | `RenderTextureHasSkyContent`：中心像素过暗则尝试下一级 |
| 10 | 每帧 orbit + FOV | Y/X 分层旋转 + `ClientGameSettings.CombatVerticalFovDeg` |
| 11 | RT 分辨率上限 | `ClientGameSettings.CombatBackgroundMaxResolution` |
| 12 | UI 层 | `art-viewport-bg` → RT `Image` 或 equirect `Image`（`.rtcombat-space-bg`） |

失败时 Console 搜 `TopDog: combat sky`；成功日志示例：`TopDog: combat sky loaded U_Skybox_01 (SkyboxClear)`。



## SG 原始实现（il2cpp 对照）

| 第二银河 | TopDog 对应 |
|----------|-------------|
| `SolarSystemCelestialLayerController.SetSkyBoxMaterial` — 六面 Cubemap → `Skybox/Cubemap` | `CombatBackgroundCatalog.LoadCubemap` → `ApplySkyMaterial` |
| `SolarSystemCameraTransformController` — `m_yRotationRoot` / `m_xRotationRoot` orbit | `SyncOrbitAndZoom` — Y/X 分层旋转 |
| 主相机 `CameraClearFlags.Skybox` + `Skybox` 组件 | 同左；Skybox RT 全黑时内翻球 + `TopDog/CombatSkyboxInterior` |
| 每帧相机旋转，背景随视角变化 | `LateUpdate` → `SyncOrbitAndZoom` → `Camera.Render()` → RT → UITK |
| （无对应） | RT 仍黑时用 `equirect.png` + yaw/pitch 平移作末级回退 |

TopDog 因战斗 UI 为 UITK，额外增加 **RenderTexture → art-viewport-bg** 一步（SG 为全屏 3D 相机直出）。

## 重新生成



```powershell

& e:\game_dev\top_dog_unity\scripts\copy_combat_backgrounds.ps1

```



代码：`CombatBackgroundCatalog` · `CombatSpaceBackgroundCameraHost` · `CombatSpaceBackgroundState` · `CombatSpaceBackgroundPresenter`

