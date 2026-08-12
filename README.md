# Ice Reversi · Unity 6

这是使用本机 Unity 6 重新搭建的黑白棋工程。主场景、材质、Prefab、HUD、规则层和 AI 均已从旧 Unity 2019 结构迁出；运行时不依赖旧兄弟工程。

## 环境

- Unity：`6000.5.7f1`
- 本机 Editor：`/Applications/Unity/Hub/Editor/6000.5.7f1/Unity.app`
- 启动场景：`Assets/Scenes/Game.unity`（Build Settings 中唯一启用的场景）
- 关键包：Input System 1.20.0、URP 17.5.0、UGUI 2.5.0、Test Framework 1.7.0

使用其他 Unity 版本打开可能会改写包、场景或 ProjectSettings，不属于已验证配置。

## 打开与游玩

通过 Unity Hub 将仓库根目录作为工程打开，进入 `Assets/Scenes/Game.unity` 后按 Play。

- 鼠标单击或单指触摸绿色合法落点
- `Restart`：重新开始并取消正在运行的 AI 请求
- `Undo`：人机模式撤销一轮人机交换；观战模式撤销一步
- `Play White / Play Black`：切换玩家棋色并保存本机偏好
- `Watch AI / Stop Watching`：开始或停止 AI 对弈观战
- `AI: Easy / Normal / Hard / Expert`：循环切换并保存电脑难度；默认 Normal
- `中文 / EN`：在英文和简体中文之间切换并保存语言选择
- `Exit`：取消 AI 并退出；在 Editor 中停止 Play

HUD 显示双方分数、当前行棋方、AI 思考、Pass、当前难度和胜负结果。所有 HUD 与操作文案均提供英文和简体中文版本；中文模式会选用运行平台可用的中文系统字体。布局会适配安全区以及竖屏、横屏、16:9 和 4:3。

每次合法落子会播放约 180 ms 的缩放入场，翻转棋子约 270 ms，并按离落点的距离以约 35 ms 错峰；整步表现限制在约 500 ms 内。玩家动画期间电脑可以在后台思考，但电脑不会在上一段动画结束前改变棋盘，电脑自己的落子动画结束前也不会重新开放输入。Restart、Undo、换边、切观战和切难度会取消旧动画与搜索并恢复到当前权威局面。

难度采用有界迭代加深搜索：Easy 适合快速休闲对局，Normal 是默认平衡档，Hard 提高搜索预算并尝试 10 空格残局求解，Expert 进一步提高预算并尝试 12 空格残局求解。具体预算和本机实测见 [Docs/AiPerformance.md](Docs/AiPerformance.md)。

## 重建和验证

菜单命令：

- `Ice Reversi > Rebuild Game Scene`
- `Ice Reversi > Validate Project`
- `Ice Reversi > Capture Reference Layouts`

批处理重建并验证：

```bash
"/Applications/Unity/Hub/Editor/6000.5.7f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath "/path/to/ice_reversi" \
  -executeMethod IceReversi.Editor.ReversiProjectValidator.BuildAndValidateFromCommandLine \
  -logFile /tmp/ice_reversi_validate.log
```

EditMode 测试：

```bash
"/Applications/Unity/Hub/Editor/6000.5.7f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics \
  -projectPath "/path/to/ice_reversi" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/ice_reversi_editmode.xml \
  -logFile /tmp/ice_reversi_editmode.log
```

将 `EditMode` 改为 `PlayMode` 可运行场景交互回归测试。Builder 可重复运行：它会更新 Unity 6 材质、Piece/MoveHint Prefab 和 `Game.unity`，不会追加重复场景对象。

## 素材与性能记录

- 素材逐项来源：[Assets/Reversi/Art/PROVENANCE.md](Assets/Reversi/Art/PROVENANCE.md)
- AI 行为迁移：[Docs/LegacyAiBehavior.md](Docs/LegacyAiBehavior.md)
- AI 有界搜索画像：[Docs/AiPerformance.md](Docs/AiPerformance.md)
- 迁移基线：[Docs/MigrationBaseline.md](Docs/MigrationBaseline.md)

旧素材仅在迁移时从本机兄弟工程复制，场景和 Prefab 的依赖已全部收口到本仓库。

## 平台模块

当前 Unity 安装检测到 macOS Standalone 与 WebGL Build Support，未检测到 iOS 或 Android Build Support。本次验收范围是 macOS Unity Editor；未来要构建 iOS/Android，需先在 Unity Hub 为 `6000.5.7f1` 安装对应 Build Support（iOS 还需要 Xcode，Android 需要 SDK/NDK/JDK）。
