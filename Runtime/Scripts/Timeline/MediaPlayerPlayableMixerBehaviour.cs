using System;
using UnityEngine;
using UnityEngine.Playables;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// MediaPlayerPlayableTrack の唯一の制御点。区間内のクリップの時刻へ MediaPlayer をコマ単位でシークする
    /// </summary>
    public class MediaPlayerPlayableMixerBehaviour : PlayableBehaviour
    {
        // Timeline のフレーム境界の時刻は浮動小数で僅かに手前へずれるため、切り捨て前に足す
        private const double FrameEpsilon = 1e-6;

        private static Camera _dummyCamera;

        /// <summary>シーク後、新しいコマがテクスチャに届くまでメインスレッドを止めるか</summary>
        public bool WaitForFrame { get; set; }

        /// <summary>グラフ破棄時に CloseMedia するか</summary>
        public bool CloseOnDestroy { get; set; }

        private MediaPlayer _mediaPlayer;
        private int _activeInput = -1;
        private bool _activeAutoRewind;
        private bool _openRequested;
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
            }

            if (!mediaPlayer.gameObject.activeInHierarchy)
            {
                // 非アクティブの間に MediaPlayer 側の状態が変わり得るため、戻ったら必ずシークし直す
                ResetSeekState();
                return;
            }

            if (!EnsureOpened(mediaPlayer)) return;

            // 時刻の主導権は Timeline にある。MediaPlayer 自身の再生と競合させない
            if (mediaPlayer.Control.IsPlaying()) mediaPlayer.Control.Pause();

            SeekTo(mediaPlayer, playable.GetInput(input).GetTime());

#if UNITY_EDITOR
            if (!Application.isPlaying) mediaPlayer.EditorUpdate();
#endif
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            // Edit mode では Timeline ウィンドウが編集のたびにグラフを作り直すため、破棄に副作用を持たせない
            if (!Application.isPlaying) return;
            if (_mediaPlayer == null) return;

            if (CloseOnDestroy)
            {
                _mediaPlayer.CloseMedia();
            }
            else if (_activeInput >= 0 && _activeAutoRewind)
            {
                // クリップ区間内でグラフが破棄される（PlayableDirector.Stop・終端到達）のはクリップを抜けたのと同じ扱い
                _mediaPlayer.Rewind(true);
            }
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
                mediaPlayer.OpenMedia(false);
            }
            return false;
        }

        private void SeekTo(MediaPlayer mediaPlayer, double time)
        {
            var control = mediaPlayer.Control;
            var frameRate = mediaPlayer.Info.GetVideoFrameRate();

            if (frameRate <= 0f)
            {
                // コマ番号が出せない（メタデータ未着・フレームレート不明）間は時刻でシークする
                if (!control.CanPlay() || time == _seekedTime) return;
                _seekedTime = time;
                control.Seek(time);
                return;
            }

            var frame = (int)Math.Floor(Math.Max(0.0, time) * frameRate + FrameEpsilon);
            frame = Mathf.Min(frame, mediaPlayer.Info.GetMaxFrameNumber());
            if (frame == _seekedFrame) return;
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
