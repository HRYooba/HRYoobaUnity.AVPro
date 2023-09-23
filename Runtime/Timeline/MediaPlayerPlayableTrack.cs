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
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var mediaPlayerClip = clip.asset as MediaPlayerPlayableClip;
            var director = gameObject.GetComponent<PlayableDirector>();
            var mediaPlayer = director.GetGenericBinding(this) as MediaPlayer;

            mediaPlayerClip.SetMediaPlayer(mediaPlayer);
            mediaPlayerClip.SetPlayableDirector(director);

            if (mediaPlayer != null)
            {
                if (!mediaPlayer.MediaOpened)
                {
                    mediaPlayer.OpenMedia(false);
                }
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    mediaPlayer.EditorUpdate();

                    // clipの長さをMediaPlayerの長さに合わせる
                    clip.duration = mediaPlayer.Info.GetDuration();

                }
#endif
            }

            return base.CreatePlayable(graph, gameObject, clip);
        }
    }
}