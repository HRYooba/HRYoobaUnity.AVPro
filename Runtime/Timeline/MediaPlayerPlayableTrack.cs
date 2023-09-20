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
            base.GatherProperties(director, driver);
        }

        protected override void OnCreateClip(TimelineClip clip)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            if (_director != null)
            {
                var mediaPlayer = _director.GetGenericBinding(this) as MediaPlayer;
                if (mediaPlayer != null)
                {
                    if (!mediaPlayer.MediaOpened)
                    {
                        mediaPlayer.OpenMedia(false);
                    }

                    mediaPlayer.EditorUpdate();
                    
                    clip.duration = mediaPlayer.Info.GetDuration();
                }
            }
#endif

            base.OnCreateClip(clip);
        }
    }
}