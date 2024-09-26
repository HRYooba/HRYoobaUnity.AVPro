using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.Library
{
    [Serializable]
    public class MediaPlayerPlayableBehaviour : PlayableBehaviour
    {
        private MediaPlayer _mediaPlayer = null;
        private PlayableDirector _director = null;
        private bool _isTimeSync = true;
        private bool _isAutoPause = true;
        private bool _isAutoRewind = true;
        private bool _isLoop = false;
        private double _startTime = 0.0;
        private double _seekThreshold = 0.1;
        private float _playbackRate = 1.0f;

        private bool _canInit = false;
        private bool _canSeek = true;

        public void SetMediaPlayer(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;

            var isMp4 = _mediaPlayer.MediaPath.Path.EndsWith(".mp4");

            // DirectShow&mp4の場合は強制シークするとEditorが落ちる
            // DirectShow&hap(mov), MediaFoundation&mp4は動作確認済み
            _canSeek = !(isMp4 && _mediaPlayer.PlatformOptionsWindows.videoApi == RenderHeads.Media.AVProVideo.Windows.VideoApi.DirectShow);
            if (!_canSeek) Debug.LogWarning("[MediaPlayerPlayable] DirectShow&mp4の場合はTimelineのシークするとEditorが落ちるのでTimelineのシークと再生の同期は無効になります");
        }

        public void SetPlayableDirector(PlayableDirector director)
        {
            _director = director;
        }

        public void SetTimeSync(bool isTimeSync)
        {
            _isTimeSync = isTimeSync;
        }

        public void SetAutoPause(bool isAutoPause)
        {
            _isAutoPause = isAutoPause;
        }

        public void SetAutoRewind(bool isAutoRewind)
        {
            _isAutoRewind = isAutoRewind;
        }

        public void SetLoop(bool isLoop)
        {
            _isLoop = isLoop;
        }

        public void SetStartTime(double startTime)
        {
            _startTime = startTime;
        }

        public void SetSeekThreshold(double threshold)
        {
            _seekThreshold = threshold;
        }

        public void SetPlaybackRate(float playbackRate)
        {
            _playbackRate = playbackRate;
        }

        // #if UNITY_EDITOR
        //         public override void OnPlayableDestroy(Playable playable)
        //         {
        //             if (_mediaPlayer == null) return;
        //             if (!Application.isPlaying) _mediaPlayer.ForceDispose();
        //         }
        // #endif

        public override void OnGraphStart(Playable playable)
        {
            if (_mediaPlayer == null) return;

            if (!_mediaPlayer.MediaOpened)
            {
                _mediaPlayer.OpenMedia(false);
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _canInit = true;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_mediaPlayer == null) return;
            if (_isAutoPause) _mediaPlayer.Pause();
            if (_isAutoRewind) _mediaPlayer.Rewind(true);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (_mediaPlayer == null) return;

            // MediaPlayerがOpenされていない場合はOpenする
            if (!_mediaPlayer.MediaOpened)
            {
                _mediaPlayer.OpenMedia(false);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) _mediaPlayer.EditorUpdate();
#endif

            if (_mediaPlayer.Control == null) return;
            if (_mediaPlayer.Info == null) return;

            // 初回のみ設定を行う
            if (_canInit)
            {
                _mediaPlayer.Control.SetPlaybackRate(_playbackRate);
                _mediaPlayer.Control.SetLooping(_isLoop);
                _mediaPlayer.Control.Seek(_startTime);
                _canInit = false;
            }

            if (_mediaPlayer.Control.IsPlaying())
            {
                try
                {
                    // PlayableDirectorの再生停止とMediaPlayerの再生停止の同期
                    if (!_director.playableGraph.IsPlaying())
                    {
                        _mediaPlayer.Control.Pause();
                        return;
                    }
                }
                catch (NullReferenceException)
                {
                    // ControlTrackにした際に　NullReferenceException: The PlayableGraph is null.の対策
                    _mediaPlayer.Control.Pause();
                    return;
                }
            }
            else
            {
                // 再生中でなければ再生する
                _mediaPlayer.Control.Play();
            }

            // Time同期が無効なら処理を終了
            if (!_isTimeSync) return;
            if (!_canSeek) return;

            // シークと再生の同期
            var time = playable.GetTime() * _mediaPlayer.Control.GetPlaybackRate() + _startTime;
            if (_isLoop)
            {
                time %= _mediaPlayer.Info.GetDuration();
            }
            else
            {
                // clamp処理
                if (time <= 0.0)
                {
                    time = 0.0;
                }
                if (time >= _mediaPlayer.Info.GetDuration())
                {
                    time = _mediaPlayer.Info.GetDuration();
                }
            }

            var delta = time - _mediaPlayer.Control.GetCurrentTime();
            if (delta > _seekThreshold || delta < -_seekThreshold)
            {
                _mediaPlayer.Control.Seek(time);
            }
        }
    }
}