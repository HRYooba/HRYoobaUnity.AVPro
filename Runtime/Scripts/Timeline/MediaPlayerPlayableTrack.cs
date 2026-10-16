using System;
using System.Linq;
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
        [SerializeField]
        [Tooltip("シーク後、新しいコマが届くまで待ってから描画へ進む。映像と他のトラックが同じゲームフレームで切り替わる。\n" +
                 "全コマがキーフレームの映像（HAP・ProRes など）向け。H.264 などは 1 コマのシークに時間がかかり、その間フレームが止まる")]
        private bool _waitForFrame = true;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<MediaPlayerPlayableMixerBehaviour>.Create(graph, inputCount);
            var behaviour = mixer.GetBehaviour();
            behaviour.WaitForFrame = _waitForFrame;
            behaviour.CloseOnDestroy = GetClips().Any(c => c.asset is MediaPlayerPlayableClip clip && clip.DestroyClose);
            return mixer;
        }
    }
}
