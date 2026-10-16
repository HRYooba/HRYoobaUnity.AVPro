using System;
using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// 再生中の MediaPlayer が出したコマを PTS 付きで貯め、指定した時刻のコマを出力テクスチャへ書き出す
    /// </summary>
    /// <remarks>
    /// AVPro はテクスチャをゲームフレームの描画と無関係な時点で更新するため、その時点で表示中のテクスチャは Timeline の時刻と一致しない。
    /// 映像を先行して再生させ、届いたコマから Timeline の時刻のコマを選ぶことで、同じゲームフレームに揃える。
    /// 表示部品は <see cref="TryGetPresentedTexture"/> で出力を取り出す。単一テクスチャの映像だけを扱う。
    /// </remarks>
    public sealed class MediaPlayerFrameBuffer
    {
        // PTS からコマ番号へ直すとき、映像の PTS の端数を吸収する
        private const double FrameRounding = 0.5;
        private const double FrameEpsilon = 1e-6;

        private static readonly Dictionary<MediaPlayer, MediaPlayerFrameBuffer> _buffers = new Dictionary<MediaPlayer, MediaPlayerFrameBuffer>();

        private readonly MediaPlayer _mediaPlayer;
        private readonly RenderTexture[] _slots;
        private readonly long[] _slotTimeStamps;
        private RenderTexture _output;
        private int _next;
        private long _lastCapturedTimeStamp = long.MinValue;
        private long _presentedTimeStamp = long.MinValue;

        /// <summary>出力テクスチャに有効なコマが入っていて、表示部品がそれを使うべきか</summary>
        public bool IsPresenting { get; set; }

        private MediaPlayerFrameBuffer(MediaPlayer mediaPlayer, int size)
        {
            _mediaPlayer = mediaPlayer;
            _slots = new RenderTexture[size];
            _slotTimeStamps = new long[size];
            Clear();
        }

        /// <summary>
        /// MediaPlayer に紐づくバッファを返す。無ければ作る
        /// </summary>
        public static MediaPlayerFrameBuffer Acquire(MediaPlayer mediaPlayer, int size)
        {
            if (_buffers.TryGetValue(mediaPlayer, out var buffer) && buffer._slots.Length == size) return buffer;

            buffer?.Dispose();
            buffer = new MediaPlayerFrameBuffer(mediaPlayer, size);
            _buffers[mediaPlayer] = buffer;
            return buffer;
        }

        /// <summary>
        /// バッファを捨てる。表示部品は MediaPlayer のテクスチャ表示に戻る
        /// </summary>
        public static void Release(MediaPlayer mediaPlayer)
        {
            if (mediaPlayer == null) return;
            if (!_buffers.TryGetValue(mediaPlayer, out var buffer)) return;

            _buffers.Remove(mediaPlayer);
            buffer.Dispose();
        }

        /// <summary>
        /// Timeline の時刻に合わせたコマを表示中なら、その出力テクスチャを返す
        /// </summary>
        public static bool TryGetPresentedTexture(MediaPlayer mediaPlayer, out Texture texture)
        {
            texture = null;
            if (mediaPlayer == null) return false;
            if (!_buffers.TryGetValue(mediaPlayer, out var buffer)) return false;
            if (!buffer.IsPresenting || buffer._output == null) return false;

            texture = buffer._output;
            return true;
        }

        /// <summary>
        /// 貯めたコマを捨てる。出力テクスチャは直前のコマのまま残す
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _slotTimeStamps.Length; i++) _slotTimeStamps[i] = long.MinValue;
            _next = 0;
            _lastCapturedTimeStamp = long.MinValue;
        }

        /// <summary>
        /// MediaPlayer のテクスチャが新しいコマに変わっていれば貯める
        /// </summary>
        public void Capture()
        {
            var producer = _mediaPlayer.TextureProducer;
            if (producer == null || producer.GetTextureCount() != 1) return;

            var texture = producer.GetTexture();
            if (texture == null) return;

            // シーク中のテクスチャは PTS と中身が食い違い得る
            if (_mediaPlayer.Control.IsSeeking()) return;

            var timeStamp = producer.GetTextureTimeStamp();
            if (timeStamp == _lastCapturedTimeStamp) return;
            _lastCapturedTimeStamp = timeStamp;

            var slot = EnsureTexture(ref _slots[_next], texture);
            Graphics.Blit(texture, slot);
            _slotTimeStamps[_next] = timeStamp;
            _next = (_next + 1) % _slots.Length;
        }

        /// <summary>
        /// 時刻 <paramref name="time"/>（秒）に表示すべきコマを出力する。貯めた中に無ければ出力を変えない
        /// </summary>
        public void Present(double time, float frameRate)
        {
            var targetFrame = (long)Math.Floor(time * frameRate + FrameEpsilon);

            var found = -1;
            var foundFrame = long.MinValue;
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slotTimeStamps[i] == long.MinValue) continue;

                var frame = ToFrame(_slotTimeStamps[i], frameRate);
                if (frame <= targetFrame && frame > foundFrame)
                {
                    found = i;
                    foundFrame = frame;
                }
            }
            if (found < 0) return;

            if (_slotTimeStamps[found] != _presentedTimeStamp || !IsPresenting)
            {
                var source = _slots[found];
                Graphics.Blit(source, EnsureTexture(ref _output, source));
                _presentedTimeStamp = _slotTimeStamps[found];
            }
            IsPresenting = true;
        }

        private static long ToFrame(long timeStamp, float frameRate)
        {
            return (long)Math.Floor(timeStamp / Helper.SecondsToHNS * frameRate + FrameRounding);
        }

        private static RenderTexture EnsureTexture(ref RenderTexture texture, Texture source)
        {
            if (texture != null && texture.width == source.width && texture.height == source.height) return texture;

            DestroyTexture(texture);
            texture = new RenderTexture(source.width, source.height, 0)
            {
                name = "HRYooba.AVPro FrameBuffer",
                filterMode = source.filterMode,
                wrapMode = source.wrapMode,
                anisoLevel = source.anisoLevel,
            };
            texture.Create();
            return texture;
        }

        private void Dispose()
        {
            for (var i = 0; i < _slots.Length; i++) DestroyTexture(_slots[i]);
            DestroyTexture(_output);
            _output = null;
            IsPresenting = false;
        }

        private static void DestroyTexture(RenderTexture texture)
        {
            if (texture == null) return;

            texture.Release();
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
