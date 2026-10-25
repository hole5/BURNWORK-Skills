# Agent Note: 首页 AI 框支持拖入参考文件

Status: implemented

## Problem

首页创意输入框只能点「+ 参考内容」选文件；从资源管理器拖入 txt/md/图片没有反应。工作台右侧聊天已接 `AllowDrop` + `OnChatDragOver` / `OnChatDrop`，首页 `ComposeBox` 外框没有同样接线，拖放只会进 `_chatAttach`。

## Decision

首页 compose 外框与 `ComposeBox` 开 `AllowDrop`，`PreviewDragOver` / `PreviewDrop` 走共用校验；落下时调用 `AddComposeAttachment`（芯片进 `ComposeAttachHost`），与「+ 参考内容」菜单一致。工作台仍走 `AddChatAttachment`。

## Alternatives considered

- **只提示用户点按钮** — 最强理由：实现零改动。不用：截图与「参考内容」预期就是拖放可用。
- **首页与聊天共用一个 Drop 且一律进 chat** — 最强理由：少一份 handler。不用：首页尚未建项目，附件语义是 compose，创建时再拷入。

## Consequences

- **收益**：首页拖入原著/参考图与按钮行为一致。
- **代价**：多一组 Preview 事件绑定；无效文件仍提示扩展名限制。
