# Agent Note: WPF 全屏文字对比度

Status: implemented

## Problem

暗金工作台里大量文字近色或继承系统黑字，首页导航、设置表单、分集标题、聊天、下拉与勾选都难以辨认。只抬 `Muted` 色值不够，因为嵌套 `TextBlock` 不走按钮 `Foreground`。

## Decision

应用资源里给 `TextBlock`/`Label` 隐式浅色前景（`Ink` `#F7F3EC`）。次要说明用 `SoftInk` `#F3EFE6`，`Muted` 提到 `#E8E4D8`。自绘 `CheckBox`、暗色 `ComboBox`/`ComboBoxItem`/`ListBoxItem`/`ContextMenu`/`MenuItem`/`ToolTip`。代码生成的空状态与占位符显式绑 `Ink`/`SoftInk`。

## Alternatives considered

- **只继续提高 Muted** — 最强理由：改动面小。不用：系统默认黑字与未模板化的 ComboBox/CheckBox 仍不可读。
- **全局改成白底** — 最强理由：对比天然够。不用：与暗金工作台产品外观冲突。

## Consequences

- **收益**：各页标签、列表、输入占位、阶段栏与聊天模型下拉都走浅色字，不再依赖 Windows 控件默认黑字。
- **代价**：主题把系统 ComboBox/CheckBox 模板换掉，后续若加系统控件要同样自绘前景。
