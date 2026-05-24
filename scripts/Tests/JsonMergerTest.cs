using Godot;
using System;
using System.Text.Json.Nodes;
using UmaEraArchive.Core.Extensions;

namespace UmaEraArchive.Tests
{
    /// <summary>
    /// ExtensionJsonMerger 的单元测试
    /// </summary>
    public partial class JsonMergerTest : Node
    {
        public override void _Ready()
        {
            GD.Print("[JsonMergerTest] Starting tests...");
            try 
            {
                TestBaseMerge();
                TestArrayAppend();
                TestOverride();
                GD.Print("[JsonMergerTest] All tests finished successfully.");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[JsonMergerTest] Test suite failed with exception: {ex.Message}");
                GD.PrintErr(ex.StackTrace);
            }
        }

        private void TestBaseMerge()
        {
            var target = JsonNode.Parse("{\"a\": 1, \"b\": {\"c\": 2}}");
            var source = JsonNode.Parse("{\"b\": {\"d\": 3}, \"e\": 4}");
            
            var result = ExtensionJsonMerger.Merge(target, source);
            
            string json = result.ToJsonString();
            GD.Print($"[JsonMergerTest] Base Merge Result: {json}");
            
            // Expected: {"a": 1, "b": {"c": 2, "d": 3}, "e": 4}
            bool success = result["a"].GetValue<int>() == 1 && 
                           result["b"]["c"].GetValue<int>() == 2 && 
                           result["b"]["d"].GetValue<int>() == 3 && 
                           result["e"].GetValue<int>() == 4;

            if (success)
            {
                GD.Print("[JsonMergerTest] Base Merge PASSED");
            }
            else
            {
                throw new Exception("Base Merge Result mismatch");
            }
        }

        private void TestArrayAppend()
        {
            var target = JsonNode.Parse("{\"items\": [1, 2]}");
            var source = JsonNode.Parse("{\"items\": [3, 4]}");
            
            var result = ExtensionJsonMerger.Merge(target, source);
            
            string json = result.ToJsonString();
            GD.Print($"[JsonMergerTest] Array Append Result: {json}");
            
            // Expected: {"items": [1, 2, 3, 4]}
            var arr = result["items"].AsArray();
            bool success = arr.Count == 4 && 
                           arr[0].GetValue<int>() == 1 &&
                           arr[1].GetValue<int>() == 2 &&
                           arr[2].GetValue<int>() == 3 && 
                           arr[3].GetValue<int>() == 4;

            if (success)
            {
                GD.Print("[JsonMergerTest] Array Append PASSED");
            }
            else
            {
                throw new Exception("Array Append Result mismatch");
            }
        }

        private void TestOverride()
        {
            var target = JsonNode.Parse("{\"data\": {\"old\": 1, \"nested\": {\"x\": 10}}}");
            var source = JsonNode.Parse("{\"data\": {\"new\": 2, \"override\": true}}");
            
            var result = ExtensionJsonMerger.Merge(target, source);
            
            string json = result.ToJsonString();
            GD.Print($"[JsonMergerTest] Override Result: {json}");
            
            // Expected: {"data": {"new": 2}}
            var dataObj = result["data"].AsObject();
            bool success = dataObj.Count == 1 && 
                           dataObj.ContainsKey("new") && 
                           !dataObj.ContainsKey("old") && 
                           !dataObj.ContainsKey("override");

            if (success)
            {
                GD.Print("[JsonMergerTest] Override PASSED");
            }
            else
            {
                throw new Exception("Override Result mismatch");
            }
        }
    }
}
