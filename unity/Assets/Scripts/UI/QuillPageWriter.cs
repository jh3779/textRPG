/*
 * QuillPageWriter.cs
 *
 * 📝 역할: DEC-145에서 만든 레이어드 깃펜 애니메이션 최소 세트(Assets/Art/UI/Quill/
 * quill_base.png, quill_shadow.png, nib_contact.png, ink_brush_thin/medium/dry.png,
 * ink_bleed_mask.png, writing_final_mask.png, parchment_clean.png, writing_path.json,
 * quill_pivot_meta.json)를 실제로 재생하는 컴포넌트다.
 *
 * docs/quill_animation_image_list.md 4.2절 합성 순서(양피지 → 누적 잉크 → 깃펜 그림자 →
 * 접점 효과 → 깃펜)를 그대로 하이어라키 형제 순서(뒤→앞)로 구현한다 — RenderTexture나
 * 커스텀 셰이더 없이, 각 레이어를 UI Image로 쌓고 잉크는 지나간 경로에 작은 Image
 * "도장"을 계속 생성해 누적시키는 방식이다(문서 6절 "과설계하지 마라" 지침에 따라
 * 가장 단순하게 실제로 동작하는 방법을 선택함). 검정 잉크 레이어는 전부 UGUI 기본
 * Image 머티리얼(표준 알파 블렌드: SrcAlpha/OneMinusSrcAlpha)만 쓰고 Additive 셰이더는
 * 어디에도 붙이지 않는다(문서 5절 "검정 잉크에 Additive 금지" 그대로 준수).
 *
 * writing_path.json의 시간 축을 따라 pen_down 구간에서만 잉크 도장을 찍고, 획(stroke_id)이
 * 바뀌는 첫 샘플에서만 ink_bleed_mask 한 장을 살짝 얹는다(문서 4.2/6 "최소 적용"). 획이
 * 끝나는 마지막 샘플에는 ink_brush_dry를 써서 펜을 뗄 때 잉크가 옅어지는 느낌을 흉내
 * 낸다. 필압(pressure)이 0.85 이상이면 medium, 그 미만이면 thin 브러시를 쓴다.
 *
 * 재생 종료 직전(finalMaskCrossfadeDuration)에는 writing_final_mask.png(검정 틴트,
 * TextureImporter alphaUsage=FromGrayScale로 흰 글자 영역만 알파를 갖도록 임포트됨)를
 * 페이드인해, 손으로 찍은 브러시 도장 근사치가 아니라 확정된 "던전게이트" 글자 마스크와
 * 픽셀 단위로 정확히 일치하는 결과가 최종적으로 드러나도록 보장한다(브러시 누적만으로는
 * 정확한 글자 형태를 재현하기 어렵다는 문서 6절의 합격 기준 "완성 문구가 정확하고
 * 읽히는가"를 이 방식으로 확실히 만족시킨다).
 *
 * 깃펜(quill_base)·그림자(quill_shadow)의 회전 피벗은 quill_pivot_meta.json이 지정한
 * 펜촉 접점(nib_contact_px, 캔버스 1024×1024 기준 (191,1006))이다. 해당 .png.meta에도 같은
 * 값이 Sprite 커스텀 spritePivot으로 구워져 있지만, UGUI Image는 spritePivot을 무시하고
 * RectTransform.pivot만 본다 — 그래서 ProjectSetupTool.BuildQuillTitleWriter()가
 * LoadPivot01FromMeta()로 quill_pivot_meta.json을 직접 읽어 QuillPen/QuillShadow의
 * RectTransform.pivot을 이 값으로 명시적으로 맞춰준다(리뷰 반영, 2026-09-14). 이 스크립트가
 * anchoredPosition에 넣는 local 좌표는 그 펜촉 피벗 지점이 위치할 좌표라고 전제한다 — pivot이
 * 어긋나면 여기 계산이 맞아도 실제로는 그림 중심이 그 자리로 이동한다. 기본 자세에서 펜대가
 * 향하는 방향(펜촉→깃털)은
 * 알파 채널 최원점 분석으로 약 50.5°(y-up, +x축 기준)로 측정했다 — 실제로 쓰는 동안은
 * 이동 방향의 반대쪽(펜대가 진행 방향 뒤로 눕는 자연스러운 자세)을 향하도록 매 샘플
 * 회전을 갱신한다.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class QuillPageWriter : MonoBehaviour
    {
        [Header("경로 데이터 (DEC-145 writing_path.json)")]
        [SerializeField] private TextAsset writingPathJson;

        [Header("표시 영역 — writing_path 좌표(2048x1024 px 공간)를 이 RectTransform 안에 맞춰 그린다")]
        [SerializeField] private RectTransform pageArea;

        [Header("레이어 (문서 4.2 합성 순서 그대로: 양피지 → 누적 잉크 → 깃펜 그림자 → 접점 효과 → 깃펜)")]
        [SerializeField] private RectTransform bleedLayer;
        [SerializeField] private RectTransform inkLayer;
        [SerializeField] private Image finalMaskImage;
        [SerializeField] private CanvasGroup penGroup; // 그림자+접점+펜을 묶어 재생 종료 시 함께 페이드아웃
        [SerializeField] private RectTransform quillShadow;
        [SerializeField] private Image quillShadowImage;
        [SerializeField] private RectTransform nibContact;
        [SerializeField] private RectTransform quillPen;

        [Header("잉크 브러시 스프라이트 (필압/획 시작·끝에 따라 전환)")]
        [SerializeField] private Sprite inkBrushThin;
        [SerializeField] private Sprite inkBrushMedium;
        [SerializeField] private Sprite inkBrushDry;
        [SerializeField] private Sprite inkBleedMaskSprite;

        [Header("튜닝")]
        [SerializeField] private float playbackSpeed = 1.8f; // 1.0=원본 타이밍(약 9.4초) 그대로
        [SerializeField] private float minStampDistance = 3f; // 로컬 px, 이보다 가까우면 도장을 새로 찍지 않음
        [SerializeField] private float stampSizeThin = 12f;
        [SerializeField] private float stampSizeMedium = 18f;
        [SerializeField] private float stampSizeDry = 14f;
        [SerializeField] private float bleedSize = 24f;
        [SerializeField] private float pressureThresholdForMedium = 0.85f;
        [SerializeField] private float finalMaskCrossfadeDuration = 0.35f;
        [SerializeField] private float penExitFadeDuration = 0.35f;
        [SerializeField] private float pageMarginPx = 40f; // path 좌표계(px) 기준 여백
        [SerializeField] private Vector2 shadowLocalOffset = new Vector2(4f, -4f);

        [Header("writing_final_mask.png 전체 캔버스 크기 (quill_pivot_meta.json coordinate_space와 동일해야 함)")]
        [SerializeField] private float fullPageWidthPx = 2048f;
        [SerializeField] private float fullPageHeightPx = 1024f;

        // quill_base.png 알파 채널 분석 결과(1024x1024 캔버스, 펜촉 접점 (191,1006) 기준
        // 가장 먼 불투명 픽셀 = 깃털 끝 (984,44)) — 기본 자세에서 펜대(펜촉→깃털)가 향하는
        // 각도(y-up, +x축 기준). Sprite 커스텀 피벗을 펜촉으로 잡았으므로, 이 값만큼의
        // 오프셋을 실제 회전 계산에서 빼줘야 "이동 반대 방향으로 펜대가 눕는" 자세가 나온다.
        private const float DefaultShaftAngleDeg = 50.5f;

        [System.Serializable]
        private class QuillPathSample
        {
            public float time;
            public float page_x;
            public float page_y;
            public float pressure;
            public bool pen_down;
            public int stroke_id;
        }

        [System.Serializable]
        private class QuillPathMeta
        {
            public string coordinate_space;
            public string text;
            public string text_status;
        }

        [System.Serializable]
        private class QuillPathData
        {
            public QuillPathMeta _meta;
            public QuillPathSample[] samples;
        }

        private QuillPathData pathData;
        private bool[] strokeStartFlags;
        private bool[] strokeEndFlags;
        private float scale;
        private float boxMinX, boxMinY, boxWidth, boxHeight;

        private Coroutine routine;
        private bool haveLastPos;
        private Vector2 lastPos;
        private float lastAngleDeg;
        private Vector2 lastStampPos;
        private int lastStampStrokeId = int.MinValue;

        private void OnEnable()
        {
            // ProjectSetupTool이 씬을 조립하는 동안(에디터 모드, Play 아님)에도 이 GameObject는
            // 기본적으로 활성 상태로 생성되어 OnEnable이 즉시 호출된다. StartCoroutine은 Play
            // 모드가 아니면 예외를 던지므로(에디터 스크립트 실행 자체가 깨짐), 실제 게임 실행
            // 중(Application.isPlaying)에만 재생을 시작한다.
            if (!Application.isPlaying)
            {
                return;
            }
            Play();
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        /// <summary>재생을 처음부터 다시 시작한다. 타이틀 화면이 다시 활성화될 때마다(OnEnable) 호출된다.</summary>
        public void Play()
        {
            if (!EnsureParsed())
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            ResetVisualState();
            routine = StartCoroutine(WriteRoutine());
        }

        private bool EnsureParsed()
        {
            if (pathData != null)
            {
                return true;
            }

            if (writingPathJson == null)
            {
                Debug.LogError("[QuillPageWriter] writingPathJson 참조가 없습니다 — 재생할 수 없습니다.");
                return false;
            }
            if (pageArea == null)
            {
                Debug.LogError("[QuillPageWriter] pageArea 참조가 없습니다 — 좌표를 매핑할 기준 사각형이 필요합니다.");
                return false;
            }

            pathData = JsonUtility.FromJson<QuillPathData>(writingPathJson.text);
            if (pathData?.samples == null || pathData.samples.Length == 0)
            {
                Debug.LogError("[QuillPageWriter] writing_path.json 파싱에 실패했거나 samples가 비어 있습니다.");
                pathData = null;
                return false;
            }

            ComputeBoundingBox();
            ComputeStrokeFlags();
            PositionFinalMask();
            return true;
        }

        /// <summary>
        /// writing_final_mask.png는 손으로 찍은 브러시 도장과 달리 페이지 전체(2048x1024)
        /// 캔버스 좌표계를 그대로 쓰는 이미지다. ComputeBoundingBox()가 정한 것과 동일한
        /// scale/원점으로 배치해야 도장이 지나간 자리와 최종 마스크의 글자 위치가 정확히
        /// 겹친다 — MapToLocal(0,0)이 곧 이 이미지의 좌상단 픽셀이 와야 할 로컬 좌표다.
        /// </summary>
        private void PositionFinalMask()
        {
            if (finalMaskImage == null) return;

            var mrt = finalMaskImage.rectTransform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot = new Vector2(0f, 1f); // 좌상단 기준 — MapToLocal(0,0)과 대응시키기 위함
            mrt.sizeDelta = new Vector2(fullPageWidthPx * scale, fullPageHeightPx * scale);
            mrt.anchoredPosition = MapToLocal(0f, 0f);
        }

        private void ComputeBoundingBox()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var s in pathData.samples)
            {
                if (!s.pen_down) continue;
                if (s.page_x < minX) minX = s.page_x;
                if (s.page_x > maxX) maxX = s.page_x;
                if (s.page_y < minY) minY = s.page_y;
                if (s.page_y > maxY) maxY = s.page_y;
            }

            minX -= pageMarginPx; maxX += pageMarginPx;
            minY -= pageMarginPx; maxY += pageMarginPx;

            boxMinX = minX;
            boxMinY = minY;
            boxWidth = Mathf.Max(1f, maxX - minX);
            boxHeight = Mathf.Max(1f, maxY - minY);

            float areaW = Mathf.Max(1f, pageArea.rect.width);
            float areaH = Mathf.Max(1f, pageArea.rect.height);
            // "contain" 스케일: pageArea 안에 경로 전체가 잘리지 않고 들어가도록 가로/세로 중 더
            // 작은 배율을 쓴다.
            scale = Mathf.Min(areaW / boxWidth, areaH / boxHeight);
        }

        private void ComputeStrokeFlags()
        {
            var samples = pathData.samples;
            strokeStartFlags = new bool[samples.Length];
            strokeEndFlags = new bool[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                if (!samples[i].pen_down) continue;

                bool prevSameStroke = i > 0 && samples[i - 1].pen_down && samples[i - 1].stroke_id == samples[i].stroke_id;
                bool nextSameStroke = i < samples.Length - 1 && samples[i + 1].pen_down && samples[i + 1].stroke_id == samples[i].stroke_id;

                strokeStartFlags[i] = !prevSameStroke;
                strokeEndFlags[i] = !nextSameStroke;
            }
        }

        private Vector2 MapToLocal(float pageX, float pageY)
        {
            float mappedW = boxWidth * scale;
            float mappedH = boxHeight * scale;
            float localX = (pageX - boxMinX) * scale - mappedW / 2f;
            // writing_path.json의 page_y는 이미지 좌표계(위→아래로 증가)이므로 Unity UI의
            // y-up(RectTransform)로 뒤집는다.
            float localY = -((pageY - boxMinY) * scale - mappedH / 2f);
            return new Vector2(localX, localY);
        }

        private void ResetVisualState()
        {
            ClearChildren(inkLayer);
            ClearChildren(bleedLayer);

            if (finalMaskImage != null)
            {
                var c = finalMaskImage.color;
                c.a = 0f;
                finalMaskImage.color = c;
            }
            if (penGroup != null)
            {
                penGroup.alpha = 1f;
            }
            if (nibContact != null)
            {
                nibContact.gameObject.SetActive(false);
            }

            haveLastPos = false;
            lastAngleDeg = 0f;
            lastStampStrokeId = int.MinValue;
            lastPos = Vector2.zero;
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private IEnumerator WriteRoutine()
        {
            var samples = pathData.samples;
            float totalDuration = samples[samples.Length - 1].time;
            int idx = 0;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime * Mathf.Max(0.0001f, playbackSpeed);

                while (idx < samples.Length && samples[idx].time <= elapsed)
                {
                    ProcessSample(idx);
                    idx++;
                }

                UpdateFinalMaskCrossfade(totalDuration - elapsed);
                yield return null;
            }

            // 남은 샘플(마지막 프레임에서 한꺼번에 지나친 경우 포함)을 모두 반영해 정확히
            // 완성된 상태로 끝난다.
            while (idx < samples.Length)
            {
                ProcessSample(idx);
                idx++;
            }
            UpdateFinalMaskCrossfade(0f);

            if (penGroup != null)
            {
                float t = 0f;
                float startAlpha = penGroup.alpha;
                while (t < penExitFadeDuration)
                {
                    t += Time.deltaTime;
                    penGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / penExitFadeDuration);
                    yield return null;
                }
                penGroup.alpha = 0f;
            }

            routine = null;
        }

        private void UpdateFinalMaskCrossfade(float remaining)
        {
            if (finalMaskImage == null) return;

            float a = remaining <= finalMaskCrossfadeDuration
                ? 1f - Mathf.Clamp01(remaining / finalMaskCrossfadeDuration)
                : 0f;
            var c = finalMaskImage.color;
            c.a = a;
            finalMaskImage.color = c;
        }

        private void ProcessSample(int index)
        {
            var sample = pathData.samples[index];
            Vector2 local = MapToLocal(sample.page_x, sample.page_y);

            if (haveLastPos)
            {
                Vector2 delta = local - lastPos;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    float moveAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    // 펜대가 진행 방향의 반대쪽으로 눕는 자연스러운 자세.
                    lastAngleDeg = moveAngle + 180f - DefaultShaftAngleDeg;
                }
            }

            if (quillPen != null)
            {
                quillPen.anchoredPosition = local;
                quillPen.localRotation = Quaternion.Euler(0f, 0f, lastAngleDeg);
            }
            if (quillShadow != null)
            {
                quillShadow.anchoredPosition = local + shadowLocalOffset;
                quillShadow.localRotation = Quaternion.Euler(0f, 0f, lastAngleDeg);
            }
            if (quillShadowImage != null)
            {
                var c = quillShadowImage.color;
                c.a = sample.pen_down ? 0.35f : 0.2f;
                quillShadowImage.color = c;
            }
            if (nibContact != null)
            {
                nibContact.anchoredPosition = local;
                nibContact.gameObject.SetActive(sample.pen_down);
            }

            haveLastPos = true;
            lastPos = local;

            if (!sample.pen_down)
            {
                return;
            }

            if (strokeStartFlags[index])
            {
                SpawnStamp(bleedLayer, inkBleedMaskSprite, local, bleedSize, 0.55f);
                // 새 획의 첫 도장은 거리 검사 없이 무조건 찍는다.
                lastStampStrokeId = int.MinValue;
            }

            bool farEnough = lastStampStrokeId != sample.stroke_id
                || (local - lastStampPos).sqrMagnitude >= minStampDistance * minStampDistance;
            if (!farEnough)
            {
                return;
            }

            Sprite brush;
            float size;
            if (strokeEndFlags[index])
            {
                brush = inkBrushDry;
                size = stampSizeDry;
            }
            else if (sample.pressure >= pressureThresholdForMedium)
            {
                brush = inkBrushMedium;
                size = stampSizeMedium;
            }
            else
            {
                brush = inkBrushThin;
                size = stampSizeThin;
            }

            SpawnStamp(inkLayer, brush, local, size, 1f);
            lastStampPos = local;
            lastStampStrokeId = sample.stroke_id;
        }

        /// <summary>
        /// 잉크/번짐 도장 하나를 생성한다. RenderTexture나 커스텀 블릿 없이, 그냥 작은 UI
        /// Image를 지나간 자리에 남기는 방식으로 "누적"을 구현한다 — 문서 6절이 요구하는
        /// "이미 쓴 잉크가 유지되는가"를 가장 단순하게 만족시키는 방법이다. Image의 기본
        /// 머티리얼은 표준 알파 블렌드이므로 Additive 합성이 아니다(문서 5절 준수).
        /// </summary>
        private void SpawnStamp(RectTransform parent, Sprite sprite, Vector2 localPos, float size, float alpha)
        {
            if (parent == null || sprite == null) return;

            var go = new GameObject("InkStamp", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = localPos;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (alpha < 1f)
            {
                img.color = new Color(1f, 1f, 1f, alpha);
            }
        }
    }
}
