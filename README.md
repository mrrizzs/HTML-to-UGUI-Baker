# HTML to UGUI Baker

这是一套将 AI 生成的 HTML 原型直接转换为 Unity UGUI 界面树的自动化生产管线。通过自定义的 HTML 数据属性规范（UI-DSL），结合 Web 坐标提取工具和 Unity 编辑器扩展，实现从“自然语言对话”到“生产级 UGUI 预制体”的无缝流转。

## 核心特性

*   **全控件拓扑支持**：不仅支持基础排版，还在 Unity 端硬编码了 `Toggle`、`Slider`、`Dropdown`、`ScrollRect` 等复杂控件的标准 UGUI 嵌套层级，保证生成的节点结构与原生右键创建的绝对一致。
*   **所见即所得**：精准提取 HTML 的绝对坐标、尺寸、背景色、字体颜色、字体大小以及文本对齐方式（Left/Right/Center）。
*   **代码级规范**：强制采用小驼峰命名法（camelCase），生成的 UI 节点名称可直接用于 C# View 层的变量绑定（如 `loginBtn`）。
*   **零依赖**：Unity 端纯 C# 原生 Editor 扩展，基于 TextMeshPro 构建，无需引入任何第三方 UI 插件。

---

## 工作流与使用指南

整个管线分为三个标准步骤：**AI 生成 -> Web 烘焙 -> Unity 构建**。

### Step 1: 让 AI 生成 UI 原型 (HTML)
1. 将本项目提供的 `AI生成UI原型HTML规范.md` 发送给任意大语言模型（如 ChatGPT, Claude, Gemini）。
2. 用自然语言描述你需要的界面，例如：“*帮我写一个系统设置界面，包含音量滑动条、全屏开关和画质下拉菜单*”。
3. 复制 AI 按照规范生成的 HTML 代码。

### Step 2: 提取坐标与状态 (JSON)
1. 在浏览器中双击打开本项目提供的 `HTML转JSON坐标烘焙器.html` 工具。
2. 将 AI 生成的 HTML 代码粘贴到左侧的输入框中。
3. 点击 **“提取坐标并生成 JSON”**，工具会在沙盒中渲染预览，并计算所有节点的相对坐标与高级属性。
4. 点击 **“复制 JSON”**，将生成的 JSON 数据保存为 `.json` 文件（例如 `SettingsWindow.json`）。

### Step 3: 在 Unity 中一键生成 UGUI
1. 将 `HtmlToUGUIBaker.cs` 放入 Unity 工程的 `Assets/Editor/` 目录下。
2. 将刚才生成的 `.json` 数据文件拖入 Unity 工程中。
3. 在 Unity 顶部菜单栏点击：`Tools -> UI Architecture -> HTML to UGUI Baker (Full Controls)`。
4. 在弹出的工具窗口中：
   *   **JSON 数据源**：拖入你的 `.json` 文件。
   *   **目标 Canvas**：拖入场景中的 Canvas 节点。
5. 点击 **“执行烘焙生成”**。
6. 完成！你的 Canvas 下会自动生成完整的 UI 节点树，所有控件均已配置完毕且可直接运行。

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
