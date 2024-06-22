using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// AssistantSeikaを利用した音声読み上げ呼び出しクラス
    /// </summary>
    public class AssistantSeikaService
    {
        private ILogger _logger;
        private string _seikaSayPath;

        public AssistantSeikaService(string seikaSayPath, ILogger logger)
        {
            _logger = logger;
            _seikaSayPath = seikaSayPath;
        }

        /// <summary>
        /// 話者一覧をログに出力
        /// </summary>
        public void ListSpeakers()
        {
            var speackersStr = ExecuteCommand("-list");
            _logger.LogInformation("List of speakers:\n" + speackersStr);
        }

        /// <summary>
        /// 読み上げ実行
        /// </summary>
        /// <param name="cid">話者ID</param>
        /// <param name="text">読み上げテキスト</param>
        /// <param name="async">trueなら読み上げ終了を待たず非同期に実行</param>
        /// <param name="outputFile">ファイルを出力する場合はパスを指定</param>
        public void Speak(int cid, string text, bool async = false, string outputFile = null)
        {
            var command = $"-cid {cid} {(async ? "-async " : "")}";

            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                command += $"-save \"{outputFile}\" ";
            }

            command += $"-t \"{text}\"";
            _logger.LogInformation("AssistantSeika command:" + command);
            ExecuteCommand(command);
        }

        /// <summary>
        /// AssistantSeikaコマンド実行
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private string ExecuteCommand(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = $"{_seikaSayPath}",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
                return process.StandardOutput.ReadToEnd();
            }
        }
    }
}
