# 团员随机立绘池

将任意人物立绘图片放入 **本目录或其子文件夹**（`content/member_portrait_templates/`）。

运行时 `MemberPortraitCatalog` 会 **递归扫描** 该目录下所有图片；**招新随机生成** 的团员会从池中随机分配 `portraitRef`（多开同身份共用一张）。

## 支持格式

`.png` · `.jpg` · `.jpeg` · `.webp` · `.bmp` · `.tga`

（放入即可，无需改代码或登记文件名。）

## 路径

开发：

```text
content/member_portrait_templates/
  ├── 第二银河/          ← 可保留子文件夹
  │   └── *.png
  └── 任意子目录/*.jpg
```

Unity 打包后：

```text
StreamingAssets/content/member_portrait_templates/
```

也可设置环境变量 `TOPDOG_PORTRAIT_POOL` 指向外部根目录（同样递归扫描）。

## 预设团员

CSV `portraitRef` 或 8 位 `identityCode` → 优先加载：

```text
core/assets/raster/members/{identityCode}.png
```

或 `member_portrait_templates/` 下相对路径。

## 池为空时

显示 **色块缩写占位**（`cardBackdrop` / `proceduralPortraitSeed`）；放入图片后重启或重新招新生效。
