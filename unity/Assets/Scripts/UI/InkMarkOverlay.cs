/*
 * InkMarkOverlay.cs
 *
 * 📝 역할: DEC-118(디자인 결정)·DEC-127(이번 Unity 포팅). docs/design-system/styles.css의
 * .inkmark/.ink-loop/.ink-wash(SVG stroke-dashoffset으로 손으로 동그라미를 그리는 애니메이션
 * + 옅은 잉크 번짐)을 Unity uGUI로 단순화해 근사한다.
 *
 * docs/design-system/unity-mapping.html M-02 표에는 접힘선·깃펜 두 항목만 있고 잉크마크의
 * 구체적인 Unity 매핑이 없었다 — 이번 작업(DEC-127)에서 방식을 직접 정했다:
 * 임의 SVG 벡터 궤적을 그대로 그리는 대신, 새 이미지 에셋을 추가하지 않기 위해 Unity
 * 에디터 내장 원형 스프라이트(AssetDatabase.GetBuiltinExtraResource&lt;Sprite&gt;("UI/Skin/Knob.psd"))를
 * 진남색(UIColors.InkMark)으로 물들이고, Image.type=Filled/fillMethod=Radial360의 fillAmount를
 * 0→1로 코루틴 애니메이션해 "원을 그리는" 손맛을 낸다. 다 그려지면 옅은 알파
 * (settledAlpha, 원본 CSS ink-fill 최종 opacity .16과 동일 값)로 가라앉혀 잉크 번짐을 표현한다.
 * 완전히 동일한 SVG draw-in을 재현하려 하지 않는다(과설계 금지 지시에 따름) — "선택했다는
 * 손맛"만 전달되면 충분하다.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class InkMarkOverlay : MonoBehaviour
    {
        [SerializeField] private Image ring;
        [SerializeField] private float drawDuration = 0.45f;
        // 원본 CSS .ink-fill 최종 opacity(.16)와 동일한 값 — 다 그려진 뒤 가라앉는 잉크 번짐 알파.
        [SerializeField] private float settledAlpha = 0.16f;
        // 원본 CSS .ink-draw 애니메이션 진행 중 opacity(.85)와 동일한 값.
        [SerializeField] private float drawingAlpha = 0.85f;

        private Coroutine routine;

        /// <summary>PlayMode 테스트/외부에서 애니메이션 진행 여부를 확인하기 위한 노출 값.</summary>
        public bool IsPlaying => routine != null;

        public void Show()
        {
            gameObject.SetActive(true);
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(DrawRoutine());
        }

        public void Hide()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (ring != null)
            {
                ring.fillAmount = 0f;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator DrawRoutine()
        {
            if (ring == null)
            {
                routine = null;
                yield break;
            }

            var color = ring.color;
            color.a = drawingAlpha;
            ring.color = color;
            ring.fillAmount = 0f;

            float elapsed = 0f;
            while (elapsed < drawDuration)
            {
                elapsed += Time.deltaTime;
                ring.fillAmount = Mathf.Clamp01(elapsed / drawDuration);
                yield return null;
            }

            ring.fillAmount = 1f;

            color = ring.color;
            color.a = settledAlpha;
            ring.color = color;
            routine = null;
        }
    }
}
