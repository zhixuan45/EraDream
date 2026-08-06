using System.Collections.Generic;
using UnityEngine;

namespace EraDream.Core
{
    public class StickerItem
    {
        public int Slot { get; set; }
        public string Path { get; set; } = "";
        public Vector2 Position { get; set; } = new Vector2(0.5f, 0.5f);
        public float Scale { get; set; } = 1.0f;
        public bool IsVisible { get; set; } = true;
    }

    // 运行时贴纸/特效图层管理器
    public class StickerManager : MonoBehaviour
    {
        public static StickerManager Instance { get; private set; }

        private readonly Dictionary<int, StickerItem> _stickers = new Dictionary<int, StickerItem>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetSticker(int slot, string path, Vector2 pos, float scale, bool isVisible)
        {
            _stickers[slot] = new StickerItem
            {
                Slot = slot,
                Path = path,
                Position = pos,
                Scale = scale,
                IsVisible = isVisible
            };
        }

        public StickerItem GetSticker(int slot)
        {
            _stickers.TryGetValue(slot, out var item);
            return item;
        }

        public void ClearAll()
        {
            _stickers.Clear();
        }
    }
}
