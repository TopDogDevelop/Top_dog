# 第二银河 · 随机团员立绘池

将 **第二银河** 人物角色立绘 PNG/JPG/WebP 放入 **本目录**（可含子文件夹）。

运行时 `MemberPortraitCatalog` 会扫描此目录；**招新随机生成** 的团员会从池中随机分配 `portraitRef`（多开同身份共用一张）。

## 绝对路径（本仓库）

```text
e:\game_dev\top_dog\content\member_portrait_templates\第二银河\
```

打包后的 `TopDog.exe` 同目录下为：

```text
{TopDog.exe 所在目录}\content\member_portrait_templates\第二银河\
```

## 路径约定

```text
content/member_portrait_templates/第二银河/*.png   ← 随机池（本目录）
core/assets/raster/members/{identityCode}.png      ← 预设团员（CSV portraitRef / 身份码）
```

## 当前状态

**目录内目前只有本 README，尚无立绘文件。** 若为空，随机招新会显示 **程序化色块头像**（每人不同）；放入第二银河立绘 PNG/JPG/WebP 后重启或重新招新即可改用真实立绘。

也可设置环境变量 `TOPDOG_PORTRAIT_POOL` 指向含 `第二银河/` 子目录的外部文件夹。
