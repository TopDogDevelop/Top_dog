# FieldAuraVfxDemo

TopDog **立场特效专项** — 最小 Unity 工程，仅展示两枚场域球：

| 左 | 右 |
|----|-----|
| 护盾融合立场 `shield_fusion_field`（蓝白） | 装甲域场 `armor_link_field`（金黄） |

与主工程同源：`TopDog/FieldAuraSphere` shader + 程序化环带纹理（`FieldAuraVfxCatalog` 同款逻辑）。

## 打开方式

1. Unity Hub → **Open** → `e:\game_dev\top_dog_unity\FieldAuraVfxDemo`
2. 等待 URP 包导入（首次较慢）
3. 打开 `Assets/Scenes/SampleScene.unity`（首次会自动生成）
4. **Play** 或直接在 Scene 视图观看两球

菜单 **TopDog → Field Aura Demo → Rebuild Showcase Scene** 可重建场景。

## 技术

- Unity **6000.5** + URP **17.0.3**
- 无游戏逻辑、无 UITK、无 RT 相机 — 纯 3D 半透明球，用于验证 shader/材质是否可见
