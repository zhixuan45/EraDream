using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Core;
using EraDream.Core.Models.Nodes;
using EraDream.Services;

namespace EraDream.RuntimeEngine
{
    // Unity 平台核心剧情解释播放引擎 (Story Player Engine)
    public class StoryPlayerEngine : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private GameObject dialogueBox;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image overlayImage;
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private Button interactButton;
        [SerializeField] private Transform characterContainer;
        [SerializeField] private GameObject characterPrefab;

        private readonly Dictionary<string, BaseNodeData> _nodeMap = new Dictionary<string, BaseNodeData>();
        private readonly Dictionary<string, CharacterSpriteUI> _activeSprites = new Dictionary<string, CharacterSpriteUI>();

        private BaseNodeData _currentNode;
        private bool _isTextAnimating;
        private Coroutine _typewriterCoroutine;
        private string _targetFullText = "";

        public event Action OnStoryFinished;

        private void Start()
        {
            if (interactButton != null)
            {
                interactButton.onClick.AddListener(OnInteractButtonClicked);
            }
        }

        public void LoadAndPlayStory(List<BaseNodeData> nodes, string startNodeId = null)
        {
            _nodeMap.Clear();
            foreach (var node in nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.Id))
                {
                    _nodeMap[node.Id] = node;
                }
            }

            BaseNodeData startNode = null;
            if (!string.IsNullOrEmpty(startNodeId) && _nodeMap.TryGetValue(startNodeId, out var found))
            {
                startNode = found;
            }
            else
            {
                // 自动寻找 StartNodeData
                foreach (var n in nodes)
                {
                    if (n is StartNodeData)
                    {
                        startNode = n;
                        break;
                    }
                }
            }

            if (startNode != null)
            {
                ExecuteNode(startNode);
            }
            else
            {
                Debug.LogError("[StoryPlayerEngine] 未能找到有效起始节点!");
            }
        }

        private void ExecuteNode(BaseNodeData node)
        {
            _currentNode = node;
            if (node == null)
            {
                FinishStory();
                return;
            }

            switch (node)
            {
                case StartNodeData startNode:
                    ExecuteNextNode(startNode.NextNodeId);
                    break;

                case DialogueNodeData dialogue:
                    ShowDialogue(dialogue);
                    break;

                case NarrativeNodeData narrative:
                    ShowNarrative(narrative);
                    break;

                case BackgroundNodeData bgNode:
                    ChangeBackground(bgNode);
                    ExecuteNextNode(bgNode.NextNodeId);
                    break;

                case MusicNodeData musicNode:
                    PlayMusic(musicNode);
                    ExecuteNextNode(musicNode.NextNodeId);
                    break;

                case ChoiceNodeData choiceNode:
                    ShowChoices(choiceNode);
                    break;

                case BranchNodeData branchNode:
                    ExecuteBranch(branchNode);
                    break;

                case ValueNodeData valueNode:
                    ExecuteValueChange(valueNode);
                    ExecuteNextNode(valueNode.NextNodeId);
                    break;

                case EndNodeData _:
                    FinishStory();
                    break;

                default:
                    ExecuteNextNode(node.NextNodeId);
                    break;
            }
        }

        private void ShowDialogue(DialogueNodeData dialogue)
        {
            if (dialogueBox != null) dialogueBox.SetActive(true);
            if (nameText != null) nameText.text = dialogue.Speaker;

            _targetFullText = dialogue.Text;
            StartTypewriterAnimation(dialogue.Text);
        }

        private void ShowNarrative(NarrativeNodeData narrative)
        {
            if (dialogueBox != null) dialogueBox.SetActive(true);
            if (nameText != null) nameText.text = "";

            _targetFullText = narrative.Text;
            StartTypewriterAnimation(narrative.Text);
        }

        private void StartTypewriterAnimation(string text)
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(CoTypewriter(text));
        }

        private IEnumerator CoTypewriter(string text)
        {
            _isTextAnimating = true;
            if (contentText != null) contentText.text = "";

            float delay = SettingsManager.Instance != null ? SettingsManager.Instance.CurrentSettings.TextSpeed : 0.05f;

            for (int i = 0; i <= text.Length; i++)
            {
                if (contentText != null) contentText.text = text.Substring(0, i);
                yield return new WaitForSeconds(delay);
            }

            _isTextAnimating = false;
        }

        private void OnInteractButtonClicked()
        {
            if (_isTextAnimating)
            {
                // 跳过打字机动画
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                if (contentText != null) contentText.text = _targetFullText;
                _isTextAnimating = false;
            }
            else if (_currentNode != null && !(_currentNode is ChoiceNodeData))
            {
                ExecuteNextNode(_currentNode.NextNodeId);
            }
        }

        private void ExecuteNextNode(string nextNodeId)
        {
            if (!string.IsNullOrEmpty(nextNodeId) && _nodeMap.TryGetValue(nextNodeId, out var nextNode))
            {
                ExecuteNode(nextNode);
            }
            else
            {
                FinishStory();
            }
        }

        private void ChangeBackground(BackgroundNodeData bgNode)
        {
            if (backgroundImage == null || string.IsNullOrEmpty(bgNode.BackgroundPath)) return;
            ResourceProxy.Instance.LoadSprite(bgNode.BackgroundPath, sprite =>
            {
                if (sprite != null) backgroundImage.sprite = sprite;
            });
        }

        private void PlayMusic(MusicNodeData musicNode)
        {
            if (bgmAudioSource == null) return;
            if (musicNode.StopAudio)
            {
                bgmAudioSource.Stop();
                return;
            }

            ResourceProxy.Instance.LoadAudioClip(musicNode.AudioPath, AudioType.MPEG, clip =>
            {
                if (clip != null)
                {
                    bgmAudioSource.clip = clip;
                    bgmAudioSource.loop = musicNode.IsLoop;
                    bgmAudioSource.volume = musicNode.Volume;
                    bgmAudioSource.Play();
                }
            });
        }

        private void ShowChoices(ChoiceNodeData choiceNode)
        {
            ClearChoiceButtons();
            if (choiceContainer == null || choiceButtonPrefab == null) return;

            foreach (var opt in choiceNode.Options)
            {
                var btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
                var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = opt.Text;

                var btn = btnObj.GetComponent<Button>();
                string targetId = opt.TargetNodeId;
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        ClearChoiceButtons();
                        ExecuteNextNode(targetId);
                    });
                }
            }
        }

        private void ClearChoiceButtons()
        {
            if (choiceContainer == null) return;
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void ExecuteBranch(BranchNodeData branch)
        {
            string varId = branch.VariableId;
            int val = GlobalGameState.Instance.GetVariable<int>(varId, 0);
            int targetVal = int.TryParse(branch.CompareValue, out var parsed) ? parsed : 0;

            bool result = branch.CompareOperator switch
            {
                "==" => val == targetVal,
                "!=" => val != targetVal,
                ">" => val > targetVal,
                "<" => val < targetVal,
                ">=" => val >= targetVal,
                "<=" => val <= targetVal,
                _ => false
            };

            ExecuteNextNode(result ? branch.TrueNodeId : branch.FalseNodeId);
        }

        private void ExecuteValueChange(ValueNodeData valueNode)
        {
            int current = GlobalGameState.Instance.GetVariable<int>(valueNode.VariableId, 0);
            int delta = int.TryParse(valueNode.Value, out var parsed) ? parsed : 0;

            int nextVal = valueNode.Operation switch
            {
                "Add" => current + delta,
                "Subtract" => current - delta,
                "Set" => delta,
                _ => current
            };

            GlobalGameState.Instance.SetVariable(valueNode.VariableId, nextVal);
        }

        private void FinishStory()
        {
            Debug.Log("[StoryPlayerEngine] 剧情播放结束。");
            OnStoryFinished?.Invoke();
        }
    }
}
