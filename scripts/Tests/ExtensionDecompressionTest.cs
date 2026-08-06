using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EraDream.Core.Extensions;

namespace EraDream.Tests
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
            
            // 4. Risk Blocking Test was removed because DLL plugin security checks are deprecated.
            GD.Print("[ExtensionDecompressionTest] Risk blocking test skipped (DLL plugins deprecated).");
        }
    }
}
