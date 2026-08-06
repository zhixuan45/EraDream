using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace EraDream.Services
{
    // 跨平台动态资源代理 (支持动态加载本地磁盘图像、音频或 Resources 包内资源)
    public class ResourceProxy : MonoBehaviour
    {
        public static ResourceProxy Instance { get; private set; }

        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

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

        /// <summary>
        /// 同步或异步从本地磁盘/包内加载 Texture2D 并转为 Sprite
        /// </summary>
        public void LoadSprite(string path, Action<Sprite> onComplete)
        {
            if (string.IsNullOrEmpty(path))
            {
                onComplete?.Invoke(null);
                return;
            }

            if (_spriteCache.TryGetValue(path, out var cachedSprite) && cachedSprite != null)
            {
                onComplete?.Invoke(cachedSprite);
                return;
            }

            // 1. 尝试从 Resources 目录加载
            Sprite resSprite = Resources.Load<Sprite>(path);
            if (resSprite != null)
            {
                _spriteCache[path] = resSprite;
                onComplete?.Invoke(resSprite);
                return;
            }

            // 2. 从本地物理文件路径加载
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    _spriteCache[path] = sprite;
                    onComplete?.Invoke(sprite);
                    return;
                }
            }

            // 3. 尝试作为网络/虚拟 URL 通过 UnityWebRequest 加载
            StartCoroutine(CoLoadTextureFromUrl(path, sprite =>
            {
                if (sprite != null) _spriteCache[path] = sprite;
                onComplete?.Invoke(sprite);
            }));
        }

        private IEnumerator CoLoadTextureFromUrl(string url, Action<Sprite> callback)
        {
            using UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                callback?.Invoke(sprite);
            }
            else
            {
                Debug.LogWarning($"[ResourceProxy] 无法加载图像: {url}, Error: {www.error}");
                callback?.Invoke(null);
            }
        }

        /// <summary>
        /// 异步从本地磁盘加载 AudioClip 音频资源
        /// </summary>
        public void LoadAudioClip(string path, AudioType audioType, Action<AudioClip> onComplete)
        {
            if (string.IsNullOrEmpty(path))
            {
                onComplete?.Invoke(null);
                return;
            }

            // 尝试 Resources 加载
            AudioClip resClip = Resources.Load<AudioClip>(path);
            if (resClip != null)
            {
                onComplete?.Invoke(resClip);
                return;
            }

            string fullPath = path.StartsWith("file://") ? path : "file://" + Path.GetFullPath(path);
            StartCoroutine(CoLoadAudioClip(fullPath, audioType, onComplete));
        }

        private IEnumerator CoLoadAudioClip(string fileUrl, AudioType audioType, Action<AudioClip> callback)
        {
            using UnityWebRequest www = UnityWebRequestAudio.GetAudioClip(fileUrl, audioType);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                callback?.Invoke(clip);
            }
            else
            {
                Debug.LogWarning($"[ResourceProxy] 无法加载音频: {fileUrl}, Error: {www.error}");
                callback?.Invoke(null);
            }
        }
    }
}
