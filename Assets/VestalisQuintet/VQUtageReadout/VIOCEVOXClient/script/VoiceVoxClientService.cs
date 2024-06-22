using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.Networking;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// VOICEVOX APIを利用して音声合成を行うクライアント
    /// </summary>
    public class VoiceVoxClientService : IReadOutService
    {
        private string baseUrl = "http://localhost"; // VOICEVOX APIのベースURL

        private int port = 50021; // VOICEVOX APIのポート番号

        public string BaseUrl { get => $"{baseUrl}:{port}"; }

        private ILogger _logger; // ロガー

        // コンストラクタ
        // baseUrlとportを指定して初期化
        public VoiceVoxClientService(string baseUrl, int port, ILogger logger)
        {
            this.baseUrl = baseUrl;
            this.port = port;
            _logger = logger;
            _logger.LogInformation("VoiceVoxClientServiceを初期化しました");
        }

        /// <summary>
        /// テキストから音声を合成し、指定されたファイルパスに保存する
        /// </summary>
        /// <param name="text">読み上げる内容</param>
        /// <param name="outFilePath">出力先ファイルパス</param>
        /// <param name="speaker">話者ID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns></returns>
        public async UniTask<bool> GenerateVoiceAsync(string text, string outFilePath, ISpeaker speaker, CancellationToken cancellationToken = default)
        {
            string query = await CreateVoiceSynthesisQuery(text, speaker, cancellationToken);
            await SynthesisVoice(query, speaker, outFilePath, true, cancellationToken);
            return true;
        }

        /// <summary>
        /// 音声合成リクエストのクエリを生成する
        /// </summary>
        /// <param name="text">合成する音声のテキスト</param>
        /// <param name="speaker">話者ID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>音声合成リクエストのクエリ。エラーの場合はnull</returns>
        private async UniTask<string> CreateVoiceSynthesisQuery(string text, ISpeaker speaker, CancellationToken cancellationToken = default)
        {
            string url = $"{BaseUrl}/audio_query";

            // リクエストの作成
            int speakerId = await speaker.getSpeakerID();
            string queryUrl = $"{url}?text={UnityWebRequest.EscapeURL(text)}&speaker={speakerId}";
            Debug.Log("音声合成クエリ：" + queryUrl);
            // POSTである必要があるため、第2引数に空文字列を指定
            UnityWebRequest www = UnityWebRequest.Post(queryUrl, "");

            www.SetRequestHeader("Accept", "application/json");

            // リクエストの送信
            await www.SendWebRequest().WithCancellation(cancellationToken);

            if (www.result == UnityWebRequest.Result.Success)
            {
                // レスポンスの取得と処理
                _logger.LogInformation(www.downloadHandler.text);

                return(www.downloadHandler.text);
            }
            else
            {
                // エラーハンドリング
                _logger.LogError(www.error);
                return null;
            }
        }

        /// <summary>
        /// クエリを送信して音声合成を行う
        /// </summary>
        /// <param name="query">クエリテキスト（json）</param>
        /// <param name="speaker">話者ID</param>
        /// <param name="wavOutPath">音声ファイルの出力先</param>
        /// <param name="enableInterrogativeUpspeak">疑問文の最後に上昇調をつけるか</param>
        /// <returns></returns>
        private async UniTask SynthesisVoice(string query, ISpeaker speaker, string wavOutPath, bool enableInterrogativeUpspeak = true, CancellationToken cancellationToken = default)
        {
            string url = $"{BaseUrl}/synthesis";

            // リクエストの作成
            // speakerとenable_interrogative_upspeakについてはクエリパラメータにする必要がある
            string queryUrl = $"{url}?speaker={speaker.getSpeakerID()}&enable_interrogative_upspeak={enableInterrogativeUpspeak}";

            // Bodyとしてqueryを送信するため、第2引数にqueryを指定
            var www = UnityWebRequestMultimedia.GetAudioClip(queryUrl, AudioType.WAV);

            // リクエストクエリをデバッグ出力
            _logger.LogInformation(query);

            www.SetRequestHeader("content-type", "application/json");
            // Body
            using(var handler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(query)))
            {
                www.uploadHandler = handler;

                // リクエストの送信
                www.method = "POST";
                await www.SendWebRequest().WithCancellation(cancellationToken);
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                // レスポンスとしてwavファイルが返ってくるので、ファイルに保存する
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                SaveWavFile(wavOutPath, clip);
            }
            else
            {
                // エラーハンドリング
                Debug.Log(www.error);
            }

            return;
        }

        /// <summary>
        /// 指定されたファイルパスに、AudioClipをWAVファイルとして保存する
        /// </summary>
        /// <param name="filepath">WAVファイルが保存されるファイルパス</param>
        /// <param name="clip">WAVファイルとして保存されるAudioClip</param>
        private void SaveWavFile(string filepath, AudioClip clip)
        {
            // AudioClipからWAVファイルを作成
            byte[] wavBytes = WavUtility.FromAudioClip(clip);
            File.WriteAllBytes(filepath, wavBytes);
        }

        /// <summary>
        /// デフォルトの地の文の話者を取得する
        /// </summary>
        /// <returns></returns>
        public ISpeaker getDefaultDescriptiveSpeaker()
        {
            return new VoiceVoxSpeaker(baseUrl, port, "四国めたん");
        }

        /// <summary>
        /// デフォルトのその他の話者を取得する
        /// </summary>
        /// <returns></returns>
        public ISpeaker getDefaultOtherSpeaker()
        {
            return new VoiceVoxSpeaker(baseUrl, port, "ずんだもん");
        }
    }
}
