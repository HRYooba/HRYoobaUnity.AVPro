using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using R3;

namespace HRYooba.AVPro
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
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="autoPlay">自動再生するか</param>
        /// <param name="showLog">ログを表示するか</param>
        /// <param name="timeout">タイムアウト処理の秒数</param>
        /// <returns></returns>
        public static async UniTask OpenMediaAsync(this MediaPlayer mediaPlayer, CancellationToken cancellationToken, bool autoPlay = true, bool showLog = false, float timeout = 5.0f)
        {
            try
            {
                // Media open
                mediaPlayer.OpenMedia(autoPlay);

                // Mediaが開けるまで待機
                await UniTask.WaitWhile(() => !mediaPlayer.MediaOpened).Timeout(TimeSpan.FromSeconds(timeout));
                await UniTask.Yield(cancellationToken: cancellationToken);

                if (showLog)
                {
                    Debug.Log($"Opened Media: {mediaPlayer.MediaPath.Path} {mediaPlayer.Info.GetVideoWidth()}x{mediaPlayer.Info.GetVideoHeight()} FPS{mediaPlayer.Info.GetVideoFrameRate().ToString("F2")} Duration{mediaPlayer.Info.GetDuration()}");
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// OpenMediaの非同期
        /// </summary>
        /// <param name="mediaPlayer">self</param>
        /// <param name="path">パス</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="pathType">パスタイプ</param>
        /// <param name="autoPlay">自動再生するか</param>
        /// <param name="showLog">ログを表示するか</param>
        /// <param name="timeout">タイムアウト処理の秒数</param>
        /// <returns></returns>
        public static async UniTask OpenMediaAsync(this MediaPlayer mediaPlayer, string path, CancellationToken cancellationToken, MediaPathType pathType = MediaPathType.AbsolutePathOrURL, bool autoPlay = true, bool showLog = false, float timeout = 5.0f)
        {
            try
            {
                // Media open
                mediaPlayer.OpenMedia(pathType, path, autoPlay);

                // Mediaが開けるまで待機
                await UniTask.WaitWhile(() => !mediaPlayer.MediaOpened).Timeout(TimeSpan.FromSeconds(timeout));
                await UniTask.Yield(cancellationToken: cancellationToken);

                if (showLog)
                {
                    Debug.Log($"Opened Media: {mediaPlayer.MediaPath.Path} {mediaPlayer.Info.GetVideoWidth()}x{mediaPlayer.Info.GetVideoHeight()} FPS{mediaPlayer.Info.GetVideoFrameRate().ToString("F2")} Duration{mediaPlayer.Info.GetDuration()}");
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// OpenMediaの非同期
        /// </summary>
        /// <param name="mediaPlayer">self</param>
        /// <param name="reference">MediaReference</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="autoPlay">自動再生するか</param>
        /// <param name="showLog">ログを表示するか</param>
        /// <param name="timeout">タイムアウト処理の秒数</param>
        /// <returns></returns>
        public static async UniTask OpenMediaAsync(this MediaPlayer mediaPlayer, MediaReference reference, CancellationToken cancellationToken, bool autoPlay = true, bool showLog = false, float timeout = 5.0f)
        {
            await mediaPlayer.OpenMediaAsync(reference.MediaPath.Path, cancellationToken, reference.MediaPath.PathType, autoPlay, showLog, timeout);
        }

        /// <summary>
        /// Playの非同期
        /// </summary>
        /// <param name="mediaPlayer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async UniTask PlayAsync(this MediaPlayer mediaPlayer, CancellationToken cancellationToken)
        {
            await mediaPlayer.EventsAsObservable().Where(_ => _.EventType == MediaPlayerEvent.EventType.FinishedPlaying).FirstAsync(cancellationToken);
        }

        /// <summary>
        /// Events as Observable
        /// </summary>
        /// <param name="MediaPlayer"></param>
        /// <param name="EventType"></param>
        /// <param name="mediaPlayer"></param>
        public static Observable<(MediaPlayer MediaPlayer, MediaPlayerEvent.EventType EventType, ErrorCode ErrorCode)> EventsAsObservable(this MediaPlayer mediaPlayer)
        {
            if (!mediaPlayer.Events.HasListeners())
            {
                mediaPlayer.Events.AddListener((mp, e, c) => { });
            }

            return mediaPlayer.Events.AsObservable();
        }
    }
}