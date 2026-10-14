using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Effects
{
    /// <summary>
    /// ワープゲート突入時の演出（加速、発光/フラッシュ、画面フェード）を制御し、
    /// 演出完了後に結果表示（ResultUI）を綺麗に見せるための演出管理スクリプト。
    /// </summary>
    public class WarpSequence : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private WarpGate warpGate;
        [SerializeField] private ResultUI resultUI;

        [Header("演出用UI / 視覚効果")]
        [Tooltip("フェード・ホワイトアウト用の全画面UI Image（Whiteを推奨）")]
        [SerializeField] private Image fadeImage;

        [Tooltip("ワープ突入時のカメラ（FOVアニメーション用）")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("自機（宇宙船）のTransform（突入時の前進アニメーション用）")]
        [SerializeField] private Transform playerShip;

        [Header("演出設定")]
        [Tooltip("突入時の加速・カメラ演出時間（秒）")]
        [SerializeField] private float accelerationDuration = 1.0f;

        [Tooltip("ホワイトアウト（発光・フラッシュ）時間（秒）")]
        [SerializeField] private float flashDuration = 0.4f;

        [Tooltip("ホワイトアウトから画面が明けて結果画面を見せるまでの時間（秒）")]
        [SerializeField] private float fadeOutDuration = 0.8f;

        [Tooltip("ワープ突入時の目標カメラFOV（画角拡大でスピード感を表現）")]
        [SerializeField] private float warpFov = 90f;

        [Tooltip("突入時に自機を前進させる追加距離")]
        [SerializeField] private float shipForwardDistance = 30f;

        private float originalFov = 60f;
        private Vector3 originalShipLocalPos;
        private Coroutine sequenceCoroutine;

        /// <summary>
        /// ワープ演出が最後まで終わった時に通知する（ResultUI が結果画面を出すために使う）
        /// </summary>
        public event System.Action OnFinished;

        private void Awake()
        {
            if (mainCamera != null)
            {
                originalFov = mainCamera.fieldOfView;
            }

            if (playerShip != null)
            {
                originalShipLocalPos = playerShip.localPosition;
            }

            // フェード用Imageの初期化（非表示・透明）
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = 0f;
                fadeImage.color = color;
                fadeImage.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Goal)
            {
                StartSequence();
            }
            else if (state == GameState.Ready)
            {
                ResetSequence();
            }
        }

        /// <summary>
        /// ワープ演出の開始
        /// </summary>
        public void StartSequence()
        {
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
            }
            sequenceCoroutine = StartCoroutine(PlayWarpSequence());
        }

        /// <summary>
        /// ワープ演出コルーチン（加速 → 発光・ホワイトアウト → フェード明けて結果表示）
        /// </summary>
        private IEnumerator PlayWarpSequence()
        {
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
            }

            // -------------------------------------------------------------
            // 1. 加速・カメラFOV拡大・自機突入アニメーション
            // -------------------------------------------------------------
            float elapsed = 0f;
            Vector3 startShipPos = playerShip != null ? playerShip.localPosition : Vector3.zero;
            Vector3 targetShipPos = startShipPos + Vector3.forward * shipForwardDistance;

            while (elapsed < accelerationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / accelerationDuration);
                float easeIn = t * t; // 二乗で加速感を表現

                if (mainCamera != null)
                {
                    mainCamera.fieldOfView = Mathf.Lerp(originalFov, warpFov, easeIn);
                }

                if (playerShip != null)
                {
                    playerShip.localPosition = Vector3.Lerp(startShipPos, targetShipPos, easeIn);
                }

                yield return null;
            }

            // -------------------------------------------------------------
            // 2. 発光・ホワイトアウト（画面フラッシュ）
            // -------------------------------------------------------------
            elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flashDuration);

                if (fadeImage != null)
                {
                    Color color = fadeImage.color;
                    color.a = t; // 透明 → 白へ
                    fadeImage.color = color;
                }

                yield return null;
            }

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 1f;
                fadeImage.color = c;
            }

            // -------------------------------------------------------------
            // 3. フェードアウト（画面が明けて ResultUI をスムーズに表示）
            // -------------------------------------------------------------
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);

                if (fadeImage != null)
                {
                    Color color = fadeImage.color;
                    color.a = 1f - t; // 白 → 透明へ
                    fadeImage.color = color;
                }

                yield return null;
            }

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                fadeImage.gameObject.SetActive(false);
            }

            sequenceCoroutine = null;

            // 演出の完了を通知（途中で ResetSequence された場合はここまで来ないので通知されない）
            OnFinished?.Invoke();
        }

        /// <summary>
        /// ゲームが Ready 状態に戻った際のリセット処理
        /// </summary>
        private void ResetSequence()
        {
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            if (mainCamera != null)
            {
                mainCamera.fieldOfView = originalFov;
            }

            if (playerShip != null)
            {
                playerShip.localPosition = originalShipLocalPos;
            }

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                fadeImage.gameObject.SetActive(false);
            }
        }
    }
}