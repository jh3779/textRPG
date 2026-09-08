/*
 * DamagePopupText.cs
 *
 * 📝 역할: DEC-133(신규) 전투 공격 이펙트 — "데미지 숫자 팝업". 피해를 입은(또는 회복한)
 * 쪽 포트레이트 근처에 "-12"(피해, 기존 위험색 UIColors.Tertiary) 또는 "+12"(회복,
 * UIColors.HealNumber) 형태의 TMP 텍스트가 나타났다가 위로 살짝(기본 40px) 떠오르며
 * 페이드아웃(기본 0.7초)되는 연출.
 *
 * DEC-127(InkMarkOverlay.cs/QuillRevealText.cs)이 쓴 패턴을 그대로 따른다: 신규 컴포넌트
 * 클래스 + 코루틴 + StopCoroutine으로 중복 실행 방지. 새 이미지 에셋은 추가하지 않고
 * 기존 TMP 텍스트 + CanvasGroup만 사용한다(요구사항 그대로).
 */

using System.Collections;
using TMPro;
using UnityEngine;

namespace TextRPG.UI
{
    public class DamagePopupText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private float riseDistance = 40f;
        [SerializeField] private float duration = 0.7f;

        private Coroutine routine;
        private Vector2 startAnchoredPosition;
        private bool hasStartPosition;

        /// <summary>PlayMode 테스트에서 코루틴 진행 여부를 확인하기 위한 노출 값.</summary>
        public bool IsPlaying => routine != null;

        /// <summary>현재 표시 중인 텍스트("-12"/"+12" 등). PlayMode 테스트에서 값 확인용.</summary>
        public string CurrentText => text != null ? text.text : null;

        public void Show(int amount, bool isHeal)
        {
            if (text == null || canvasGroup == null || rectTransform == null)
            {
                return;
            }

            if (!hasStartPosition)
            {
                // 최초 1회만 "쉬는 위치"를 기억해둔다 — 이후 Show()가 연속 호출돼도 매번 이
                // 기준 위치에서 다시 시작해야, 애니메이션이 누적으로 계속 위로 떠오르지 않는다.
                startAnchoredPosition = rectTransform.anchoredPosition;
                hasStartPosition = true;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            text.text = isHeal ? $"+{amount}" : $"-{amount}";
            text.color = isHeal ? UIColors.HealNumber : UIColors.Tertiary;
            rectTransform.anchoredPosition = startAnchoredPosition;
            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);

            routine = StartCoroutine(PopupRoutine());
        }

        private IEnumerator PopupRoutine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rectTransform.anchoredPosition = startAnchoredPosition + Vector2.up * (riseDistance * t);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            rectTransform.anchoredPosition = startAnchoredPosition;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            routine = null;
        }
    }
}
