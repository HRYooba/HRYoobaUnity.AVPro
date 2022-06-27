using System;
using System.Threading;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using Cysharp.Threading.Tasks;
using UniRx;

namespace HRYooba.Library
{
    /// <summary>
    /// AVProのMediaPlayerの拡張
    /// </summary>
    public static class MediaPlayerExtension
    {
        /// <summary>
        /// OpenMediaの非同期
        /// </summary>
        /// <param name="mediaPlayer">self</param>
        /// <param name="path">パス</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="pathType">パスタイプ</param>
        /// <param name="autoPlay">自動再生するか</param>
        /// <param name="timeout">タイムアウト処理の秒数</param>
        /// <returns></returns>
        public static async UniTask OpenMediaAsync(this MediaPlayer mediaPlayer, string path, CancellationToken cancellationToken, MediaPathType pathType = MediaPathType.AbsolutePathOrURL, bool autoPlay = false, float timeout = 5.0f)
        {
            try
            {
                // Media open
                mediaPlayer.OpenMedia(pathType, path, autoPlay);

                // Mediaが開けるまで待機
                await UniTask.WaitWhile(() => !mediaPlayer.MediaOpened).Timeout(TimeSpan.FromSeconds(timeout));

                // Create DebugLog Task
                var showDebugLogAsync = UniTask.Create(async () =>
                {
                    await UniTask.WaitWhile(() => mediaPlayer.Info.GetVideoFrameRate() == 0, cancellationToken: cancellationToken);
                    Debug.Log($"Opened Media: {path} {mediaPlayer.Info.GetVideoWidth()}x{mediaPlayer.Info.GetVideoHeight()} FPS{mediaPlayer.Info.GetVideoFrameRate().ToString("F2")} Duration{mediaPlayer.Info.GetDuration()}");
                });

                await showDebugLogAsync;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 再生終了のIObservable
        /// </summary>
        /// <param name="mediaPlayer">self</param>
        /// <returns></returns>
        public static IObservable<Unit> OnFinishedPlaying(this MediaPlayer mediaPlayer)
        {
            return mediaPlayer.Events.AsObservable().Where(args => args.Item2 == MediaPlayerEvent.EventType.FinishedPlaying).AsUnitObservable();
        }
    }
}