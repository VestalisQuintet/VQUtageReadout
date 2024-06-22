
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.Networking;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// StyleBertVits2 APIを利用して音声合成を行うクライアント
    /// </summary>
    public class StyleBertVits2ClientService : IReadOutService
    {
        private string baseUrl = "http://127.0.0.1"; // StyleBertVits2 APIのベースURL

        private int port = 5000; // StyleBertVits2 APIのポート番号

        private ILogger _logger; // ロガー

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="baseUrl">接続先URL</param>
        /// <param name="port">接続先ポート</param>
        /// <param name="unityLogger">ロガー</param>
        public StyleBertVits2ClientService(string baseUrl, int port, ILogger unityLogger)
        {
            this.baseUrl = baseUrl;
            this.port = port;
            this._logger = unityLogger;
        }

        public async UniTask<bool> GenerateVoiceAsync(string message, string outFilePath, ISpeaker speaker, CancellationToken cancellationToken)
        {
            // modelとspeakerを引数で指定できるようにする
            int modelId = await speaker.getModelID();
            int speakerId = await speaker.getSpeakerID();

            // messageをURLエンコードする
            string encodedMessage = UnityWebRequest.EscapeURL(message);

            // クエリ作成
            // クエリの例
            // http://127.0.0.1:5000/voice?text=%E3%82%82%E3%81%B5%E3%82%82%E3%81%B5%EF%BC%9F&model_id=0&speaker_id=0&sdp_ratio=0.2&noise=0.6&noisew=0.8&length=1&language=JP&auto_split=true&split_interval=0.5&assist_text_weight=1&style=Neutral&style_weight=5
            string query = $"{baseUrl}:{port}/voice?text={encodedMessage}&model_id={modelId}&speaker_id={speakerId}&sdp_ratio=0.2&noise=0.6&noisew=0.8&length=1&language=JP&auto_split=true&split_interval=0.5&assist_text_weight=1&style=Neutral&style_weight=5";

            var www = UnityWebRequestMultimedia.GetAudioClip(query, AudioType.WAV);

            // リクエストクエリをデバッグ出力
            _logger.LogInformation(query);

            www.method = "GET";

            await www.SendWebRequest().WithCancellation(cancellationToken);

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
                return false;
            }

            // レスポンスとしてwavファイルが返ってくるので、ファイルに保存する
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            SaveWavFile(outFilePath, clip);

            return true;
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

        public ISpeaker getDefaultDescriptiveSpeaker()
        {
            return new StyleBertVits2Speaker(baseUrl, port, "jvnv-M1-jp_e158_s14000", "jvnv-M1-jp", "Neutral");
        }
        public ISpeaker getDefaultOtherSpeaker()
        {
            return new StyleBertVits2Speaker(baseUrl, port, "jvnv-F1-jp_e160_s14000", "jvnv-F1-jp", "Neutral");
        }
    }
}
