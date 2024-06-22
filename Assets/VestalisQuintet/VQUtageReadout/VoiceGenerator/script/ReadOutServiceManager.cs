
using System.Collections.Generic;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// 話者ごとに異なる音声合成サービスを管理するクラス
    /// </summary>
    public class ReadOutServiceManager
    {
        private Dictionary<string, IReadOutService> _services;

        /// <summary>
        /// ReadOutServiceManagerクラスの新しいインスタンスを初期化する
        /// </summary>
        public ReadOutServiceManager()
        {
            _services = new Dictionary<string, IReadOutService>();
        }
        
        /// <summary>
        /// 特定の話者に対して読み上げサービスを登録する
        /// </summary>
        /// <param name="speaker">話者の名前</param>
        /// <param name="service">登録する読み上げサービス</param>
        public void RegisterService(string speaker, IReadOutService service)
        {
            _services[speaker] = service;
        }

        /// <summary>
        /// 特定の話者に対する読み上げサービスを取得する
        /// </summary>
        /// <param name="speaker">話者の名前</param>
        /// <param name="result">サービスが取得できたか</param>
        /// <returns>指定された話者の読み上げサービス</returns>
        public IReadOutService GetService(string speaker, out bool result)
        {
            result = _services.ContainsKey(speaker);
            if(result)
            {
                return _services[speaker];
            }
            return null;
        }
    }
}
