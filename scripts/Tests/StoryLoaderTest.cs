using Godot;
using System;
using System.Collections.Generic;
using EraDream.StoryEditor;
using EraDream.StoryEditor.Nodes;

namespace EraDream.Tests
{
    /// <summary>
    /// 覆盖剧情加载格式与运行时寻址所依赖的数据校验。
    /// </summary>
    public partial class StoryLoaderTest : Node
    {
        public override void _Ready()
        {
            try
            {
                TestLegacyArrayFormat();
                TestProjectObjectFormat();
                TestEmptyFile();
                TestInvalidJson();
                TestDuplicateId();
                TestDanglingReferences();
                GD.Print("[StoryLoaderTest] All tests passed.");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[StoryLoaderTest] Failed: {ex}");
                // 不吞掉异常，确保无聚合测试框架时引擎和 CI 仍能观察到失败。
                throw;
            }
        }

        private static void TestLegacyArrayFormat()
        {
            string path = WriteFixture("legacy", "[{\"$type\":\"start\",\"Id\":\"start\",\"NextNodeId\":\"\"}]");
            Assert(StoryNodeManager.TryLoadProject(path, out var nodes, out string error), error);
            Assert(nodes.Count == 1 && nodes[0] is StartNodeData, "旧数组格式未正确加载。");
        }

        private static void TestProjectObjectFormat()
        {
            string path = WriteFixture("object", "{\"SchemaVersion\":1,\"Nodes\":[{\"$type\":\"end\",\"Id\":\"end\"}]}");
            Assert(StoryNodeManager.TryLoadProject(path, out var nodes, out string error), error);
            Assert(nodes.Count == 1 && nodes[0] is EndNodeData, "项目对象格式未正确加载。");
        }

        private static void TestEmptyFile()
        {
            string path = WriteFixture("empty", "");
            Assert(!StoryNodeManager.TryLoadProject(path, out var nodes, out string error), "空文件不应加载成功。");
            Assert(nodes.Count == 0, "空文件失败后不应留下部分节点。");
            Assert(error.Contains("为空"), $"空文件错误原因不明确: {error}");
        }

        private static void TestInvalidJson()
        {
            string path = WriteFixture("invalid", "{not-json}");
            Assert(!StoryNodeManager.TryLoadProject(path, out var nodes, out string error), "损坏 JSON 不应加载成功。");
            Assert(nodes.Count == 0, "损坏 JSON 失败后不应留下部分节点。");
            Assert(!string.IsNullOrWhiteSpace(error), "损坏 JSON 应返回错误原因。");
        }

        private static void TestDuplicateId()
        {
            string json = "[{\"$type\":\"start\",\"Id\":\"same\"},{\"$type\":\"end\",\"Id\":\"same\"}]";
            string path = WriteFixture("duplicate", json);
            Assert(!StoryNodeManager.TryLoadProject(path, out _, out string error), "重复节点 ID 不应加载成功。");
            Assert(error.Contains("重复"), $"未返回重复节点 ID 错误: {error}");
        }

        private static void TestDanglingReferences()
        {
            // 同时覆盖普通出口、选项出口和分支出口，防止只校验 NextNodeId。
            string json = "["
                + "{\"$type\":\"start\",\"Id\":\"start\",\"NextNodeId\":\"missing_next\"},"
                + "{\"$type\":\"choice\",\"Id\":\"choice\",\"Options\":[{\"Text\":\"A\",\"TargetNodeId\":\"missing_choice\"}]},"
                + "{\"$type\":\"branch\",\"Id\":\"branch\",\"SuccessNodeId\":\"missing_success\",\"FailNodeId\":\"missing_fail\"}"
                + "]";
            string path = WriteFixture("dangling", json);
            Assert(!StoryNodeManager.TryLoadProject(path, out _, out string error), "悬空节点引用不应加载成功。");
            Assert(error.Contains("missing_next"), $"未识别普通悬空引用: {error}");
            Assert(error.Contains("missing_choice"), $"未识别选项悬空引用: {error}");
            Assert(error.Contains("missing_success"), $"未识别成功分支悬空引用: {error}");
            Assert(error.Contains("missing_fail"), $"未识别失败分支悬空引用: {error}");
        }

        private static string WriteFixture(string name, string content)
        {
            string path = $"user://story_loader_{name}.json";
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            Assert(file != null, $"无法创建测试文件: {path}");
            file.StoreString(content);
            return path;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
