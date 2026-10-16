using System;
using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// MediaPlayer が出したコマを、表示できる形へ変換して PTS 付きで貯め、指定した時刻のコマを出力する
    /// </summary>
    /// <remarks>
    /// AVPro はテクスチャをゲームフレームの描画と無関係な時点で更新するため、その時点の MediaPlayer のテクスチャは Timeline の時刻と一致しない。
    /// 届いたコマを貯めておき、Timeline の時刻のコマを選ぶことで、他のトラックと同じゲームフレームに揃える。
    /// 表示部品は <see cref="TryGetPresentedTexture"/> で出力を取り出す。
    /// </remarks>
    public sealed class MediaPlayerFrameBuffer : IDisposable
    {
        // PTS からコマ番号へ直すとき、映像の PTS の端数を吸収する
        private const double FrameRounding = 0.5;

        // MediaPlayer が先に破棄されても引けるよう、Unity の生存判定（== null）に依らない参照の同一性で持つ
        private static readonly Dictionary<MediaPlayer, MediaPlayerFrameBuffer> _presenters = new Dictionary<MediaPlayer, MediaPlayerFrameBuffer>(ReferenceComparer.Instance);

        private readonly MediaPlayer _mediaPlayer;
        private readonly MediaPlayerResolver _resolver = new MediaPlayerResolver();
        private readonly RenderTexture[] _slots;
        private readonly long[] _slotFrames;
        private RenderTexture _output;
        private int _next;
        private long _lastCapturedTimeStamp = long.MinValue;
        private bool _disposed;

        /// <summary>出力テクスチャに有効なコマが入っていて、表示部品がそれを使うべきか</summary>
        public bool IsPresenting { get; set; }

        /// <summary>出力中のコマ番号。無ければ -1</summary>
        public long PresentedFrame { get; private set; } = -1;

        public MediaPlayerFrameBuffer(MediaPlayer mediaPlayer, int size)
        {
            _mediaPlayer = mediaPlayer;
            _slots = new RenderTexture[size];
            _slotFrames = new long[size];
            Clear();

            // 1 つの MediaPlayer を出力するのは最後に作られたバッファ
            if (_presenters.TryGetValue(mediaPlayer, out var previous)) previous.IsPresenting = false;
            _presenters[mediaPlayer] = this;
        }

        /// <summary>
        /// Timeline の時刻に合わせたコマを出力中なら、その出力テクスチャを返す
        /// </summary>
        public static bool TryGetPresentedTexture(MediaPlayer mediaPlayer, out Texture texture)
        {
            texture = null;
            if (mediaPlayer == null) return false;
            if (!_presenters.TryGetValue(mediaPlayer, out var buffer)) return false;
            if (!buffer.IsPresenting || buffer._output == null) return false;

            texture = buffer._output;
            return true;
        }

        /// <summary>
        /// 映像の PTS（100ns 単位）をコマ番号へ直す
        /// </summary>
        public static long ToFrame(long timeStamp, float frameRate)
        {
            return (long)Math.Floor(timeStamp / Helper.SecondsToHNS * frameRate + FrameRounding);
        }

        /// <summary>
        /// 貯めたコマを捨てる。別の映像を開いたときなど、PTS が別の中身を指すようになったときに呼ぶ。出力テクスチャは直前のコマのまま残す
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _slotFrames.Length; i++) _slotFrames[i] = -1;
            _next = 0;
            _lastCapturedTimeStamp = long.MinValue;
        }

        public bool Contains(long frame)
        {
            return Array.IndexOf(_slotFrames, frame) >= 0;
        }

        /// <summary>
        /// MediaPlayer のテクスチャが新しいコマに変わっていれば貯める
        /// </summary>
        public void Capture(float frameRate)
        {
            if (_disposed || frameRate <= 0f) return;

            var producer = _mediaPlayer.TextureProducer;
            if (producer == null || producer.GetTexture() == null) return;

            var timeStamp = producer.GetTextureTimeStamp();
            if (timeStamp == long.MinValue || timeStamp == _lastCapturedTimeStamp) return;
            _lastCapturedTimeStamp = timeStamp;

            var frame = ToFrame(timeStamp, frameRate);
            var index = Array.IndexOf(_slotFrames, frame);
            if (index < 0)
            {
                index = _next;
                _next = (_next + 1) % _slots.Length;
            }

            _slots[index] = _resolver.Resolve(_mediaPlayer, _slots[index]);
            _slotFrames[index] = frame;
        }

        /// <summary>
        /// <paramref name="targetFrame"/> 以前で最も新しいコマを出力する。貯めた中に無ければ出力を変えない
        /// </summary>
        public void Present(long targetFrame)
        {
            if (_disposed) return;

            var found = -1;
            for (var i = 0; i < _slots.Length; i++)
            {
                var frame = _slotFrames[i];
                if (frame < 0 || frame > targetFrame) continue;
                if (found < 0 || frame > _slotFrames[found]) found = i;
            }
            if (found < 0) return;

            if (_slotFrames[found] != PresentedFrame || !IsPresenting)
            {
                // 貯めたスロットは後のコマで上書きされるので、表示用は別に持つ
                var source = _slots[found];
                if (_output == null || _output.width != source.width || _output.height != source.height)
                {
                    MediaPlayerResolver.ReleaseTexture(_output);
                    _output = RenderTexture.GetTemporary(source.descriptor);
                }
                Graphics.Blit(source, _output);
                PresentedFrame = _slotFrames[found];
            }
            IsPresenting = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_presenters.TryGetValue(_mediaPlayer, out var registered) && registered == this)
            {
                _presenters.Remove(_mediaPlayer);
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                MediaPlayerResolver.ReleaseTexture(_slots[i]);
                _slots[i] = null;
            }
            MediaPlayerResolver.ReleaseTexture(_output);
            _output = null;
            IsPresenting = false;
            PresentedFrame = -1;
            _resolver.Dispose();
        }

        private sealed class ReferenceComparer : IEqualityComparer<MediaPlayer>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(MediaPlayer x, MediaPlayer y) => ReferenceEquals(x, y);

            public int GetHashCode(MediaPlayer obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
