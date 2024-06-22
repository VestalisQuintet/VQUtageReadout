using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// VOICEVOX用話者設定
    /// </summary>
    public class VoiceVoxSpeaker : ISpeaker
    {
        private string _baseUrl; // 接続先URL
        private int _port; // 接続先ポート
        private string _speakerName; // 話者名
        private string _styleName; // スタイル名
        private JArray _modelInfo = null; // モデル情報

        public VoiceVoxSpeaker(string baseUrl, int port, string speakerName, string styleName = "ノーマル")
        {
            this._baseUrl = baseUrl;
            this._port = port;
            this._speakerName = speakerName;
            this._styleName = styleName;
        }

        public async UniTask<int> getModelID()
        {
            // VOICEVOXはモデルIDを持たない
            throw new System.NotImplementedException();
        }

        public async UniTask<int> getSpeakerID()
        {
            JArray jsonArray;
            jsonArray = await getModelsInfo();
            if(jsonArray == null)
            {
                Debug.LogError("jsonArray is null");
                return -1;
            }

            var modelDataIterator = jsonArray.Children().Where(x => ((string)x["name"]) == _speakerName);
            // モデル名が一致するものがない場合
            if (!modelDataIterator.Any())
            {
                Debug.LogError($"話者名{_speakerName}が見つかりません");
                return -1;
            }

            // スタイル名が一致するものを取得する
            var speakerData = modelDataIterator.First();
            JToken jToken = speakerData["styles"].Children().Where(x => ((string)x["name"]) == _styleName).First()["id"];
            if(jToken == null)
            {
                Debug.LogError($"スタイル名{_styleName}が見つかりません");
                return -1;
            }
            int speakerID = (int)jToken;
            
            return speakerID;
        }

        /// <summary>
        /// 話者情報jsonオブジェクトを取得する
        /// </summary>
        /// <returns></returns>
        private async UniTask<JArray> getModelsInfo()
        {
            if(_modelInfo != null)
            {
                return _modelInfo;
            }

            string url = $"{_baseUrl}:{_port}/speakers";
            using (UnityWebRequest www = new UnityWebRequest(url))
            {
                www.method = UnityWebRequest.kHttpVerbGET;
                www.downloadHandler = new DownloadHandlerBuffer();
                await www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error);
                    return null;
                }

                Debug.Log(www.downloadHandler.text);

                string jsonString = www.downloadHandler.text;
                _modelInfo = JArray.Parse(jsonString);
                return _modelInfo;
            }
        }
    }

}
