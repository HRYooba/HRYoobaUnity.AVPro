using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.Library
{
    [Serializable]
    public class MediaPlayerPlayableClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private bool _isTimeSync = true;
        [SerializeField, Range(0.0001f, 1f)] private double _seekThreshold = 0.1;
        [SerializeField] private float _playbackRate = 1.0f;

        private MediaPlayer _mediaPlayer = null;
        private PlayableDirector _director = null;

        public ClipCaps clipCaps => ClipCaps.None;

        public void SetMediaPlayer(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
        }

        public void SetPlayableDirector(PlayableDirector director)
        {
            _director = director;
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MediaPlayerPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.SetMediaPlayer(_mediaPlayer);
            behaviour.SetPlayableDirector(_director);
            behaviour.SetTimeSync(_isTimeSync);
            behaviour.SetSeekThreshold(_seekThreshold);
            behaviour.SetPlaybackRate(_playbackRate);

            return playable;
        }
    }
}