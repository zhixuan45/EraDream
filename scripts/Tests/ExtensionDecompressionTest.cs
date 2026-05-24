using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UmaEraArchive.Core.Extensions;

namespace UmaEraArchive.Tests
{
    public partial class ExtensionDecompressionTest : Node
    {
        public override async void _Ready()
        {
            GD.Print("[ExtensionDecompressionTest] Starting...");
            try 
            {
                await TestDecompression();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ExtensionDecompressionTest] Exception: {ex.Message}\n{ex.StackTrace}");
            }
            GD.Print("[ExtensionDecompressionTest] Finished.");
        }

        private async Task TestDecompression()
        {
            string extDir = "user://extensions";
            string cacheDir = "user://cache/ext";
            string testId = "test_zip_ext";
            string archivePath = Path.Combine(ProjectSettings.GlobalizePath(extDir), testId + ".umaext");

            // Ensure directory exists
            string globalExtDir = ProjectSettings.GlobalizePath(extDir);
            if (!Directory.Exists(globalExtDir)) Directory.CreateDirectory(globalExtDir);

            // 1. Create a dummy .umaext
            GD.Print($"[ExtensionDecompressionTest] Creating dummy archive at {archivePath}...");
            using var packer = new ZipPacker();
            Error err = packer.Open(archivePath);
            if (err != Error.Ok)
            {
                GD.PrintErr($"[ExtensionDecompressionTest] Failed to open packer: {err}");
                return;
            }
            
            var manifest = new ExtensionManifest {
                Id = testId,
                Name = "Test Extension",
                Type = PackType.Character
            };
            string manifestJson = JsonSerializer.Serialize(manifest);
            packer.StartFile("manifest.json");
            packer.WriteFile(System.Text.Encoding.UTF8.GetBytes(manifestJson));
            
            packer.StartFile("Logic/dummy.txt");
            packer.WriteFile(System.Text.Encoding.UTF8.GetBytes("hello"));
            
            packer.Close();

            // 2. Scan
            GD.Print("[ExtensionDecompressionTest] Scanning...");
            ExtensionManager.Instance.ScanExtensions();

            // 3. Activate
            GD.Print($"[ExtensionDecompressionTest] Activating {testId}...");
            bool success = await ExtensionManager.Instance.ActivateExtension(testId);
            
            // 4. Test Risk Blocking
            GD.Print("[ExtensionDecompressionTest] Testing risk blocking...");
            string riskyId = "risky_ext";
            var riskyManifest = new ExtensionManifest {
                Id = riskyId,
                Name = "Risky Extension",
                Type = PackType.Gameplay
            };
            riskyManifest.DetectedPermissions.Add("文件系统访问"); // Manually inject risk
            
            // Mock loaded manifest
            // Note: ExtensionManager doesn't expose _loadedManifests easily, 
            // but we can trigger ScanExtensions after creating a folder
            string riskyPath = Path.Combine(globalExtDir, riskyId);
            if (!Directory.Exists(riskyPath)) Directory.CreateDirectory(riskyPath);
            File.WriteAllText(Path.Combine(riskyPath, "manifest.json"), JsonSerializer.Serialize(riskyManifest));
            
            ExtensionManager.Instance.ScanExtensions();
            
            bool riskySuccess = await ExtensionManager.Instance.ActivateExtension(riskyId);
            if (!riskySuccess)
            {
                GD.Print("[ExtensionDecompressionTest] Risk blocking works (returned false).");
                
                // Now authorize and try again
                var targetManifest = ExtensionManager.Instance.GetAvailableExtensions().First(m => m.Id == riskyId);
                targetManifest.IsAuthorized = true;
                GD.Print("[ExtensionDecompressionTest] Authorizing and retrying...");
                riskySuccess = await ExtensionManager.Instance.ActivateExtension(riskyId);
                if (riskySuccess)
                {
                    GD.Print("[ExtensionDecompressionTest] Activation after authorization success!");
                }
                else
                {
                    GD.PrintErr("[ExtensionDecompressionTest] Activation after authorization FAILED!");
                }
            }
            else
            {
                GD.PrintErr("[ExtensionDecompressionTest] Risk blocking FAILED (returned true)!");
            }
        }
    }
}
