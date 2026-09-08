/*
 * QuillRevealText.cs
 *
 * 📝 역할: DEC-121(디자인 결정)·DEC-127(이번 Unity 포팅). docs/design-system/styles.css의
 * .quillwrite/.qw-text/.qw-pen(clip-path로 왼쪽부터 텍스트가 드러나고, 그 끝을 작은 펜촉
 * 아이콘이 문단 하단 기준선을 따라가는 연출)을 TMP maxVisibleCharacters 코루틴으로 근사한다.
 *
 * docs/design-system/unity-mapping.html M-02가 명시적으로 권고한 방식을 그대로 따른다 —
 * 2026-09-07 수정(review-verify-agent Major 확인) 이후로 펜 아이콘을 특정 글자(caret) 위치가
 * 아니라 텍스트 블록 전체의 하단 진행 기준선(가로 트랙)을 따라가도록 만들어야 한다는 지침을
 * 그대로 반영했다 — TMP textInfo.lineCount로 줄 단위 캐릭터 위치를 따라가려는 시도는 하지
 * 않는다(줄바꿈이 실제 폭·폰트에 따라 달라지면 어긋나는 함정, 웹 프리뷰와 동일한 이유).
 *
 * 펜촉 자체는 실제 깃펜 모양 스프라이트 대신(신규 이미지 에셋 추가 금지) 잉크브라운
 * (UIColors.QuillInk) 색의 작은 원형 Image(Unity 에디터 내장 Knob 스프라이트, 새 에셋 파일을
 * 만들지 않고 AssetDatabase.GetBuiltinExtraResource로 참조)로 "펜 끝"을 단순화해 표현한다.
 */

using System.Collections;
using TMPro;
using UnityEngine;

namespace TextRPG.UI
{
    public class QuillRevealText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform penIcon; // null이어도 텍스트 드러남 자체는 동작해야 함
        [SerializeField] private CanvasGroup penCanvasGroup; // null이면 펜 알파 연출은 생략
        [SerializeField] private float duration = 1.6f;

        private Coroutine routine;

        /// <summary>현재 진행률(0~1). PlayMode 테스트에서 시간 경과에 따라 증가하는지 확인하는 용도.</summary>
        public float Progress { get; private set; }

        public void Play(string content)
        {
            if (text == null)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            text.text = content;
            text.maxVisibleCharacters = 0;
            Progress = 0f;

            if (penIcon != null)
            {
                var pos = penIcon.anchoredPosition;
                pos.x = 0f;
                penIcon.anchoredPosition = pos;
            }
            if (penCanvasGroup != null)
            {
                penCanvasGroup.alpha = 0f;
            }

            routine = StartCoroutine(RevealRoutine());
        }

        private IEnumerator RevealRoutine()
        {
            // maxVisibleCharacters/textInfo를 계산하려면 메시가 먼저 만들어져 있어야 한다.
            text.ForceMeshUpdate();
            int totalChars = text.textInfo.characterCount;
            float trackWidth = text.rectTransform.rect.width;

            if (penCanvasGroup != null)
            {
                penCanvasGroup.alpha = 0.85f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Progress = t;

                text.maxVisibleCharacters = Mathf.CeilToInt(totalChars * t);

                if (penIcon != null)
                {
                    var pos = penIcon.anchoredPosition;
                    pos.x = trackWidth * t;
                    penIcon.anchoredPosition = pos;
                }

                // 원본 CSS qw-pen-move: 94%까지 opacity .85 유지 후 100%에서 0으로 페이드아웃.
                if (penCanvasGroup != null && t > 0.94f)
                {
                    penCanvasGroup.alpha = Mathf.Lerp(0.85f, 0f, (t - 0.94f) / 0.06f);
                }

                yield return null;
            }

            text.maxVisibleCharacters = totalChars;
            Progress = 1f;
            if (penCanvasGroup != null)
            {
                penCanvasGroup.alpha = 0f;
            }
            routine = null;
        }
    }
}
