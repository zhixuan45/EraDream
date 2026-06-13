# =====================================================================
# 一键打包并部署测试马娘包至 Godot 沙盒扩展目录的脚本
# =====================================================================

$packId = "test.manual_uma"
$srcDir = "c:\Users\JuziD\godot\eradream\test_uma_pack_src"
$zipPath = "c:\Users\JuziD\godot\eradream\test.manual_uma.zip"
$targetDir = "$env:APPDATA\Godot\app_userdata\EraDream\extensions"

Write-Host ">>> 开始打包测试扩展包: $packId..." -ForegroundColor Cyan

# 1. 确保源目录存在
if (-not (Test-Path $srcDir)) {
    Write-Error "错误: 测试包源目录不存在: $srcDir"
    exit 1
}

# 2. 如果存在旧的压缩包，先清理
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# 3. 将 test_uma_pack_src 压缩成 zip 包
try {
    # 采用 PowerShell 原生的 Compress-Archive 压缩为 zip
    Compress-Archive -Path "$srcDir\*" -DestinationPath $zipPath -Force
    Write-Host ">>> 成功压缩为临时包: $zipPath" -ForegroundColor Green
} catch {
    Write-Error "错误: 压缩失败! $_"
    exit 1
}

# 4. 确保 Godot 的沙盒 extensions 目录存在
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host ">>> 创建 Godot 扩展沙盒目录: $targetDir" -ForegroundColor Yellow
}

# 5. 复制压缩包到扩展沙盒中并改名为 .umaext
try {
    Copy-Item $zipPath -Destination "$targetDir\$packId.umaext" -Force
    Write-Host ">>> [成功] 扩展包已完美部署到 Godot 沙盒目录!" -ForegroundColor Green
    Write-Host ">>> 物理路径: $targetDir\$packId.umaext" -ForegroundColor Cyan
    Write-Host ">>> 现在您可以直接启动游戏，在扩展管理器中激活后开始手动测试养成系统！" -ForegroundColor Green
    
    # 清理临时 zip 包
    Remove-Item $zipPath -Force
} catch {
    Write-Error "错误: 复制失败! $_"
    exit 1
}
