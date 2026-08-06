using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EraDream.Core.Models.Nodes;

namespace EraDream.StoryEditor
{
    // Unity 平台运行时 Node 图表单卡片 UI 视图组件 (对应原 Godot GraphNode)
    public class RuntimeNodeViewUI : MonoBehaviour, IDragHandler, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button deleteButton;
        [SerializeField] private RectTransform inputPin;
        [SerializeField] private RectTransform outputPin;

        public BaseNodeData NodeData { get; private set; }
        public RectTransform RectTransformComponent { get; private set; }

        public event Action<RuntimeNodeViewUI> OnDeleteRequested;
        public event Action<RuntimeNodeViewUI> OnNodeSelected;
        public event Action<RuntimeNodeViewUI, Vector2> OnNodeMoved;

        private void Awake()
        {
            RectTransformComponent = GetComponent<RectTransform>();
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => OnDeleteRequested?.Invoke(this));
            }
        }

        public void BindData(BaseNodeData data)
        {
            NodeData = data;
            if (titleText != null)
            {
                titleText.text = $"{data.GetType().Name.Replace("NodeData", "")} ({data.Id.Substring(0, 4)})";
            }

            if (RectTransformComponent != null)
            {
                RectTransformComponent.anchoredPosition = new Vector2(data.PosX, data.PosY);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformComponent != null && RectTransformComponent.parent != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    RectTransformComponent.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint);

                RectTransformComponent.anchoredPosition = localPoint;
                if (NodeData != null)
                {
                    NodeData.PosX = localPoint.x;
                    NodeData.PosY = localPoint.y;
                }

                OnNodeMoved?.Invoke(this, localPoint);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeSelected?.Invoke(this);
        }

        public Vector2 GetOutputPinWorldPosition()
        {
            return outputPin != null ? outputPin.position : transform.position;
        }

        public Vector2 GetInputPinWorldPosition()
        {
            return inputPin != null ? inputPin.position : transform.position;
        }
    }
}
