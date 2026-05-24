using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

namespace UmaEraArchive.Core.Extensions
{
    /// <summary>
    /// 通用的 JSON 自适应合并引擎，支持递归合并、数组追加和 override 覆盖
    /// </summary>
    public static class ExtensionJsonMerger
    {
        /// <summary>
        /// 递归合并两个 JSON 节点。
        /// 注意：此操作会修改 target 节点及其子节点，如果需要保持 target 不变，请预先 DeepClone。
        /// </summary>
        /// <param name="target">目标节点，合并后的结果将反映在此节点上</param>
        /// <param name="source">来源节点，提供要合并的数据</param>
        /// <returns>合并后的节点。如果触发了 override，可能会返回一个新的节点实例</returns>
        public static JsonNode Merge(JsonNode target, JsonNode source)
        {
            if (source == null) return target;

            // 1. 检查 override 标志：如果 source 包含 "override": true，则完全替换
            if (source is JsonObject sourceObj && sourceObj.TryGetPropertyValue("override", out var overrideNode))
            {
                if (overrideNode != null && overrideNode.GetValueKind() == JsonValueKind.True)
                {
                    var result = source.DeepClone().AsObject();
                    result.Remove("override");
                    return result;
                }
            }

            if (target == null) return source.DeepClone();

            // 2. 类型不匹配时直接覆盖（以 source 为准）
            if (target.GetValueKind() != source.GetValueKind())
            {
                return source.DeepClone();
            }

            // 3. 递归合并对象
            if (target is JsonObject tObj && source is JsonObject sObj)
            {
                // 我们需要克隆键列表，因为在遍历过程中可能会修改 tObj
                var sourceKeys = new System.Collections.Generic.List<string>();
                foreach (var kvp in sObj) sourceKeys.Add(kvp.Key);

                foreach (var key in sourceKeys)
                {
                    JsonNode sValue = sObj[key];
                    if (tObj.TryGetPropertyValue(key, out var tValue))
                    {
                        // 递归合并同名属性，并更新引用（Merge 可能返回新实例）
                        tObj[key] = Merge(tValue, sValue);
                    }
                    else
                    {
                        // 目标不存在该键，直接克隆添加
                        tObj[key] = sValue?.DeepClone();
                    }
                }
                return tObj;
            }
            
            // 4. 合并数组（追加模式）
            if (target is JsonArray tArr && source is JsonArray sArr)
            {
                foreach (var item in sArr)
                {
                    tArr.Add(item?.DeepClone());
                }
                return tArr;
            }

            // 5. 基本类型（Value），直接覆盖
            return source.DeepClone();
        }
    }
}
