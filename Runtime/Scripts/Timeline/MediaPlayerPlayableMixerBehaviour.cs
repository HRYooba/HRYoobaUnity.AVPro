using System;
using UnityEngine;
using UnityEngine.Playables;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// MediaPlayerPlayableTrack の唯一の制御点。時刻の主は Timeline で、MediaPlayer をそれに従わせる
    /// </summary>
    /// <remarks>
    /// 再生中（Buffered）: 映像を Timeline より先行して再生させ、<see cref="MediaPlayerFrameBuffer"/> から Timeline の時刻のコマを出す。音も鳴る。
    /// 停止中・スクラブ・Edit mode・Seek モード: 映像を止め、Timeline の時刻のコマへシークする。
    /// </remarks>
    public class MediaPlayerPlayableMixerBehaviour : PlayableBehaviour
    {
        // Timeline のフレーム境界の時刻は浮動小数で僅かに手前へずれるため、切り捨て前に足す
        private const double FrameEpsilon = 1e-6;
        // 先行量からこのコマ数より外れたらシークで戻す。ゲームフレームの刻みによる揺れより大きく取る
        private const int ResyncFrames = 2;

        private static Camera _dummyCamera;

        public MediaPlayerTimelineSyncMode SyncMode { get; set; }

        /// <summary>Buffered で映像を Timeline より先行させるコマ数。音はこの分だけ絵より先に出る</summary>
        public int LeadFrames { get; set; }

        /// <summary>Buffered で貯めるコマ数</summary>
        public int BufferSize { get; set; }

        /// <summary>シーク後、新しいコマがテクスチャに届くまでメインスレッドを止めるか</summary>
        public bool WaitForFrame { get; set; }

        /// <summary>グラフ破棄時に CloseMedia するか</summary>
        public bool CloseOnDestroy { get; set; }

        private MediaPlayer _mediaPlayer;
        private MediaPlayerFrameBuffer _buffer;
        private int _activeInput = -1;
        private bool _activeAutoRewind;
        private bool _openRequested;
        private bool _buffered;
        private int _seekedFrame = -1;
        private double _seekedTime = -1.0;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var mediaPlayer = playerData as MediaPlayer;
            if (mediaPlayer == null) return;
            _mediaPlayer = mediaPlayer;

            var input = FindActiveInput(playable);
            if (input < 0)
            {
                if (_activeInput >= 0) ExitClip(mediaPlayer);
                return;
            }

            if (input != _activeInput)
            {
                _activeInput = input;
                _activeAutoRewind = ((ScriptPlayable<MediaPlayerPlayableBehaviour>)playable.GetInput(input)).GetBehaviour().AutoRewind;
                ResetSeekState();
                _buffer?.Clear();
            }

            if (!mediaPlayer.gameObject.activeInHierarchy)
            {
                // 非アクティブの間に MediaPlayer 側の状態が変わり得るため、戻ったら必ずシークし直す
                ResetSeekState();
                return;
            }

            if (!EnsureOpened(mediaPlayer)) return;

            var control = mediaPlayer.Control;
            var frameRate = mediaPlayer.Info.GetVideoFrameRate();
            var time = playable.GetInput(input).GetTime();

            var buffered = CanBuffer(playable, info, mediaPlayer, frameRate);
            if (buffered != _buffered)
            {
                _buffered = buffered;
                ResetSeekState();
                _buffer?.Clear();
            }

            if (buffered)
            {
                PlayBuffered(mediaPlayer, time, frameRate, info.deltaTime);
            }
            else
            {
                if (control.IsPlaying()) control.Pause();
                if (_buffer != null) _buffer.IsPresenting = false;
                SeekTo(mediaPlayer, time, frameRate);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) mediaPlayer.EditorUpdate();
#endif
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // PlayableDirector.Pause() では ProcessFrame が来ないので、ここで音と映像を止める。
            // 表示は出力テクスチャに残った Timeline の時刻のコマのまま
            PauseMedia();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_mediaPlayer == null) return;

            MediaPlayerFrameBuffer.Release(_mediaPlayer);
            _buffer = null;

            // Edit mode では Timeline ウィンドウが編集のたびにグラフを作り直すため、破棄に副作用を持たせない
            if (!Application.isPlaying) return;

            if (CloseOnDestroy)
            {
                _mediaPlayer.CloseMedia();
            }
            else if (_activeInput >= 0 && _activeAutoRewind)
            {
                // クリップ区間内でグラフが破棄される（PlayableDirector.Stop・終端到達）のはクリップを抜けたのと同じ扱い
                _mediaPlayer.Rewind(true);
            }
            else
            {
                PauseMedia();
            }
        }

        private bool CanBuffer(Playable playable, FrameData info, MediaPlayer mediaPlayer, float frameRate)
        {
            if (SyncMode != MediaPlayerTimelineSyncMode.Buffered) return false;
            if (!Application.isPlaying) return false;
            if (info.evaluationType != FrameData.EvaluationType.Playback) return false;
            if (!playable.GetGraph().IsPlaying()) return false;
            if (frameRate <= 0f) return false;

            // 出力テクスチャは 1 枚だけを貯める。YCbCr など複数枚の映像はシークで合わせる
            var producer = mediaPlayer.TextureProducer;
            return producer != null && producer.GetTextureCount() == 1;
        }

        private void PlayBuffered(MediaPlayer mediaPlayer, double time, float frameRate, float deltaTime)
        {
            var control = mediaPlayer.Control;
            if (!control.CanPlay()) return;

            _buffer ??= MediaPlayerFrameBuffer.Acquire(mediaPlayer, Mathf.Max(BufferSize, LeadFrames + 2));

            var desiredFrame = ToFrame(time, frameRate) + LeadFrames;
            if (!control.IsPlaying())
            {
                if (!control.IsFinished() || desiredFrame < mediaPlayer.Info.GetMaxFrameNumber())
                {
                    control.SeekToFrame(desiredFrame);
                    control.Play();
                    _buffer.Clear();
                }
            }
            else
            {
                // Timeline はスケール済みのゲーム時間、映像は実時間で進むので、ずれは溜まる前提でシークで戻す
                var desiredTime = time + LeadFrames / (double)frameRate;
                var threshold = Math.Max(ResyncFrames / (double)frameRate, 2.0 * deltaTime);
                if (Math.Abs(control.GetCurrentTime() - desiredTime) > threshold)
                {
                    control.SeekToFrame(desiredFrame);
                    _buffer.Clear();
                }
            }

            _buffer.Capture();
            _buffer.Present(time, frameRate);
        }

        private void PauseMedia()
        {
            if (_mediaPlayer == null || _mediaPlayer.Control == null) return;
            if (_mediaPlayer.Control.IsPlaying()) _mediaPlayer.Control.Pause();
        }

        private static int FindActiveInput(Playable playable)
        {
            var result = -1;
            var maxWeight = 0f;
            var count = playable.GetInputCount();
            for (var i = 0; i < count; i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight > maxWeight)
                {
                    maxWeight = weight;
                    result = i;
                }
            }
            return result;
        }

        private void ExitClip(MediaPlayer mediaPlayer)
        {
            if (_activeAutoRewind && mediaPlayer.Control != null)
            {
                mediaPlayer.Rewind(true);
                // 先頭へ戻したコマを見せる
                if (_buffer != null) _buffer.IsPresenting = false;
            }
            else
            {
                PauseMedia();
            }
            _activeInput = -1;
            ResetSeekState();
        }

        private bool EnsureOpened(MediaPlayer mediaPlayer)
        {
            // MediaOpened は「開く要求を出した」で true になり、読み込み完了を意味しない。
            // ドメインリロード後は MediaOpened だけが残り Control が null になる
            if (mediaPlayer.Control != null && mediaPlayer.MediaOpened) return true;

            // 読み込みに失敗しても MediaOpened は true のままなので、要求はグラフごとに 1 回で足りる
            if (!_openRequested)
            {
                _openRequested = true;
                ResetSeekState();

                // OpenMedia は引数を AutoStart（シリアライズされる設定）へ書き込むため、自動再生だけ止めて設定は戻す
                var autoStart = mediaPlayer.AutoStart;
                mediaPlayer.OpenMedia(false);
                mediaPlayer.AutoStart = autoStart;
            }
            return false;
        }

        private void SeekTo(MediaPlayer mediaPlayer, double time, float frameRate)
        {
            var control = mediaPlayer.Control;

            if (frameRate <= 0f)
            {
                // コマ番号が出せない（メタデータ未着・フレームレート不明）間は時刻でシークする
                if (!control.CanPlay() || time == _seekedTime) return;
                _seekedTime = time;
                control.Seek(time);
                return;
            }

            var frame = Mathf.Min(ToFrame(time, frameRate), mediaPlayer.Info.GetMaxFrameNumber());
            if (frame == _seekedFrame) return;

            // デコーダの準備前は新しいコマが届かず待ちが空回りするので、準備できるまで毎フレーム試す
            if (!control.CanPlay()) return;
            _seekedFrame = frame;

            // 表示中のコマへのシークでは新しいコマが届かず、待ちがタイムアウトまで続く
            if (Helper.ConvertTimeSecondsToFrame(control.GetCurrentTime(), frameRate) == frame) return;

            var previousFrameCount = mediaPlayer.TextureProducer.GetTextureFrameCount();
            control.SeekToFrame(frame);

            // シークは非同期で、待たないと新しいコマは次のゲームフレームの描画に回る
            if (WaitForFrame && Application.isPlaying)
            {
                control.WaitForNextFrame(DummyCamera, previousFrameCount);
            }
        }

        private static int ToFrame(double time, float frameRate)
        {
            return (int)Math.Floor(Math.Max(0.0, time) * frameRate + FrameEpsilon);
        }

        private void ResetSeekState()
        {
            _seekedFrame = -1;
            _seekedTime = -1.0;
        }

        /// <summary>
        /// WaitForNextFrame がレンダースレッドを回すために Render() を呼ぶ、何も描かないカメラ
        /// </summary>
        private static Camera DummyCamera
        {
            get
            {
                if (_dummyCamera == null)
                {
                    var go = new GameObject("HRYooba.AVPro Frame Sync Camera") { hideFlags = HideFlags.HideAndDontSave };
                    go.SetActive(false);
                    UnityEngine.Object.DontDestroyOnLoad(go);

                    _dummyCamera = go.AddComponent<Camera>();
                    _dummyCamera.cullingMask = 0;
                    _dummyCamera.clearFlags = CameraClearFlags.Nothing;
                    _dummyCamera.enabled = false;
                }
                return _dummyCamera;
            }
        }
    }
}
