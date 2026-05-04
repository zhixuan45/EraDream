# Security To-Do

## Future Enhancements
* **Mod Sandbox and Static Analysis:** Implement a static analysis pass (e.g., using Roslyn) or runtime sandboxing to detect when extension packages (Mods) attempt to call destructive components like `System.IO` (file deletion, etc.).
* **User Warning for Malicious Mods:** If a mod is detected using these potentially malicious components, alert the user and highlight the mod's name in red in the UI to indicate risk.
* **Official API for Assets:** Instead of allowing raw `System.IO` access for asset replacement, provide a safe, official API for extensions to perform tasks like modifying UI textures, reducing the need for raw file system access.
