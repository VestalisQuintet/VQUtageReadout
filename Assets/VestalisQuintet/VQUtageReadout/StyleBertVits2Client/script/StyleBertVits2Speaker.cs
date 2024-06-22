using Cysharp.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// StyleBertVits2用話者設定
    /// </summary>
    public class StyleBertVits2Speaker : ISpeaker
    {
        private string _baseUrl;
        private int _port;
        private string _modelName; // モデル名
        private string _speakerName; // 話者名
        private string _styleName; // スタイル名
        private JObject _modelInfo = null; // モデル情報

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="baseUrl">接続先url</param>
        /// <param name="port">ポート番号</param>
        /// <param name="modelName">モデル名</param>
        /// <param name="speakerName">話者名</param>
        public StyleBertVits2Speaker(string baseUrl, int port, string modelName, string speakerName, string styleName = "Neutral")
        {
            this._baseUrl = baseUrl;
            this._port = port;
            this._modelName = modelName;
            this._speakerName = speakerName;
            this._styleName = styleName;
        }

        /// <summary>
        /// モデルIDを取得する
        /// </summary>
        /// <returns></returns>
        public async UniTask<int> getModelID()
        {
            JObject jsonObj;
            jsonObj = await getModelsInfo();
            if(jsonObj == null)
            {
                Debug.LogError("jsonObj is null");
                return -1;
            }

            var modelDataIterator = jsonObj.Properties().Where(x => ((string)x.Value["model_path"]).EndsWith($"{_modelName}.safetensors"));
            // モデル名が一致するものがない場合
            if (!modelDataIterator.Any())
            {
                Debug.LogError($"モデル名{_modelName}が見つかりません");
                return -1;
            }

            // 一致するものがあるなら、先頭のキーを数値化して取得
            return int.Parse(modelDataIterator.First().Path);
        }

        /// <summary>
        /// モデル情報jsonオブジェクトを取得する
        /// </summary>
        /// <returns></returns>
        private async UniTask<JObject> getModelsInfo()
        {
            if(_modelInfo != null)
            {
                return _modelInfo;
            }

            string url = $"{_baseUrl}:{_port}/models/info";
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
                _modelInfo = JObject.Parse(jsonString);
                return _modelInfo;
            }
        }

        /// <summary>
        /// 話者IDを取得する
        /// </summary>
        /// <returns></returns>
        public async UniTask<int> getSpeakerID()
        {
            int modelId = await getModelID();

            JObject jsonObj;
            jsonObj = await getModelsInfo();
            if(jsonObj == null)
            {
                Debug.LogError("jsonObj is null");
                return -1;
            }

            var objInfo = jsonObj[modelId.ToString()];
            var speakerIdResult = objInfo["spk2id"][_speakerName];
            if(speakerIdResult != null)
            {
                return speakerIdResult.Value<int>();
            }
            else
            {
                Debug.LogError($"話者名{_speakerName}が見つかりません");
                return -1;
            }
        }
    }
}
