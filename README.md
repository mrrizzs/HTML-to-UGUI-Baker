# HTML to UGUI Baker

这是一套将 AI 生成的 HTML 原型直接转换为 Unity UGUI 界面树的自动化生产管线。通过自定义的 HTML 数据属性规范（UI-DSL），结合 Web 坐标提取工具和 Unity 编辑器扩展，实现从“自然语言对话”到“生产级 UGUI 预制体”的无缝流转。

演示视频：https://www.bilibili.com/video/BV17BcXzwEer
![PixPin_2026-03-10_15-44-19](https://github.com/user-attachments/assets/ad14f70e-fe40-437b-8a0a-54b4764630c0)

## 🚀 v2.0 全新特性 (最新更新)

*   **多分辨率预设与动态适配**：引入 `HtmlToUGUIConfig` (ScriptableObject) 全局配置，支持在编辑器内自由增删分辨率预设（如 PC 横屏 1920x1080、Mobile 竖屏 1080x1920）。烘焙时会自动配置目标 Canvas 的 `CanvasScaler`。
*   **动态 DSL 规范导出**：无需手动修改 AI 提示词，在编辑器中选择目标分辨率后，点击“复制 DSL 规范”，系统会自动替换模板中的 `{WIDTH}` 和 `{HEIGHT}` 占位符并写入剪贴板。
*   **极速剪贴板直通流**：彻底告别繁琐的文件保存与拖拽！Web 工具支持“一键烘焙并复制 JSON”，Unity 端支持“直接粘贴 JSON 字符”模式，实现跨软件的秒级流转。
*   **Web 烘焙器全面升级**：新增自适应屏幕的等比缩放预览功能，彻底修复了 CSS 动画导致的坐标偏移问题，确保 1:1 完美还原绝对坐标与尺寸。
*   **外部工具链桥接**：在 Unity 烘焙窗口内可直接配置本地 HTML 工具的路径，一键在浏览器中唤起工作流。

---

## 核心特性

*   **全控件拓扑支持**：不仅支持基础排版，还在 Unity 端硬编码了 `Toggle`、`Slider`、`Dropdown`、`ScrollRect` 等复杂控件的标准 UGUI 嵌套层级，保证生成的节点结构与原生右键创建的绝对一致。
*   **所见即所得**：精准提取 HTML 的绝对坐标、尺寸、背景色、字体颜色、字体大小以及文本对齐方式（Left/Right/Center）。
*   **代码级规范**：强制采用小驼峰命名法（camelCase），生成的 UI 节点名称可直接用于 C# View 层的变量绑定（如 `loginBtn`）。
*   **零依赖**：Unity 端纯 C# 原生 Editor 扩展，基于 TextMeshPro 构建，无需引入任何第三方 UI 插件。

---

## 标准工作流与使用指南 (v2.0)

整个管线分为三个标准步骤：**导出规范 -> AI 生成 -> 剪贴板烘焙**。

### 准备工作 (仅首次需要)
1. 在 Unity 中右键 `Project` 窗口 -> `Create -> UI Architecture -> HtmlToUGUI Config` 创建配置文件。
2. 将提供的 `UI-DSL.md` 模板文件拖入该配置的对应槽位中。
3. 在顶部菜单栏打开 `Tools -> UI Architecture -> HTML to UGUI Baker`。

### Step 1: 导出规范与 AI 生成
1. 在 Unity 的烘焙窗口中，选择你的**目标分辨率**（如 Mobile 竖屏）。
2. 点击 **“复制对应分辨率的 DSL 规范文档”**。
3. 将剪贴板中的内容发送给任意大语言模型（如 ChatGPT, Claude, Gemini），并附加你的自然语言需求（例如：“*帮我写一个系统设置界面，包含音量滑动条、全屏开关和画质下拉菜单*”）。
4. 复制 AI 生成的 HTML 代码。

### Step 2: Web 端一键提取坐标
1. 在 Unity 烘焙窗口中点击 **“在浏览器中打开”**（需提前配置好 HTML 工具的本地路径）。
2. 将 AI 生成的 HTML 代码粘贴到左侧输入框中，右侧将实时渲染自适应预览。
3. 点击右上角的 **“🚀 一键烘焙并复制 JSON”**，工具会在后台完成坐标换算并将 JSON 数据直接写入你的系统剪贴板。

### Step 3: Unity 端极速生成
1. 回到 Unity 的 HTML to UGUI Baker 窗口。
2. 拖入场景中的目标 **Canvas**。
3. 将输入模式切换为 **“直接粘贴 JSON 字符”**，并将刚才复制的 JSON 粘贴到文本框中。
4. 点击 **“执行烘焙生成”**。
5. 完成！你的 Canvas 下会自动生成完整的 UI 节点树，所有控件均已配置完毕且可直接运行。

---

## 控件映射支持列表

| HTML 标签声明 | Unity UGUI 对应组件 | 支持的高级属性 |
| :--- | :--- | :--- |
| `data-u-type="div"` | Image (Raycast 自动剔除) | 尺寸、坐标、背景色 |
| `data-u-type="image"` | Image | 尺寸、坐标、背景色 |
| `data-u-type="text"` | TextMeshProUGUI | 字体大小、颜色、对齐方式 |
| `data-u-type="button"` | Button + Image + TMP | 交互组件、内部文本样式 |
| `data-u-type="input"` | TMP_InputField | 占位符文本、输入文本样式 |
| `data-u-type="scroll"`| ScrollRect + Mask | 滚动方向 (`data-u-dir="v/h"`) |
| `data-u-type="toggle"`| Toggle + Image + TMP | 默认开关状态 (`data-u-checked`) |
| `data-u-type="slider"`| Slider + Image | 默认进度值 (`data-u-value`) |
| `data-u-type="dropdown"`| TMP_Dropdown + ScrollRect | 内部 `<option>` 选项列表解析 |
