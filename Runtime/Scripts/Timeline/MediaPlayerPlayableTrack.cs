using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    [Serializable]
    [TrackColor(0.855f, 0.8623f, 0.87f)]
    [TrackBindingType(typeof(MediaPlayer))]
    [TrackClipType(typeof(MediaPlayerPlayableClip))]
    public class MediaPlayerPlayableTrack : TrackAsset
    {
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
        {
            var director = gameObject.GetComponent<PlayableDirector>();

            var mediaPlayerClip = clip.asset as MediaPlayerPlayableClip;
            var mediaPlayer = director.GetGenericBinding(this) as MediaPlayer;

            mediaPlayerClip.MediaPlayer = mediaPlayer;
            mediaPlayerClip.Director = director;
            mediaPlayerClip.Clip = clip;

            if (!Application.isPlaying)
            {
                if (mediaPlayer.gameObject.activeSelf)
                {
                    if (!mediaPlayer.MediaOpened || mediaPlayer.Info == null)
                    {
                        mediaPlayer.OpenMedia(mediaPlayer.AutoStart);
                    }

                    if (mediaPlayer.Info != null)
                    {
                        // clipの長さをMediaPlayerの長さに合わせる
                        clip.duration = mediaPlayer.Info.GetDuration();
                    }
                }
            }

            return base.CreatePlayable(graph, gameObject, clip);
        }
    }
}