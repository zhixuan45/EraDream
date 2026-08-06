namespace EraDream.Core
{
    // 应用持久化配置参数
    public class AppSettings
    {
        public bool IsDarkMode { get; set; } = true;
        public float MasterVolume { get; set; } = 1.0f;
        public float BgmVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 1.0f;
        public float VoiceVolume { get; set; } = 1.0f;
        public bool IsFullscreen { get; set; } = false;
        public string Language { get; set; } = "zh_CN";
        public float TextSpeed { get; set; } = 0.05f; // 打字机文字打字速度 (秒/字)
        public bool AutoPlay { get; set; } = false;
        public float AutoPlayDelay { get; set; } = 2.0f;
    }
}
