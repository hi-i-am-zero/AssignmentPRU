using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Title screen: game name + Play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleUI : MonoBehaviour
    {
        void Start()
        {
            BuildUi();
        }

        void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas("TitleCanvas", transform);

            UiFactory.CreatePanel(
                canvas.transform,
                "Background",
                new Color(0.05f, 0.08f, 0.14f, 1f),
                Vector2.zero,
                Vector2.one);

            var title = UiFactory.CreateText(
                canvas.transform,
                "Title",
                "Skyfall Arena",
                72,
                TextAnchor.MiddleCenter,
                Color.white);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.55f);
            titleRect.anchorMax = new Vector2(0.9f, 0.85f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var subtitle = UiFactory.CreateText(
                canvas.transform,
                "Subtitle",
                "Local 1v1 Platform Fighter",
                28,
                TextAnchor.MiddleCenter,
                new Color(0.75f, 0.8f, 0.9f, 1f));
            var subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.2f, 0.45f);
            subtitleRect.anchorMax = new Vector2(0.8f, 0.55f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;

            UiFactory.CreateButton(
                canvas.transform,
                "PlayButton",
                "Play",
                Vector2.zero,
                new Vector2(280f, 72f),
                () => GameSession.Instance.LoadSelect());

            var playRect = canvas.transform.Find("PlayButton") as RectTransform;
            if (playRect != null)
            {
                playRect.anchorMin = new Vector2(0.38f, 0.22f);
                playRect.anchorMax = new Vector2(0.62f, 0.34f);
                playRect.offsetMin = Vector2.zero;
                playRect.offsetMax = Vector2.zero;
                playRect.anchoredPosition = Vector2.zero;
                playRect.sizeDelta = Vector2.zero;
            }
        }
    }
}
