using UnityEngine;

namespace Effects
{
    /// <summary>
    /// BGMおよび効果音（SE）の再生・管理を行うスクリプト。
    /// GameManagerのイベント（状態変化・被弾）に応じて自動でBGM切り替えやSE再生を行います。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameManager gameManager;

        [Header("Audio Sources")]
        [Tooltip("BGM再生用のAudioSource（Loopを有効化してください）")]
        [SerializeField] private AudioSource bgmSource;

        [Tooltip("効果音(SE)再生用のAudioSource")]
        [SerializeField] private AudioSource seSource;

        [Header("BGM Clips")]
        [Tooltip("Ready（待機中・キャリブレーション中）のBGM")]
        [SerializeField] private AudioClip readyBgm;

        [Tooltip("Playing（プレイ中）のBGM")]
        [SerializeField] private AudioClip playingBgm;

        [Tooltip("Goal（ゴール時）のBGM")]
        [SerializeField] private AudioClip goalBgm;

        [Header("SE Clips")]
        [Tooltip("隕石等に衝突した際のSE")]
        [SerializeField] private AudioClip hitSe;

        [Tooltip("ゴール到達時のファンファーレ/SE")]
        [SerializeField] private AudioClip goalSe;

        [Tooltip("ゲームスタート時の決定/開始SE")]
        [SerializeField] private AudioClip startSe;

        [Header("音量設定 (0.0 〜 1.0)")]
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float seVolume = 0.8f;

        public float BgmVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                if (bgmSource != null) bgmSource.volume = bgmVolume;
            }
        }

        public float SeVolume
        {
            get => seVolume;
            set => seVolume = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            // AudioSource の初期設定（BGM用はループを有効に設定）
            if (bgmSource != null)
            {
                bgmSource.loop = true;
                bgmSource.volume = bgmVolume;
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged += HandleStateChanged;
                gameManager.OnHit += HandleHit;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleStateChanged;
                gameManager.OnHit -= HandleHit;
            }
        }

        private void Start()
        {
            if (gameManager == null)
            {
                Debug.LogError("[AudioManager] GameManager が設定されていません。Inspector でつないでください。");
                return;
            }

            // 初期状態のBGM再生
            PlayBgmForState(gameManager.State);
        }

        /// <summary>
        /// GameManager の状態変化イベント（OnStateChanged）のハンドラ
        /// </summary>
        private void HandleStateChanged(GameState newState)
        {
            PlayBgmForState(newState);

            // 状態に応じた単発SEの再生
            switch (newState)
            {
                case GameState.Playing:
                    PlaySe(startSe);
                    break;
                case GameState.Goal:
                    PlaySe(goalSe);
                    break;
            }
        }

        /// <summary>
        /// GameManager の被弾イベント（OnHit）のハンドラ
        /// </summary>
        private void HandleHit()
        {
            PlaySe(hitSe);
        }

        /// <summary>
        /// ゲーム状態（Ready / Playing / Goal）に応じたBGMのループ再生
        /// </summary>
        private void PlayBgmForState(GameState state)
        {
            if (bgmSource == null) return;

            AudioClip targetClip = state switch
            {
                GameState.Ready => readyBgm,
                GameState.Playing => playingBgm,
                GameState.Goal => goalBgm,
                _ => null
            };

            if (targetClip == null)
            {
                bgmSource.Stop();
                return;
            }

            // 既に同じBGMが再生中の場合は再再生しない
            if (bgmSource.clip == targetClip && bgmSource.isPlaying) return;

            bgmSource.clip = targetClip;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        /// <summary>
        /// ワンショット効果音(SE)の再生
        /// </summary>
        public void PlaySe(AudioClip clip)
        {
            if (clip == null || seSource == null) return;

            seSource.PlayOneShot(clip, seVolume);
        }

        /// <summary>
        /// 外部やUIからBGM音量を変更する場合
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            BgmVolume = volume;
        }

        /// <summary>
        /// 外部やUIからSE音量を変更する場合
        /// </summary>
        public void SetSeVolume(float volume)
        {
            SeVolume = volume;
        }
    }
}