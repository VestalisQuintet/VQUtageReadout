using System.Threading;
using Cysharp.Threading.Tasks;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// 音声合成を行わないサービス
    /// </summary>
    public class NoneReadOutService : IReadOutService
    {
        public async UniTask<bool> GenerateVoiceAsync(string message, string outFilePath, ISpeaker speaker, CancellationToken cancellationToken)
        {
            // 何もしない
            await UniTask.CompletedTask;
            return true;
        }

        public ISpeaker getDefaultDescriptiveSpeaker()
        {
            return new NoneSpeaker();
        }

        public ISpeaker getDefaultOtherSpeaker()
        {
            return new NoneSpeaker();
        }
    }
}
