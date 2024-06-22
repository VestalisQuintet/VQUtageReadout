
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// 読み上げサービスのインターフェース
    /// </summary>
    public interface IReadOutService {
        UniTask<bool> GenerateVoiceAsync(string message, string outFilePath, ISpeaker speaker, CancellationToken cancellationToken);
        ISpeaker getDefaultDescriptiveSpeaker();
        ISpeaker getDefaultOtherSpeaker();
    }
}
