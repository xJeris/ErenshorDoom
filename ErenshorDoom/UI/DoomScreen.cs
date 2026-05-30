using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ErenshorDoom.UI
{
    /// <summary>
    /// Creates a draggable windowed Doom viewport so the player retains access to Erenshor.
    /// The window has a title bar for dragging and a close button.
    /// </summary>
    public sealed class DoomScreen : IDisposable
    {
        private static readonly Color TitleBarColor = new Color(0.15f, 0.0f, 0.0f, 0.95f);
        private static readonly Color BorderColor = new Color(0.3f, 0.0f, 0.0f, 0.95f);
        private static readonly Color CloseButtonColor = new Color(0.6f, 0.1f, 0.1f, 1f);

        private const float TitleBarHeight = 28f;
        private const float BorderWidth = 3f;
        // Default window size: 640x480 (4:3) + title bar + borders
        private const float DefaultGameWidth = 640f;
        private const float DefaultGameHeight = 480f;

        private GameObject canvasObject;
        private Canvas canvas;
        private RawImage rawImage;
        private GameObject windowObject;
        private RectTransform windowRect;

        private Action onCloseCallback;

        public DoomScreen(Action onClose)
        {
            onCloseCallback = onClose;

            // Create Canvas
            canvasObject = new GameObject("ErenshorDoom_Canvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasObject);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998; // High but not absolute top

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObject.AddComponent<GraphicRaycaster>();

            // Create window container
            windowObject = new GameObject("DoomWindow");
            windowObject.transform.SetParent(canvasObject.transform, false);
            windowRect = windowObject.AddComponent<RectTransform>();

            float totalWidth = DefaultGameWidth + BorderWidth * 2;
            float totalHeight = DefaultGameHeight + TitleBarHeight + BorderWidth * 2;
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(totalWidth, totalHeight);

            // Window border/background
            var windowBg = windowObject.AddComponent<Image>();
            windowBg.color = BorderColor;

            // Title bar
            var titleBarObj = new GameObject("TitleBar");
            titleBarObj.transform.SetParent(windowObject.transform, false);
            var titleBarImage = titleBarObj.AddComponent<Image>();
            titleBarImage.color = TitleBarColor;
            var titleBarRect = titleBarObj.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1);
            titleBarRect.sizeDelta = new Vector2(0, TitleBarHeight);
            titleBarRect.anchoredPosition = Vector2.zero;

            // Make title bar draggable
            var dragHandler = titleBarObj.AddComponent<WindowDragHandler>();
            dragHandler.Target = windowRect;

            // Title text
            var titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(titleBarObj.transform, false);
            var titleText = titleTextObj.AddComponent<Text>();
            titleText.text = "DOOM";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            var titleTextRect = titleTextObj.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(8, 0);
            titleTextRect.offsetMax = new Vector2(-30, 0);

            // Close button
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(titleBarObj.transform, false);
            var closeImage = closeObj.AddComponent<Image>();
            closeImage.color = CloseButtonColor;
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(24, 20);
            closeRect.anchoredPosition = new Vector2(-4, 0);

            var closeButton = closeObj.AddComponent<Button>();
            closeButton.onClick.AddListener(() => onCloseCallback?.Invoke());

            var closeTextObj = new GameObject("CloseText");
            closeTextObj.transform.SetParent(closeObj.transform, false);
            var closeText = closeTextObj.AddComponent<Text>();
            closeText.text = "X";
            closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            closeText.fontSize = 14;
            closeText.fontStyle = FontStyle.Bold;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            var closeTextRect = closeTextObj.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;

            // Game viewport area (inside borders, below title bar)
            var viewportObj = new GameObject("DoomFramebuffer");
            viewportObj.transform.SetParent(windowObject.transform, false);
            rawImage = viewportObj.AddComponent<RawImage>();
            rawImage.color = Color.white;
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(BorderWidth, BorderWidth); // left, bottom inset
            viewportRect.offsetMax = new Vector2(-BorderWidth, -(TitleBarHeight + BorderWidth)); // right, top inset

            canvasObject.SetActive(false);
        }

        public void SetTexture(Texture2D texture)
        {
            rawImage.texture = texture;
        }

        public void Show()
        {
            canvasObject.SetActive(true);
        }

        public void Hide()
        {
            canvasObject.SetActive(false);
        }

        public void Dispose()
        {
            if (canvasObject != null)
            {
                UnityEngine.Object.Destroy(canvasObject);
                canvasObject = null;
            }
        }

        public bool IsActive => canvasObject != null && canvasObject.activeSelf;

        /// <summary>
        /// Checks if the mouse is currently over the Doom window.
        /// Used to determine whether input should go to Doom or Erenshor.
        /// </summary>
        public bool IsMouseOverWindow()
        {
            if (!IsActive || windowRect == null) return false;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                windowRect, UnityEngine.Input.mousePosition, null, out localPoint);
            return windowRect.rect.Contains(localPoint);
        }
    }

    /// <summary>
    /// Allows dragging a UI window by its title bar.
    /// </summary>
    public class WindowDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public RectTransform Target;
        private Vector2 dragOffset;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Target.parent as RectTransform, eventData.position, eventData.pressEventCamera, out dragOffset);
            dragOffset = Target.anchoredPosition - dragOffset;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Target.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
            Target.anchoredPosition = localPoint + dragOffset;
        }
    }
}
