using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Core.Models.Nodes;

namespace EraDream.StoryEditor
{
    // 选项 A：Unity 运行时节点图编辑器画布主控制器 (对应原 Godot GraphEdit)
    public class RuntimeNodeEditorCanvas : MonoBehaviour
    {
        [Header("Canvas References")]
        [SerializeField] private RectTransform nodeContainer;
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private UILineRenderer lineRendererPrefab;

        private readonly List<BaseNodeData> _storyNodes = new List<BaseNodeData>();
        private readonly Dictionary<string, RuntimeNodeViewUI> _nodeViews = new Dictionary<string, RuntimeNodeViewUI>();

        public IReadOnlyList<BaseNodeData> StoryNodes => _storyNodes;

        public RuntimeNodeViewUI CreateNode(BaseNodeData nodeData)
        {
            _storyNodes.Add(nodeData);

            if (nodePrefab != null && nodeContainer != null)
            {
                var obj = Instantiate(nodePrefab, nodeContainer);
                var view = obj.GetComponent<RuntimeNodeViewUI>();
                if (view != null)
                {
                    view.BindData(nodeData);
                    view.OnDeleteRequested += RemoveNode;
                    _nodeViews[nodeData.Id] = view;
                    return view;
                }
            }
            return null;
        }

        public void RemoveNode(RuntimeNodeViewUI view)
        {
            if (view == null || view.NodeData == null) return;

            string id = view.NodeData.Id;
            _storyNodes.RemoveAll(n => n.Id == id);
            _nodeViews.Remove(id);

            Destroy(view.gameObject);
        }

        public string ExportToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(_storyNodes, options);
        }

        public void ImportFromJson(string json)
        {
            ClearAll();
            try
            {
                var list = JsonSerializer.Deserialize<List<BaseNodeData>>(json);
                if (list != null)
                {
                    foreach (var n in list)
                    {
                        CreateNode(n);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RuntimeNodeEditorCanvas] 反序列化 JSON 剧情文件失败: {ex.Message}");
            }
        }

        public void ClearAll()
        {
            foreach (var kvp in _nodeViews)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _nodeViews.Clear();
            _storyNodes.Clear();
        }
    }

    // 用于连接线绘制的简单 UI 线段绘制辅助类
    public class UILineRenderer : MaskableGraphic
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public float Thickness = 3.0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 dir = (EndPoint - StartPoint).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (Thickness * 0.5f);

            UIVertex v1 = UIVertex.simpleVert;
            v1.color = color;
            v1.position = StartPoint - normal;

            UIVertex v2 = UIVertex.simpleVert;
            v2.color = color;
            v2.position = StartPoint + normal;

            UIVertex v3 = UIVertex.simpleVert;
            v3.color = color;
            v3.position = EndPoint + normal;

            UIVertex v4 = UIVertex.simpleVert;
            v4.color = color;
            v4.position = EndPoint - normal;

            vh.AddUIVertexQuad(new UIVertex[] { v1, v2, v3, v4 });
        }
    }
}
