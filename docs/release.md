# 发布流程

## 方法一：自动发布（推荐）

推送到 GitHub 后 CI 自动构建、打包、创建 Release。

```bash
# 1. 确保 main 分支是最新代码
git checkout main
git pull

# 2. 打好版本 tag（格式：主.次.修.号，如 0.1.1.0 或 v0.1.1.0）
git tag 0.1.1.0

# 3. 推送 tag，触发 GitHub Actions
git push origin 0.1.1.0
```

CI 会自动完成：
- 下载 Dalamud CN
- 构建 Release
- 更新 `Phantom.json` 中的 `AssemblyVersion`
- 打包 `Phantom.dll + Phantom.json + Phantom.deps.json` 为 `Phantom.zip`
- 创建 GitHub Release 并上传 zip

释放后更新 `repo.json`：
- `AssemblyVersion`: 改为新版本号
- `DownloadLinkInstall/DownloadLinkTesting/DownloadLinkUpdate`: 如果是新 tag，无需修改（路径依赖 tag 名）
- `LastUpdated`: 改为当前时间（ISO 8601 格式）

## 方法二：手动发布

```bash
# 1. 构建
dotnet build

# 2. 打包（仅包含 dll + json + deps.json，不含 icon.png）
Compress-Archive -Path ./output/Phantom.dll,./output/Phantom.json,./output/Phantom.deps.json -DestinationPath ./Phantom.zip -Force

# 3. 创建 Release（已有则跳过）
gh release create v0.1.1.0 --repo anmili2022/Phantom --title "v0.1.1.0" --notes "更新内容" ./Phantom.zip

# 4. 更新已存在的 Release
gh release upload v0.1.1.0 --repo anmili2022/Phantom --clobber ./Phantom.zip
```

## 版本号规则

- 格式：`主版本.次版本.修订.构建`
- 示例：`0.1.0.0`、`0.1.1.0`、`0.2.0.0`
- tag 名对应版本号，不加 `v` 前缀（`0.1.1.0` 而非 `v0.1.1.0`）

## 发布后检查

1. 确认 Release 页面有 `Phantom.zip` 附件
2. 确认 zip 内只有 3 个文件：
   - `Phantom.dll`
   - `Phantom.json`
   - `Phantom.deps.json`
3. 更新 `repo.json` 中的版本号和 `LastUpdated`
4. 测试从 Dalamud 插件列表能否正常安装
