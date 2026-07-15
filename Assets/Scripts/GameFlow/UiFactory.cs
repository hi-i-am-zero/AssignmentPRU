using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Helper tạo Canvas/Button/Text runtime cho Title, Select, Result.
    /// </summary>
    public static class UiFactory
    {
        static Sprite whiteSprite;
        static Font uiFont;

        public static Canvas CreateCanvas(string name, Transform parent = null)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
                canvasGo.transform.SetParent(parent, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        public static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
                return whiteSprite;

            var tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            whiteSprite.name = "UiWhiteSprite";
            return whiteSprite;
        }

        public static Font GetUiFont()
        {
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return uiFont;
        }

        static void ConfigureImage(Image image, Color color)
        {
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
        }

        public static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            ConfigureImage(image, color);
            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.GetComponent<Text>();
            text.font = GetUiFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            ConfigureImage(image, new Color(0.18f, 0.42f, 0.72f, 1f));

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            CreateText(go.transform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white);
            return button;
        }

        public static Button CreateSelectableButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 52f;

            var image = go.GetComponent<Image>();
            ConfigureImage(image, new Color(0.2f, 0.2f, 0.25f, 0.95f));

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter, Color.white);
            return button;
        }

        public static void SetButtonHighlight(Button button, bool selected)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = selected
                ? new Color(0.85f, 0.55f, 0.15f, 1f)
                : new Color(0.2f, 0.2f, 0.25f, 0.95f);
        }
    }
}
