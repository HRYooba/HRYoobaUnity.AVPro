using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro.Editor
{
    /// <summary>
    /// クリップの長さを映像の長さに合わせる操作。どちらも Timeline の Undo に乗る
    /// </summary>
    [CustomTimelineEditor(typeof(MediaPlayerPlayableClip))]
    public class MediaPlayerPlayableClipTimelineEditor : ClipEditor
    {
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (clonedFrom != null) return;

            var duration = MediaPlayerPlayableClipEditorUtility.GetMediaDuration(track);
            if (duration > 0) clip.duration = duration;
        }
    }

    [CustomEditor(typeof(MediaPlayerPlayableClip))]
    public class MediaPlayerPlayableClipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var clip = MediaPlayerPlayableClipEditorUtility.FindTimelineClip(target as MediaPlayerPlayableClip);
            if (clip == null) return;

            var track = clip.GetParentTrack();
            var mediaPlayer = MediaPlayerPlayableClipEditorUtility.GetMediaPlayer(track);
            if (mediaPlayer == null || mediaPlayer.Info == null) return;

            EditorGUILayout.Space(10);

            var duration = mediaPlayer.Info.GetDuration();
            var durationFrames = mediaPlayer.Info.GetDurationFrames();
            var frameRate = mediaPlayer.Info.GetVideoFrameRate();
            EditorGUILayout.LabelField("Duration", $"{duration} sec ({durationFrames} frames@{frameRate}fps)");

            using (new EditorGUI.DisabledScope(duration <= 0))
            {
                if (GUILayout.Button("Set Duration"))
                {
                    Undo.RecordObject(track, "Set MediaPlayer Clip Duration");
                    clip.duration = duration;
                    EditorUtility.SetDirty(track);
                    TimelineEditor.Refresh(RefreshReason.ContentsModified);
                }
            }
        }
    }

    internal static class MediaPlayerPlayableClipEditorUtility
    {
        /// <summary>
        /// Timeline ウィンドウで開いている Timeline から、このクリップアセットを持つ TimelineClip を探す
        /// </summary>
        public static TimelineClip FindTimelineClip(MediaPlayerPlayableClip asset)
        {
            var timeline = TimelineEditor.inspectedAsset;
            if (asset == null || timeline == null) return null;

            foreach (var track in timeline.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset == asset) return clip;
                }
            }
            return null;
        }

        /// <summary>
        /// Timeline ウィンドウで開いている PlayableDirector がトラックに割り当てた MediaPlayer
        /// </summary>
        public static MediaPlayer GetMediaPlayer(TrackAsset track)
        {
            var director = TimelineEditor.inspectedDirector;
            if (director == null || track == null) return null;
            return director.GetGenericBinding(track) as MediaPlayer;
        }

        /// <summary>
        /// 開いていない・読み込み中なら 0
        /// </summary>
        public static double GetMediaDuration(TrackAsset track)
        {
            var mediaPlayer = GetMediaPlayer(track);
            if (mediaPlayer == null || mediaPlayer.Info == null) return 0;
            return mediaPlayer.Info.GetDuration();
        }
    }
}
