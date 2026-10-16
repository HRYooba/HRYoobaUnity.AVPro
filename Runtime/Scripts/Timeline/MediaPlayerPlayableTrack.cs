using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    public enum MediaPlayerTimelineSyncMode
    {
        /// <summary>映像を先行して再生し、貯めたコマから Timeline の時刻のコマを出す。音が鳴る</summary>
        Buffered,
        /// <summary>毎コマ Timeline の時刻へシークする。音は鳴らない</summary>
        Seek,
    }

    [Serializable]
    [TrackColor(0.855f, 0.8623f, 0.87f)]
    [TrackBindingType(typeof(MediaPlayer))]
    [TrackClipType(typeof(MediaPlayerPlayableClip))]
    public class MediaPlayerPlayableTrack : TrackAsset
    {
        [SerializeField]
        [Tooltip("Buffered: 映像を先行して再生し、貯めたコマから Timeline の時刻のコマを出す。音が鳴る。ゲームのフレーム時間が映像の 1 コマより長いと、届かなかったコマの代わりに 1 つ前のコマが出る\n" +
                 "Seek: 毎コマ Timeline の時刻へシークする。音は鳴らない。全コマがキーフレームの映像（HAP・ProRes など）向け")]
        private MediaPlayerTimelineSyncMode _syncMode = MediaPlayerTimelineSyncMode.Buffered;

        [SerializeField, Min(0)]
        [Tooltip("Buffered で映像を Timeline より先行させるコマ数。音はこの分だけ絵より先に出る")]
        private int _leadFrames = 1;

        [SerializeField, Min(2)]
        [Tooltip("Play mode で貯めるコマ数。先行コマ数 + 2 未満にはならない")]
        private int _bufferSize = 4;

        [SerializeField]
        [Tooltip("Play mode でシークした後（Seek モード・再生開始・ずれの補正・一時停止中の Evaluate）、新しいコマが届くまで待ってから描画へ進む。映像と他のトラックが同じゲームフレームで切り替わる\n" +
                 "待つ間メインスレッドが止まる。H.264 などは 1 コマのシークに時間がかかる。AVPro が待ちに対応しない環境では、コマが届いたゲームフレームから出る")]
        private bool _waitForFrame = true;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<MediaPlayerPlayableMixerBehaviour>.Create(graph, inputCount);
            var behaviour = mixer.GetBehaviour();
            behaviour.SyncMode = _syncMode;
            behaviour.LeadFrames = _leadFrames;
            behaviour.BufferSize = _bufferSize;
            behaviour.WaitForFrame = _waitForFrame;
            behaviour.CloseOnDestroy = GetClips().Any(c => c.asset is MediaPlayerPlayableClip clip && clip.DestroyClose);
            return mixer;
        }
    }
}
