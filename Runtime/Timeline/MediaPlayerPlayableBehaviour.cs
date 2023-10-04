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
        private bool _isAutoRewind = false;
        private bool _isLoop = false;
        private double _startTime = 0.0;
        private double _seekThreshold = 0.1;
        private float _playbackRate = 1.0f;

        public void SetMediaPlayer(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }

        public void SetPlayableDirector(PlayableDirector director)
        {
            _director = director;
        }

        public void SetTimeSync(bool isTimeSync)
        {
            _isTimeSync = isTimeSync;
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

        public override void OnGraphStop(Playable playable)
        {
            if (_mediaPlayer == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) _mediaPlayer.ForceDispose();
#endif
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_mediaPlayer == null) return;

            if (_isAutoRewind) _mediaPlayer.Rewind(true);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_mediaPlayer == null) return;

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

            // 再生中でなければ再生する
            if (!_mediaPlayer.Control.IsPlaying())
            {
                _mediaPlayer.Control.Play();
                _mediaPlayer.Control.SetLooping(_isLoop);
                _mediaPlayer.Control.Seek(_startTime);
            }

            // 再生速度を設定
            _mediaPlayer.Control.SetPlaybackRate(_playbackRate);

            // Time同期が無効なら処理を終了
            if (!_isTimeSync) return;

            // シークと再生の同期
            var time = playable.GetTime() * _mediaPlayer.Control.GetPlaybackRate() + _startTime;
            if (_isLoop)
            {
                time %= _mediaPlayer.Info.GetDuration();
            }
            else
            {
                time = Mathf.Clamp((float)time, 0f, (float)_mediaPlayer.Info.GetDuration());
            }

            var delta = time - _mediaPlayer.Control.GetCurrentTime();
            if (delta > _seekThreshold || delta < -_seekThreshold)
            {
                _mediaPlayer.Control.Seek(time);
            }

            // PlayableDirectorの再生停止とMediaPlayerの再生停止の同期
            if (!_director.playableGraph.IsPlaying())
            {
                if (_mediaPlayer.Control.IsPlaying())
                {
                    _mediaPlayer.Control.Stop();
                }
            }
        }
    }
}