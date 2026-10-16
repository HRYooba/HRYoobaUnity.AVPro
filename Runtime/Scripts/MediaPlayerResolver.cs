using System;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// MediaPlayer の現在のテクスチャを、そのまま表示できる RenderTexture へ変換する
    /// </summary>
    /// <remarks>
    /// 上下反転・ガンマ・YCbCr・アルファパッキングは AVPro の resolve が処理する。
    /// 出力は通常の sRGB テクスチャなので、RawImage・UI Toolkit・マテリアルのどれにもそのまま渡せる。
    /// </remarks>
    internal sealed class MediaPlayerResolver : IDisposable
    {
        private const VideoRender.ResolveFlags Flags = VideoRender.ResolveFlags.ColorspaceSRGB | VideoRender.ResolveFlags.PackedAlpha | VideoRender.ResolveFlags.StereoLeft;

        private Material _material;
        private bool _isMaterialOES;

        /// <summary>
        /// <paramref name="target"/> へ書き出す。サイズが合わなければ作り直したものを返す。返り値は <see cref="ReleaseTexture"/> で解放する
        /// </summary>
        public RenderTexture Resolve(MediaPlayer mediaPlayer, RenderTexture target)
        {
            var producer = mediaPlayer.TextureProducer;
            if (producer == null || producer.GetTexture() == null) return target;

            var isOES = mediaPlayer.IsUsingAndroidOESPath();
            if (_material == null || _isMaterialOES != isOES)
            {
                DestroyMaterial();
                _material = VideoRender.CreateResolveMaterial(isOES);
                _isMaterialOES = isOES;
            }

            // 反転・YCbCr の副テクスチャなどはコマごとに変わり得るので毎回設定する
            VideoRender.SetupMaterialForMedia(_material, mediaPlayer, -1);
            return VideoRender.ResolveVideoToRenderTexture(_material, target, producer, Flags);
        }

        /// <summary>
        /// <see cref="Resolve"/> が返したテクスチャを解放する
        /// </summary>
        public static void ReleaseTexture(RenderTexture texture)
        {
            // ResolveVideoToRenderTexture は GetTemporary で確保する
            if (texture != null) RenderTexture.ReleaseTemporary(texture);
        }

        public void Dispose()
        {
            DestroyMaterial();
        }

        private void DestroyMaterial()
        {
            if (_material == null) return;

            if (Application.isPlaying) UnityEngine.Object.Destroy(_material);
            else UnityEngine.Object.DestroyImmediate(_material);
            _material = null;
        }
    }

    /// <summary>
    /// 表示部品が使う、MediaPlayer の表示用テクスチャ
    /// </summary>
    /// <remarks>
    /// MediaPlayerPlayableTrack が Timeline の時刻に合わせたコマを出しているときはそれを、それ以外は MediaPlayer の現在のコマを変換して返す。
    /// </remarks>
    internal sealed class MediaPlayerDisplayTexture : IDisposable
    {
        private readonly MediaPlayerResolver _resolver = new MediaPlayerResolver();
        private RenderTexture _texture;
        private MediaPlayer _resolvedPlayer;
        private int _resolvedFrameCount = -1;

        public Texture Get(MediaPlayer mediaPlayer)
        {
            if (mediaPlayer == null) return null;
            if (MediaPlayerFrameBuffer.TryGetPresentedTexture(mediaPlayer, out var presented)) return presented;

            var producer = mediaPlayer.TextureProducer;
            if (producer == null || producer.GetTexture() == null) return null;

            var frameCount = producer.GetTextureFrameCount();
            if (_texture == null || mediaPlayer != _resolvedPlayer || frameCount != _resolvedFrameCount)
            {
                _texture = _resolver.Resolve(mediaPlayer, _texture);
                _resolvedPlayer = mediaPlayer;
                _resolvedFrameCount = frameCount;
            }
            return _texture;
        }

        public void Dispose()
        {
            MediaPlayerResolver.ReleaseTexture(_texture);
            _texture = null;
            _resolvedPlayer = null;
            _resolvedFrameCount = -1;
            _resolver.Dispose();
        }
    }
}
