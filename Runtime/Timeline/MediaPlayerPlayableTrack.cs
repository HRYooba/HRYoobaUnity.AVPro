using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.Library
{
    [Serializable]
    [TrackColor(0.855f, 0.8623f, 0.87f)]
    [TrackBindingType(typeof(MediaPlayer))]
    [TrackClipType(typeof(MediaPlayerPlayableClip))]
    public class MediaPlayerPlayableTrack : TrackAsset
    {
        private PlayableDirector _director = null;

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            _director = director;
        }

        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var mediaPlayerClip = clip.asset as MediaPlayerPlayableClip;
            var mediaPlayer = _director.GetGenericBinding(this) as MediaPlayer;

            mediaPlayerClip.SetMediaPlayer(mediaPlayer);
            mediaPlayerClip.SetPlayableDirector(_director);

            if (mediaPlayer != null)
            {
                if (!mediaPlayer.MediaOpened)
                {
                    mediaPlayer.OpenMedia(false);
                }
            }

            return base.CreatePlayable(graph, gameObject, clip);
        }

#if UNITY_EDITOR
        protected override void OnCreateClip(TimelineClip clip)
        {
            var mediaPlayer = _director.GetGenericBinding(this) as MediaPlayer;
            if (mediaPlayer != null)
            {
                if (!mediaPlayer.MediaOpened)
                {
                    mediaPlayer.OpenMedia(false);
                }

                mediaPlayer.EditorUpdate();

                // clipの長さをMediaPlayerの長さに合わせる
                clip.duration = mediaPlayer.Info.GetDuration();
            }
        }
#endif
    }
}