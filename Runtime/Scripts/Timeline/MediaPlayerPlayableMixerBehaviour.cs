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
    /// Play mode では、表示するコマは常に <see cref="MediaPlayerFrameBuffer"/> から Timeline の時刻で選ぶ。
    /// 再生中（Buffered）: 映像を Timeline より先行して再生し、届いたコマを貯める。音も鳴る。
    /// 停止中・Evaluate・Seek モード: 映像を止め、Timeline の時刻のコマへシークして貯める。
    /// Edit mode: 映像を Timeline の時刻へシークするだけで、表示は MediaPlayer のテクスチャのまま。
    /// </remarks>
    public class MediaPlayerPlayableMixerBehaviour : PlayableBehaviour
    {
        // Timeline のフレーム境界の時刻は浮動小数で僅かに手前へずれるため、切り捨て前に足す
        private const double FrameEpsilon = 1e-6;
        // 先行量からこのコマ数より外れたら先行し直す。ゲームフレームの刻みによる揺れより大きく取る
        private const int ResyncFrames = 2;

        private static Camera _dummyCamera;

        public MediaPlayerTimelineSyncMode SyncMode { get; set; }

        /// <summary>Buffered で映像を Timeline より先行させるコマ数。音はこの分だけ絵より先に出る</summary>
        public int LeadFrames { get; set; }

        /// <summary>Play mode で貯めるコマ数</summary>
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
        private int _requestedFrame = -1;
        private double _requestedTime = -1.0;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var mediaPlayer = playerData as MediaPlayer;
            if (!ReferenceEquals(mediaPlayer, _mediaPlayer))
            {
                // バインド先が変わったら、前の MediaPlayer から手を引く
                Detach();
                _mediaPlayer = mediaPlayer;
            }
            if (mediaPlayer == null) return;

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
                _requestedFrame = -1;
                _requestedTime = -1.0;
            }

            if (!mediaPlayer.gameObject.activeInHierarchy)
            {
                // 非アクティブの間に MediaPlayer 側の状態が変わり得るため、戻ったら必ずシークし直す
                _requestedFrame = -1;
                _requestedTime = -1.0;
                return;
            }

            if (!EnsureOpened(mediaPlayer)) return;

            var control = mediaPlayer.Control;
            var frameRate = mediaPlayer.Info.GetVideoFrameRate();
            var time = playable.GetInput(input).GetTime();

            if (!Application.isPlaying)
            {
                PreviewInEditMode(mediaPlayer, time, frameRate);
                return;
            }

            // フレームレートが出るまではコマ番号で扱えない
            if (frameRate <= 0f || !control.CanPlay()) return;

            _buffer ??= new MediaPlayerFrameBuffer(mediaPlayer, Mathf.Max(BufferSize, LeadFrames + 2));

            var targetFrame = Mathf.Min(ToFrame(time, frameRate), mediaPlayer.Info.GetMaxFrameNumber());
            var playing = SyncMode == MediaPlayerTimelineSyncMode.Buffered
                && info.evaluationType == FrameData.EvaluationType.Playback
                && playable.GetGraph().IsPlaying();

            if (playing)
            {
                PlayAhead(mediaPlayer, time, targetFrame, frameRate, info.deltaTime);
            }
            else
            {
                if (control.IsPlaying()) control.Pause();
                FetchFrame(mediaPlayer, targetFrame, frameRate);
            }

            _buffer.Capture(frameRate);
            _buffer.Present(targetFrame, ResyncFrames);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // PlayableDirector.Pause() では ProcessFrame が来ないので、ここで音と映像を止める。
            // 表示は出力テクスチャに残った Timeline の時刻のコマのまま
            PauseMedia();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            // Edit mode では Timeline ウィンドウが編集のたびにグラフを作り直すため、破棄に副作用を持たせない
            if (Application.isPlaying && _mediaPlayer != null && _mediaPlayer.Control != null)
            {
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
                    // 表示を MediaPlayer のテクスチャへ戻すので、先行していた映像を出力中のコマへ戻しておく
                    PauseMedia();
                    if (_buffer != null && _buffer.PresentedFrame >= 0)
                    {
                        SeekAndWait(_mediaPlayer, (int)_buffer.PresentedFrame);
                    }
                }
            }

            ReleaseBuffer();
        }

        private void PlayAhead(MediaPlayer mediaPlayer, double time, int targetFrame, float frameRate, float deltaTime)
        {
            var control = mediaPlayer.Control;
            var maxFrame = mediaPlayer.Info.GetMaxFrameNumber();

            // 映像側のループは Timeline の時刻と食い違うので使わない。周回は Timeline で表す
            if (control.IsLooping()) control.SetLooping(false);

            if (control.IsPlaying())
            {
                // Timeline はスケール済みのゲーム時間、映像は実時間で進むので、ずれは溜まる前提で先行し直す
                var desiredTime = time + LeadFrames / (double)frameRate;
                var threshold = Math.Max(ResyncFrames / (double)frameRate, 2.0 * deltaTime);
                if (Math.Abs(control.GetCurrentTime() - desiredTime) <= threshold) return;
                control.Pause();
            }

            // 先行分が末尾に届く区間では再生を始めても即座に終わるので、止めたままシークで合わせる
            if (targetFrame + LeadFrames >= maxFrame)
            {
                FetchFrame(mediaPlayer, targetFrame, frameRate);
                return;
            }

            if (Prime(mediaPlayer, targetFrame, frameRate))
            {
                control.Play();
                _requestedFrame = -1;
            }
        }

        /// <summary>
        /// 今のコマから先行分までを貯め、映像を先行分の位置に置く。揃ったら true。
        /// 揃うまでは止めたまま、ゲームフレームをまたいで続きを行う。始めた直後から Timeline の時刻のコマを出せるようにする
        /// </summary>
        private bool Prime(MediaPlayer mediaPlayer, int targetFrame, float frameRate)
        {
            var lastFrame = targetFrame + LeadFrames;
            for (var frame = targetFrame; frame <= lastFrame; frame++)
            {
                FetchFrame(mediaPlayer, frame, frameRate);
                if (!_buffer.Contains(frame)) return false;
            }

            // 貯めてあったコマはシークを省くので、映像の位置は別に合わせる
            if (ProducerFrame(mediaPlayer, frameRate) == lastFrame) return true;
            if (_requestedFrame != lastFrame)
            {
                _requestedFrame = lastFrame;
                SeekAndWait(mediaPlayer, lastFrame);
            }
            return ProducerFrame(mediaPlayer, frameRate) == lastFrame;
        }

        private static long ProducerFrame(MediaPlayer mediaPlayer, float frameRate)
        {
            var timeStamp = mediaPlayer.TextureProducer.GetTextureTimeStamp();
            return timeStamp == long.MinValue ? -1 : MediaPlayerFrameBuffer.ToFrame(timeStamp, frameRate);
        }

        /// <summary>
        /// 指定のコマを貯める。WaitForFrame なら届くまで待つ。待てない環境では、届いたゲームフレームで貯まる
        /// </summary>
        private void FetchFrame(MediaPlayer mediaPlayer, int frame, float frameRate)
        {
            _buffer.Capture(frameRate);
            if (_buffer.Contains(frame)) return;

            // シークを出したコマは、届くまで出し直さない
            if (frame == _requestedFrame) return;
            _requestedFrame = frame;

            SeekAndWait(mediaPlayer, frame);
            _buffer.Capture(frameRate);
        }

        private void SeekAndWait(MediaPlayer mediaPlayer, int frame)
        {
            var control = mediaPlayer.Control;
            var previousFrameCount = mediaPlayer.TextureProducer.GetTextureFrameCount();
            control.SeekToFrame(frame);

            // シークは非同期で、待たないと新しいコマは次のゲームフレーム以降に届く
            if (WaitForFrame)
            {
                control.WaitForNextFrame(DummyCamera, previousFrameCount);
            }
        }

        private void PreviewInEditMode(MediaPlayer mediaPlayer, double time, float frameRate)
        {
            var control = mediaPlayer.Control;
            if (control.IsPlaying()) control.Pause();

            if (frameRate <= 0f)
            {
                // コマ番号が出せない（メタデータ未着・フレームレート不明）間は時刻でシークする
                if (control.CanPlay() && time != _requestedTime)
                {
                    _requestedTime = time;
                    control.Seek(time);
                }
            }
            else if (control.CanPlay())
            {
                var frame = Mathf.Min(ToFrame(time, frameRate), mediaPlayer.Info.GetMaxFrameNumber());
                if (frame != _requestedFrame)
                {
                    _requestedFrame = frame;
                    control.SeekToFrame(frame);
                }
            }

#if UNITY_EDITOR
            mediaPlayer.EditorUpdate();
#endif
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
            _requestedFrame = -1;
            _requestedTime = -1.0;
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
                _requestedFrame = -1;
                _requestedTime = -1.0;
                // 貯めたコマの PTS は前に開いていた映像のもの
                _buffer?.Clear();

                // OpenMedia は引数を AutoStart（シリアライズされる設定）へ書き込むため、自動再生だけ止めて設定は戻す
                var autoStart = mediaPlayer.AutoStart;
                mediaPlayer.OpenMedia(false);
                mediaPlayer.AutoStart = autoStart;
            }
            return false;
        }

        private void PauseMedia()
        {
            if (_mediaPlayer == null || _mediaPlayer.Control == null) return;
            if (_mediaPlayer.Control.IsPlaying()) _mediaPlayer.Control.Pause();
        }

        private void Detach()
        {
            PauseMedia();
            ReleaseBuffer();
            _activeInput = -1;
            _openRequested = false;
            _requestedFrame = -1;
            _requestedTime = -1.0;
        }

        private void ReleaseBuffer()
        {
            // MediaPlayer が先に破棄されていても、バッファの RenderTexture は解放する
            _buffer?.Dispose();
            _buffer = null;
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

        private static int ToFrame(double time, float frameRate)
        {
            return (int)Math.Floor(Math.Max(0.0, time) * frameRate + FrameEpsilon);
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
