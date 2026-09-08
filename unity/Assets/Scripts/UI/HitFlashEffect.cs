/*
 * HitFlashEffect.cs
 *
 * 📝 역할: DEC-133(신규) 전투 공격 이펙트 — "피격 플래시". 공격을 맞은 쪽 포트레이트
 * (ExplorePanelController의 PlayerPortrait/EnemyPortrait Image)의 색을 아주 짧게
 * (기본 0.18초) 밝은 붉은색(UIColors.HitFlash)으로 틴트했다가 원래 색으로 되돌린다.
 *
 * DEC-127(InkMarkOverlay.cs/QuillRevealText.cs)이 쓴 패턴을 그대로 따른다: 신규 컴포넌트
 * 클래스 + 코루틴 + StopCoroutine으로 중복 실행 방지. 새 이미지 에셋은 추가하지 않고
 * 기존 포트레이트 Image의 color만 조작한다(요구사항 그대로).
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class HitFlashEffect : MonoBehaviour
    {
        [SerializeField] private Image target;
        [SerializeField] private Color flashColor = UIColors.HitFlash;
        [SerializeField] private float flashDuration = 0.18f;

        private Coroutine routine;
        private Color originalColor;
        private bool hasOriginalColor;

        /// <summary>PlayMode 테스트에서 코루틴 진행 여부를 확인하기 위한 노출 값.</summary>
        public bool IsPlaying => routine != null;

        public void Flash()
        {
            if (target == null)
            {
                return;
            }

            if (routine != null)
            {
                // 연속 공격 시 이전 이펙트가 끝나지 않은 채로 남아있지 않도록, 중단하기 전에
                // 원래 색을 먼저 즉시 복원한다(그렇지 않으면 원본 코루틴이 캡처한 flashColor가
                // 그대로 남아 다음 Flash()가 그 색을 "원본"으로 잘못 저장하는 문제가 생긴다).
                StopCoroutine(routine);
                if (hasOriginalColor)
                {
                    target.color = originalColor;
                }
            }
            else
            {
                originalColor = target.color;
                hasOriginalColor = true;
            }

            routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            target.color = flashColor;

            yield return new WaitForSeconds(flashDuration);

            target.color = originalColor;
            routine = null;
        }
    }
}
