
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// RemoteTAlk APIを利用して音声合成を行うクライアント
    /// </summary>
    public class RemoteTalkClientService : IReadOutService
    {
        public RemoteTalkExporter _remoteTalkExporter = null; // RemoteTalkExporterへの参照

        private ILogger _logger; // ロガー

        public RemoteTalkClientService(RemoteTalkExporter remoteTalkExporter, ILogger logger)
        {
            _remoteTalkExporter = remoteTalkExporter;
            _logger = logger;
            _logger.LogInformation("RemoteTalkClientServiceを初期化しました");
        }
        
        public async UniTask<bool> GenerateVoiceAsync(string message, string outFilePath, ISpeaker speaker, CancellationToken cancellationToken)
        {
            if(!_remoteTalkExporter)
            {
                // RemoteTalkExporterが設定されていない場合は何もしない
                return false;
            }

            // TODO: 複数の話者が存在する場合はspeakerから特定する
            // 現状は話者ごとにexeを起動しなおしている

            _logger.LogInformation("RemoteTalkClientで音声ファイルを生成します");

            await _remoteTalkExporter.ReadAndExportFile(message, outFilePath, cancellationToken);
            _logger.LogInformation($"RemoteTalkClientで音声ファイルを生成しました: {outFilePath}");

            return true;
        }

        /// <summary>
        /// デフォルトの地の文の話者を取得する
        /// </summary>
        /// <returns></returns>
        public ISpeaker getDefaultDescriptiveSpeaker()
        {
            return new RemoteTalkSpeaker();
        }

        /// <summary>
        /// デフォルトのその他の話者を取得する
        /// </summary>
        /// <returns></returns>

        public ISpeaker getDefaultOtherSpeaker()
        {
            return new RemoteTalkSpeaker();
        }
    }

}
