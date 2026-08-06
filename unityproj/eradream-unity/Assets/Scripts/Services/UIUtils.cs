using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EraDream.Services
{
    // Unity UI 渐变动画与通用工具
    public static class UIUtils
    {
        /// <summary>
        /// 协程：CanvasGroup 渐变透明度 (Fade In / Fade Out)
        /// </summary>
        public static IEnumerator CoFadeCanvasGroup(CanvasGroup group, float startAlpha, float targetAlpha, float duration)
        {
            if (group == null) yield break;

            float elapsed = 0;
            group.alpha = startAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            group.alpha = targetAlpha;
        }

        /// <summary>
        /// 清空 Transform 下的所有子节点
        /// </summary>
        public static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform child in parent)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
}
