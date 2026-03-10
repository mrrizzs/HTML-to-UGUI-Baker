using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

namespace Editor.UIBaker
{
    [System.Serializable]
    public class UIDataNode
    {
        public string name;
        public string type;
        public string dir;
        public float value;
        public bool isChecked;
        public List<string> options;
        public float x;
        public float y;
        public float width;
        public float height;
        public string color;
        public string fontColor;
        public int fontSize;
        public string textAlign;
        public string text;
        public List<UIDataNode> children;
    }

    public class HtmlToUGUIBaker : EditorWindow
    {
        private TextAsset jsonAsset;
        private Canvas targetCanvas;

        private readonly Vector2 REFERENCE_RESOLUTION = new Vector2(1920, 1080);

        [MenuItem("Tools/UI Architecture/HTML to UGUI Baker (Full Controls)")]
        public static void ShowWindow()
        {
            GetWindow<HtmlToUGUIBaker>("UI 原型烘焙器");
        }

        private void OnGUI()
        {
            GUILayout.Label("基于坐标烘焙的 UI 原型生成工具 (支持全控件与对齐)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            jsonAsset = (TextAsset)EditorGUILayout.ObjectField("JSON 数据源", jsonAsset, typeof(TextAsset), false);
            targetCanvas = (Canvas)EditorGUILayout.ObjectField("目标 Canvas", targetCanvas, typeof(Canvas), true);

            GUILayout.Space(20);

            if (GUILayout.Button("执行烘焙生成", GUILayout.Height(40)))
            {
                ExecuteBake();
            }
        }

        private void ExecuteBake()
        {
            if (jsonAsset == null)
            {
                Debug.LogError("[HtmlToUGUIBaker] 烘焙中断: 未指定 JSON 数据源。");
                return;
            }

            if (targetCanvas == null)
            {
                Debug.LogError("[HtmlToUGUIBaker] 烘焙中断: 未指定目标 Canvas。");
                return;
            }

            ConfigureCanvasScaler(targetCanvas);

            string jsonContent = string.Empty;
            try
            {
                jsonContent = File.ReadAllText(AssetDatabase.GetAssetPath(jsonAsset));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HtmlToUGUIBaker] 文件读取失败: {e.Message}");
                return;
            }

            if (string.IsNullOrEmpty(jsonContent)) return;

            UIDataNode rootNode = JsonUtility.FromJson<UIDataNode>(jsonContent);
            if (rootNode == null) return;

            GameObject rootGo = CreateUINode(rootNode, targetCanvas.transform, 0f, 0f);

            Undo.RegisterCreatedObjectUndo(rootGo, "Bake UI Prototype");
            Selection.activeGameObject = rootGo;

            Debug.Log($"[HtmlToUGUIBaker] 烘焙完成: 成功生成 UI 树 {rootGo.name}，文本对齐与智能换行已应用。");
        }

