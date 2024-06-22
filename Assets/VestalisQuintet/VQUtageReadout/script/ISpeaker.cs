
using Cysharp.Threading.Tasks;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// 話者指定インターフェース
    /// </summary>
    public interface ISpeaker
    {
        public UniTask<int> getModelID();
        public UniTask<int> getSpeakerID();
    }
}
