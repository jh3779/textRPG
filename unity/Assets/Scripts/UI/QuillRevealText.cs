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
 * 신규(DEC-138): 펜촉을 잉크브라운으로 물들인 원형(Unity 내장 Knob) 대신 실제 깃펜 이미지
 * 6프레임(art-assets/vfx_quill_idle.png, vfx_quill_writing_01~04.png, vfx_quill_writing_end.png,
 * DEC-135 버전)으로 교체한다 — 진행률(progress 0~1)에 따라 penImage.sprite를 penFrames[0..5]
 * 중 하나로 갈아끼우기만 한다(애니메이션 컨트롤러 등 무겁게 만들지 않음). 펜이 기준선을 따라
 * 좌우로 이동하는 로직(위 penIcon.anchoredPosition 갱신) 자체는 DEC-123 그대로 유지한다 —
 * 이번엔 그림(프레임)만 바꾼다. 알려진 한계(DEC-135/136): 6프레임 전부 촛불 후광 부위에 체커보드
 * 알파 잔재가 약간 남아있다 — 이미지 자체는 건드리지 않고(DEC-136에서 리터치 시도 후 실루엣
 * 손상 부작용으로 폐기됨) 있는 그대로 쓴다.
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class QuillRevealText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform penIcon; // null이어도 텍스트 드러남 자체는 동작해야 함
        [SerializeField] private CanvasGroup penCanvasGroup; // null이면 펜 알파 연출은 생략
        [SerializeField] private Image penImage; // 신규(DEC-138): 진행률별 프레임 교체 대상
        [SerializeField] private Sprite[] penFrames; // 신규(DEC-138): [idle, writing_01, writing_02, writing_03, writing_04, writing_end] 6장
        [SerializeField] private float duration = 1.6f;

        private Coroutine routine;
        private int lastFrameIndex = -1;

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
            lastFrameIndex = -1;

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
            ApplyPenFrame(0f);

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

                ApplyPenFrame(t);

                // 원본 CSS qw-pen-move: 94%까지 opacity .85 유지 후 100%에서 0으로 페이드아웃.
                if (penCanvasGroup != null && t > 0.94f)
                {
                    penCanvasGroup.alpha = Mathf.Lerp(0.85f, 0f, (t - 0.94f) / 0.06f);
                }

                yield return null;
            }

            text.maxVisibleCharacters = totalChars;
            Progress = 1f;
            ApplyPenFrame(1f);
            if (penCanvasGroup != null)
            {
                penCanvasGroup.alpha = 0f;
            }
            routine = null;
        }

        /// <summary>
        /// 신규(DEC-138): progress(0~1)를 6프레임(idle→writing_01~04→end) 구간으로 나눠
        /// penImage.sprite를 교체한다. 예시(작업 지시 기준): 0=idle, 0.2=01, 0.4=02, 0.6=03,
        /// 0.8=04, 1.0=end. 같은 구간이면 다시 대입하지 않아(lastFrameIndex 캐시) 매 프레임
        /// 불필요한 재할당을 피한다.
        /// </summary>
        private void ApplyPenFrame(float progress)
        {
            if (penImage == null || penFrames == null || penFrames.Length < 6)
            {
                return;
            }

            int index;
            if (progress >= 1f) index = 5;
            else if (progress >= 0.8f) index = 4;
            else if (progress >= 0.6f) index = 3;
            else if (progress >= 0.4f) index = 2;
            else if (progress >= 0.2f) index = 1;
            else index = 0;

            if (index == lastFrameIndex)
            {
                return;
            }
            lastFrameIndex = index;

            var frame = penFrames[index];
            if (frame != null)
            {
                penImage.sprite = frame;
            }
        }
    }
}
