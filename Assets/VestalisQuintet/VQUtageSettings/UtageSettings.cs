using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Dweiss;
using Utage;
using System.IO;

namespace VestalisQuintet.VQUtageReadout
{

	[System.Serializable]
	public class UtageSettings : ASettings {
        public bool useReadOut = false; // 読み上げ機能を使用するかどうか（既存の音声ファイルを読み上げるか）
        public bool useGenerateVoice = false; // 音声生成機能を有効にするかどうか

        public string voiceVoxBaseUrl = "http://localhost"; // VOICEVOX APIのベースURL
        public int voiceVoxPort = 50021; // VOICEVOX APIのポート番号

        public string styleBertVits2BaseUrl = "http://127.0.0.1"; // styleBertVits2のベースURL
        public int styleBertVits2Port = 5000; // VOICEVOX APIのポート番号

        public List<CastSettings> castReaderIDList = new List<CastSettings>();
        public bool overridePartVoiceByReadOut = false;// 読み上げ音声でパートボイスを上書きするオプション


        [System.Serializable]
        public class CastSettings
        {
            public string castName = "ロボ子"; // シナリオファイル上のキャスト名
            public ReaderType readerType = ReaderType.VoiceVox; // 読み上げソフト種別
            public string speakerName = ""; // 話者情報
            public string modelName = ""; // 読み上げモデル名
            public string styleName = ""; // 読み上げスタイル名
            public string readerExePath = ""; // 読み上げソフトのパス
        }

        public enum ReaderType {
            // 今バージョンでは廃止
            //BoyomiChan,
            //AssistantSeika,
            None,
            RemoteTalk,
            VoiceVox,
            StyleBertVits2
        }

        private new void Awake() {
			base.Awake ();
            SetupSingelton();
        }

        private static void WriteToFileRuntime(string json, string fileDestination)
        {
            UnityEngine.Debug.Log("Save settings file at " + fileDestination + " with data: " + json);

            File.WriteAllText(fileDestination, json);
        }
        public void SaveToFileRuntime()
        {
            // ランタイム時の書きこみ先を設定
			var fileDestination = System.IO.Path.Combine( Application.dataPath, "../");
			fileDestination = System.IO.Path.Combine(fileDestination, FileName);
			fileDestination = System.IO.Path.GetFullPath(fileDestination);

            string json = null;
            try
            {
                json = JsonUtility.ToJson(this);
               
            } catch(System.Exception e)
            {
                Debug.LogError("Error with loading settings file: " + e);
            }

            if(json != null)
            {
                WriteToFileRuntime(json, fileDestination);
            }
        }

        #region  Singelton
        public static UtageSettings _instance;
        public static UtageSettings Instance { get { return _instance; } }
        private void SetupSingelton()
        {
            if (_instance != null)
            {
                Debug.LogError("Error in settings. Multiple singeltons exists: " + _instance.name + " and now " + this.name);
            }
            else
            {
                _instance = this;
            }
        }
        #endregion
    }
}
