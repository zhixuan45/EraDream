using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EraDream.Screens
{
    // 带弹幕排版特效的跨场景加载过渡屏
    public class LoadingScreenUI : MonoBehaviour
    {
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Transform danmakuContainer;
        [SerializeField] private GameObject danmakuTextPrefab;

        private static string _targetSceneName = "MainMenuScene";

        public static void LoadScene(string sceneName)
        {
            _targetSceneName = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }

        private void Start()
        {
            StartCoroutine(CoLoadAsync());
            StartCoroutine(CoSpawnDanmaku());
        }

        private IEnumerator CoLoadAsync()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(_targetSceneName);
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                if (progressBar != null) progressBar.value = progress;
                if (progressText != null) progressText.text = $"Loading... {(progress * 100):F0}%";

                if (op.progress >= 0.9f)
                {
                    yield return new WaitForSeconds(0.5f);
                    op.allowSceneActivation = true;
                }
                yield return null;
            }
        }

        private IEnumerator CoSpawnDanmaku()
        {
            string[] sampleDanmaku = new string[]
            {
                "EraDream 剧情引擎加载中...",
                "正在生成故事节点树...",
                "初始化全局角色数据库...",
                "解压本地资源包中...",
                "祝您游玩愉快！"
            };

            while (true)
            {
                if (danmakuContainer != null && danmakuTextPrefab != null)
                {
                    var obj = Instantiate(danmakuTextPrefab, danmakuContainer);
                    var rect = obj.GetComponent<RectTransform>();
                    var tmp = obj.GetComponent<TextMeshProUGUI>();

                    if (tmp != null)
                    {
                        tmp.text = sampleDanmaku[Random.Range(0, sampleDanmaku.Length)];
                    }

                    if (rect != null)
                    {
                        float randomY = Random.Range(-300f, 300f);
                        rect.anchoredPosition = new Vector2(1000f, randomY);
                        StartCoroutine(CoMoveDanmaku(rect));
                    }
                }
                yield return new WaitForSeconds(0.8f);
            }
        }

        private IEnumerator CoMoveDanmaku(RectTransform rect)
        {
            float speed = Random.Range(200f, 400f);
            while (rect != null && rect.anchoredPosition.x > -1200f)
            {
                rect.anchoredPosition += Vector2.left * (speed * Time.deltaTime);
                yield return null;
            }
            if (rect != null) Destroy(rect.gameObject);
        }
    }
}