        private void ConfigureCanvasScaler(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = REFERENCE_RESOLUTION;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private GameObject CreateUINode(UIDataNode nodeData, Transform parent, float parentAbsX, float parentAbsY)
        {
            GameObject go = new GameObject(nodeData.name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);

            float localX = nodeData.x - parentAbsX;
            float localY = nodeData.y - parentAbsY;

            rect.anchoredPosition = new Vector2(localX, -localY);
            rect.sizeDelta = new Vector2(nodeData.width, nodeData.height);

            Transform childrenContainer = ApplyComponentByType(go, nodeData);

            if (nodeData.children != null && nodeData.children.Count > 0)
            {
                foreach (var childNode in nodeData.children)
                {
                    CreateUINode(childNode, childrenContainer, nodeData.x, nodeData.y);
                }
            }

            return go;
        }

        private Transform ApplyComponentByType(GameObject go, UIDataNode nodeData)
        {
            Color bgColor = ParseHexColor(nodeData.color, Color.white);
            Color fontColor = ParseHexColor(nodeData.fontColor, Color.black);
            int fontSize = nodeData.fontSize > 0 ? nodeData.fontSize : 24;

            TextAlignmentOptions alignment = ParseTextAlign(nodeData.textAlign);
            // 智能换行启发式：高度大于字体1.5倍视为多行文本，否则为单行文本（防止数字被挤换行）
            bool isMultiLine = nodeData.height > (fontSize * 1.5f);

            switch (nodeData.type.ToLower())
            {
                case "div":
                case "image":
                    Image img = go.AddComponent<Image>();
                    img.color = bgColor;
                    if (img.color.a <= 0.01f) img.raycastTarget = false;
                    return go.transform;

                case "text":
                    TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
                    txt.text = nodeData.text;
                    txt.color = fontColor;
                    txt.fontSize = fontSize;
                    txt.alignment = alignment;
                    txt.enableWordWrapping = isMultiLine;
                    txt.overflowMode = isMultiLine ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
                    txt.raycastTarget = false;
                    return go.transform;

                case "button":
                    Image btnImg = go.AddComponent<Image>();
                    btnImg.color = bgColor;
                    Button btn = go.AddComponent<Button>();
                    btn.targetGraphic = btnImg;

                    GameObject btnTxtGo = CreateChildRect(go, "Text (TMP)", Vector2.zero, Vector2.one);
                    TextMeshProUGUI btnTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
                    btnTxt.text = nodeData.text;
                    btnTxt.color = fontColor;
                    btnTxt.fontSize = fontSize;
                    btnTxt.alignment = alignment;
                    btnTxt.enableWordWrapping = false; // 按钮通常为单行
                    btnTxt.overflowMode = TextOverflowModes.Overflow;
                    btnTxt.raycastTarget = false;
                    return go.transform;

                case "input":
                    Image inputBg = go.AddComponent<Image>();
                    inputBg.color = bgColor;
                    TMP_InputField inputField = go.AddComponent<TMP_InputField>();
                    inputField.targetGraphic = inputBg;

                    GameObject textAreaGo = CreateChildRect(go, "Text Area", Vector2.zero, Vector2.one, new Vector2(10, 5), new Vector2(-10, -5));
                    textAreaGo.AddComponent<RectMask2D>();

                    GameObject phGo = CreateChildRect(textAreaGo, "Placeholder", Vector2.zero, Vector2.one);
                    TextMeshProUGUI phTxt = phGo.AddComponent<TextMeshProUGUI>();
                    phTxt.text = nodeData.text;
                    Color phColor = fontColor;
                    phColor.a = 0.5f;
                    phTxt.color = phColor;
                    phTxt.fontSize = fontSize;
                    phTxt.alignment = alignment;
                    phTxt.enableWordWrapping = false;
                    phTxt.raycastTarget = false;

                    GameObject textGo = CreateChildRect(textAreaGo, "Text", Vector2.zero, Vector2.one);
                    TextMeshProUGUI inTxt = textGo.AddComponent<TextMeshProUGUI>();
                    inTxt.color = fontColor;
                    inTxt.fontSize = fontSize;
                    inTxt.alignment = alignment;
                    inTxt.enableWordWrapping = false;
                    inTxt.raycastTarget = false;

                    inputField.textViewport = textAreaGo.GetComponent<RectTransform>();
                    inputField.textComponent = inTxt;
                    inputField.placeholder = phTxt;
                    return go.transform;

                case "scroll":
                    Image scrollBg = go.AddComponent<Image>();
                    scrollBg.color = bgColor;
                    if (scrollBg.color.a <= 0.01f) scrollBg.raycastTarget = false;

                    ScrollRect scrollRect = go.AddComponent<ScrollRect>();
                    bool isVertical = string.IsNullOrEmpty(nodeData.dir) || nodeData.dir.ToLower() == "v";
                    scrollRect.horizontal = !isVertical;
                    scrollRect.vertical = isVertical;

                    GameObject viewportGo = CreateChildRect(go, "Viewport", Vector2.zero, Vector2.one);
                    viewportGo.AddComponent<RectMask2D>();

                    GameObject contentGo = CreateChildRect(viewportGo, "Content", new Vector2(0, 1), new Vector2(0, 1));
                    RectTransform contentRect = contentGo.GetComponent<RectTransform>();
                    contentRect.pivot = new Vector2(0, 1);
                    contentRect.sizeDelta = new Vector2(nodeData.width, nodeData.height);

                    scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
                    scrollRect.content = contentRect;
                    return contentGo.transform;

                case "toggle":
                    Toggle toggle = go.AddComponent<Toggle>();
                    toggle.isOn = nodeData.isChecked;

                    float boxSize = Mathf.Min(nodeData.height, 30f);
                    GameObject tBgGo = CreateChildRect(go, "Background", new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                    RectTransform tBgRect = tBgGo.GetComponent<RectTransform>();
                    tBgRect.sizeDelta = new Vector2(boxSize, boxSize);
                    tBgRect.anchoredPosition = new Vector2(boxSize / 2, 0);
                    Image tBgImg = tBgGo.AddComponent<Image>();
                    tBgImg.color = Color.white;

                    GameObject checkGo = CreateChildRect(tBgGo, "Checkmark", Vector2.zero, Vector2.one);
                    Image checkImg = checkGo.AddComponent<Image>();
                    checkImg.color = Color.black;

                    RectTransform checkRect = checkGo.GetComponent<RectTransform>();
                    checkRect.offsetMin = new Vector2(4, 4);
                    checkRect.offsetMax = new Vector2(-4, -4);

                    GameObject tLblGo = CreateChildRect(go, "Label", Vector2.zero, Vector2.one);
                    RectTransform tLblRect = tLblGo.GetComponent<RectTransform>();
                    tLblRect.offsetMin = new Vector2(boxSize + 10, 0);
                    TextMeshProUGUI tLblTxt = tLblGo.AddComponent<TextMeshProUGUI>();
                    tLblTxt.text = nodeData.text;
                    tLblTxt.color = fontColor;
                    tLblTxt.fontSize = fontSize;
                    tLblTxt.alignment = TextAlignmentOptions.MidlineLeft; // 强制垂直居中靠左
                    tLblTxt.enableWordWrapping = false;

                    toggle.targetGraphic = tBgImg;
                    toggle.graphic = checkImg;
                    return go.transform;

                case "slider":
                    Slider slider = go.AddComponent<Slider>();
                    slider.value = Mathf.Clamp01(nodeData.value);

                    GameObject sBgGo = CreateChildRect(go, "Background", new Vector2(0, 0.25f), new Vector2(1, 0.75f));
                    Image sBgImg = sBgGo.AddComponent<Image>();
                    sBgImg.color = bgColor;

                    GameObject fillAreaGo = CreateChildRect(go, "Fill Area", Vector2.zero, Vector2.one, new Vector2(5, 0), new Vector2(-15, 0));
                    GameObject fillGo = CreateChildRect(fillAreaGo, "Fill", Vector2.zero, Vector2.one);
                    Image fillImg = fillGo.AddComponent<Image>();
                    fillImg.color = fontColor;

                    GameObject handleAreaGo = CreateChildRect(go, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
                    GameObject handleGo = CreateChildRect(handleAreaGo, "Handle", Vector2.zero, Vector2.one);
                    RectTransform handleRect = handleGo.GetComponent<RectTransform>();
                    handleRect.sizeDelta = new Vector2(20, 0);
                    Image handleImg = handleGo.AddComponent<Image>();
                    handleImg.color = Color.white;

                    slider.targetGraphic = handleImg;
                    slider.fillRect = fillGo.GetComponent<RectTransform>();
                    slider.handleRect = handleRect;
                    return go.transform;

                case "dropdown":
                    Image dBgImg = go.AddComponent<Image>();
                    dBgImg.color = bgColor;
                    TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();

                    GameObject dLblGo = CreateChildRect(go, "Label", Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-30, 0));
                    TextMeshProUGUI dLblTxt = dLblGo.AddComponent<TextMeshProUGUI>();
                    dLblTxt.color = fontColor;
                    dLblTxt.fontSize = fontSize;
                    dLblTxt.alignment = TextAlignmentOptions.MidlineLeft; // 强制垂直居中靠左
                    dLblTxt.enableWordWrapping = false;

                    GameObject arrowGo = CreateChildRect(go, "Arrow", new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                    RectTransform arrowRect = arrowGo.GetComponent<RectTransform>();
                    arrowRect.sizeDelta = new Vector2(20, 20);
                    arrowRect.anchoredPosition = new Vector2(-15, 0);
                    Image arrowImg = arrowGo.AddComponent<Image>();
                    arrowImg.color = fontColor;

                    GameObject templateGo = CreateChildRect(go, "Template", new Vector2(0, 0), new Vector2(1, 0));
                    RectTransform templateRect = templateGo.GetComponent<RectTransform>();
                    templateRect.pivot = new Vector2(0.5f, 1);
                    templateRect.sizeDelta = new Vector2(0, 150);
                    templateRect.anchoredPosition = new Vector2(0, -2);
                    Image tempImg = templateGo.AddComponent<Image>();
                    tempImg.color = Color.white;
                    ScrollRect tempScroll = templateGo.AddComponent<ScrollRect>();
                    tempScroll.horizontal = false;
                    tempScroll.vertical = true;
                    templateGo.SetActive(false);

                    GameObject dViewportGo = CreateChildRect(templateGo, "Viewport", Vector2.zero, Vector2.one);
                    dViewportGo.AddComponent<Mask>();
                    dViewportGo.AddComponent<Image>().color = Color.white;

                    GameObject dContentGo = CreateChildRect(dViewportGo, "Content", new Vector2(0, 1), new Vector2(1, 1));
                    RectTransform dContentRect = dContentGo.GetComponent<RectTransform>();
                    dContentRect.pivot = new Vector2(0.5f, 1);
                    dContentRect.sizeDelta = new Vector2(0, 28);

                    GameObject itemGo = CreateChildRect(dContentGo, "Item", new Vector2(0, 0.5f), new Vector2(1, 0.5f));
                    RectTransform itemRect = itemGo.GetComponent<RectTransform>();
                    itemRect.sizeDelta = new Vector2(0, 28);
                    Toggle itemToggle = itemGo.AddComponent<Toggle>();

                    GameObject itemBgGo = CreateChildRect(itemGo, "Item Background", Vector2.zero, Vector2.one);
                    Image itemBgImg = itemBgGo.AddComponent<Image>();
                    itemBgImg.color = Color.white;

                    GameObject itemCheckGo = CreateChildRect(itemGo, "Item Checkmark", new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                    RectTransform itemCheckRect = itemCheckGo.GetComponent<RectTransform>();
                    itemCheckRect.sizeDelta = new Vector2(20, 20);
                    itemCheckRect.anchoredPosition = new Vector2(15, 0);
                    Image itemCheckImg = itemCheckGo.AddComponent<Image>();
                    itemCheckImg.color = Color.black;

                    GameObject itemLblGo = CreateChildRect(itemGo, "Item Label", Vector2.zero, Vector2.one, new Vector2(30, 0), new Vector2(-10, 0));
                    TextMeshProUGUI itemLblTxt = itemLblGo.AddComponent<TextMeshProUGUI>();
                    itemLblTxt.color = Color.black;
                    itemLblTxt.fontSize = fontSize;
                    itemLblTxt.alignment = TextAlignmentOptions.MidlineLeft;
                    itemLblTxt.enableWordWrapping = false;

                    itemToggle.targetGraphic = itemBgImg;
                    itemToggle.graphic = itemCheckImg;

                    tempScroll.viewport = dViewportGo.GetComponent<RectTransform>();
                    tempScroll.content = dContentRect;

                    dropdown.targetGraphic = dBgImg;
                    dropdown.template = templateRect;
                    dropdown.captionText = dLblTxt;
                    dropdown.itemText = itemLblTxt;

                    if (nodeData.options != null && nodeData.options.Count > 0)
                    {
                        dropdown.ClearOptions();
                        List<TMP_Dropdown.OptionData> optList = new List<TMP_Dropdown.OptionData>();
                        foreach (var opt in nodeData.options) optList.Add(new TMP_Dropdown.OptionData(opt));
                        dropdown.AddOptions(optList);
                    }

                    return go.transform;

                default:
                    Debug.LogWarning($"[HtmlToUGUIBaker] 未知节点类型: {nodeData.type}");
                    return go.transform;
            }
        }

        /// <summary>
        /// 核心修正：使用 Midline 系列枚举，确保文本在垂直方向上绝对居中
        /// </summary>
        private TextAlignmentOptions ParseTextAlign(string alignStr)
        {
            if (string.IsNullOrEmpty(alignStr)) return TextAlignmentOptions.Midline;

            switch (alignStr.ToLower())
            {
                case "left":
                case "start":
                    return TextAlignmentOptions.MidlineLeft;
                case "right":
                case "end":
                    return TextAlignmentOptions.MidlineRight;
                case "center":
                default:
                    return TextAlignmentOptions.Midline;
            }
        }

        private GameObject CreateChildRect(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
            return go;
        }

        private Color ParseHexColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex)) return defaultColor;
            if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
            return defaultColor;
        }
    }
}