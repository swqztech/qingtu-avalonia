# qingtu-avalonia（轻图）

本地图片压缩 / 横向拼接，**Avalonia + SkiaSharp**，对齐 [qingtu-wpf](https://github.com/swqztech/qingtu-wpf) 功能。

```bash
dotnet build -c Release
dotnet run -c Release
```

输出 exe：`bin/Release/net8.0/qingtu-avalonia.exe`

## 绿色版（框架依赖，体积小）

```bash
bash packaging/build.sh          # 绿色 zip + NSIS 安装包
```

需本机已装 .NET 8 桌面运行时。安装包：`dist/轻图-avalonia-<版本>-win64-setup.exe`（需 NSIS）。
