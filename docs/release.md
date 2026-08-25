# 发布流程

## 一键发布（推荐）

从干净且与 `origin/main` 同步的 `main` 分支执行：

```powershell
.\scripts\release.ps1 0.1.30.0
```

脚本会自动完成：

1. 检查当前分支、已跟踪文件改动、本地/远端分叉和重复 tag。
2. 同步更新 `Phantom.csproj`、`Phantom.json`、`repo.json` 的版本、下载链接和 UTC 时间。
3. 执行 Release 构建。
4. 创建 `Release x.x.x.x` 提交并推送 `main`。
5. 创建带说明的版本 tag 并推送，触发 GitHub Actions。
6. 等待工作流完成，并确认 Release 中存在 `Phantom.zip`。

未跟踪的本地日志和预览文件不会阻止发布，也不会被脚本提交。已跟踪文件有未提交改动时脚本会停止，避免把未确认内容带入版本。

仅检查版本文件改动而不构建、提交或推送：

```powershell
.\scripts\release.ps1 0.1.30.0 -DryRun
```

已经自行完成构建时可跳过本地构建：

```powershell
.\scripts\release.ps1 0.1.30.0 -SkipBuild
```

不等待约 40-60 秒的 CI，推送 tag 后立即返回：

```powershell
.\scripts\release.ps1 0.1.30.0 -NoWait
```

`-NoWait` 只缩短本地等待时间，Release 仍由 GitHub Actions 在后台创建；需要稍后自行查看工作流结果。

## CI 发布

推送格式为 `主.次.修订.构建` 的 tag 后，`.github/workflows/release.yml` 会：

- 下载 Dalamud CN。
- 还原带 NuGet 缓存的依赖。
- 使用 tag 版本构建 Release。
- 打包 `Phantom.dll`、`Phantom.json`、`Phantom.deps.json`。
- 创建 GitHub Release 并上传 `Phantom.zip`。

CI 通常约 40-60 秒。主要耗时是依赖还原、构建和上传 Release；一键脚本消除了发布前后的人工提交与重复检查。

## 版本规则

- 格式：`主版本.次版本.修订.构建`。
- tag 不加 `v` 前缀，例如 `0.1.30.0`。
- 每个 tag 必须对应已经包含同版本 `Phantom.csproj`、`Phantom.json` 和 `repo.json` 的提交。

## 手动恢复

自动发布在推送 `main` 后、推送 tag 前失败时，修复问题后执行：

```powershell
git tag -a 0.1.30.0 -m "Release 0.1.30.0"
git push origin 0.1.30.0
```

已有 tag 但 CI 需要重新运行时，从 GitHub Actions 手动运行 `Create Release` 并填写现有 tag。

## 注意事项

- 脚本不是事务：版本提交推送到 `main` 后，如果创建 tag 或查询 CI 失败，远端提交不会自动回滚。此时不要直接重跑整个脚本，否则会遇到版本文件已更新、提交无变化或远端分叉检查；应先修复脚本，再按“手动恢复”创建并推送 tag。
- `scripts/release.ps1` 的 Git/GitHub CLI 包装函数使用 PowerShell 自动变量 `$args` 原样透传参数。不要改成普通的具名参数接收方式，否则 `git tag -a ... -m ...` 中的 `-a`、`-m` 可能被 PowerShell 绑定为包装函数自身参数，导致 tag 创建中断。
- `-NoWait` 只是不在本地等待 CI，并不会缩短 GitHub Actions 的实际构建时间。使用后必须稍后检查工作流和 Release 附件。
- NuGet 缓存首次启用时需要创建缓存，可能与未缓存时一样慢甚至略慢；通常从后续发布开始才可能命中缓存并缩短还原时间。
- 一键脚本只允许已跟踪文件干净时正式发布。未跟踪的日志和预览文件会被忽略，但不会被自动删除或提交。
- tag 必须在功能提交和发布脚本修复之后创建。tag 一旦推送并生成 Release，不要移动或重建同名 tag；后续文档修正直接提交到 `main`。

## 发布后检查

1. Release 页面存在 `Phantom.zip`。
2. zip 内只有 `Phantom.dll`、`Phantom.json`、`Phantom.deps.json`。
3. `repo.json` 已在 tag 对应提交中指向新版本下载地址。
4. 从 Dalamud 插件列表验证安装和更新。
