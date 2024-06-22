using Cysharp.Threading.Tasks;
using UnityEngine;


namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// 話者設定なし用話者設定
    /// </summary>
    public class NoneSpeaker : ISpeaker
    {
        public UniTask<int> getModelID()
        {
            throw new System.NotImplementedException();
        }

        public UniTask<int> getSpeakerID()
        {
            throw new System.NotImplementedException();
        }
    }
}
