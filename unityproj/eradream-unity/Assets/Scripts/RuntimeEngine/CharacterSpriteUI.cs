using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EraDream.Services;

namespace EraDream.RuntimeEngine
{
    // Unity uGUI 角色立绘控制组件 (支持排版定位、表情切换与拖拽偏移)
    [RequireComponent(typeof(Image))]
    public class CharacterSpriteUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public Image ImageComponent { get; private set; }
        public RectTransform RectTransformComponent { get; private set; }

        public string CharacterId { get; set; } = "";
        public string CurrentExpression { get; set; } = "";

        private Vector2 _dragOffset;

        private void Awake()
        {
            ImageComponent = GetComponent<Image>();
            RectTransformComponent = GetComponent<RectTransform>();
        }

        public void SetSprite(Sprite sprite)
        {
            if (ImageComponent != null)
            {
                ImageComponent.sprite = sprite;
                ImageComponent.enabled = (sprite != null);
                if (sprite != null)
                {
                    ImageComponent.SetNativeSize();
                }
            }
        }

        public void SetPositionAnchor(string positionKey)
        {
            if (RectTransformComponent == null) return;

            switch (positionKey.ToLower())
            {
                case "left":
                    RectTransformComponent.anchorMin = new Vector2(0.2f, 0.5f);
                    RectTransformComponent.anchorMax = new Vector2(0.2f, 0.5f);
                    break;
                case "right":
                    RectTransformComponent.anchorMin = new Vector2(0.8f, 0.5f);
                    RectTransformComponent.anchorMax = new Vector2(0.8f, 0.5f);
                    break;
                case "center":
                default:
                    RectTransformComponent.anchorMin = new Vector2(0.5f, 0.5f);
                    RectTransformComponent.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
            }
            RectTransformComponent.anchoredPosition = Vector2.zero;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RectTransformComponent.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out _dragOffset);
            _dragOffset = RectTransformComponent.anchoredPosition - _dragOffset;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RectTransformComponent.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                RectTransformComponent.anchoredPosition = localPoint + _dragOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData) { }
    }
}
