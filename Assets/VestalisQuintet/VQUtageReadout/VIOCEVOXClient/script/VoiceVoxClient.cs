using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.IO;
using System.Text;

using System;
namespace VestalisQuintet.VQUtageReadout
{
    /// <summary>
    /// VOICEVOX APIを利用して音声合成を行うクライアント
    /// </summary>
    public class VoiceVoxClient : MonoBehaviour
    {
        [SerializeField]
        private string baseUrl = "http://localhost"; // VOICEVOX APIのベースURL

        [SerializeField]
        private int port = 50021; // VOICEVOX APIのポート番号

        public string BaseUrl { get => $"{baseUrl}:{port}"; }

        // Start is called before the first frame update
        async void Start()
        {
            UnityLogger logger = new UnityLogger();
            VoiceVoxClientService vvservice = new VoiceVoxClientService(baseUrl, port, logger);

            // 非同期で音声合成を開始
            string outdir = "wavout";
            var exportDir = Path.Combine(Application.dataPath, outdir);

            // publicの音声合成インターフェース呼び出し
            var outFilePath2 = Path.Combine(exportDir, "voice2.wav");
            var speaker = new VoiceVoxSpeaker(baseUrl, port, "小夜/SAYO");
            System.Threading.CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            await vvservice.GenerateVoiceAsync("こんばんは、VOICEVOXなのだ", outFilePath2, speaker, cancellationToken);
            Debug.Log("GenerateVoiceメソッドによる音声合成が完了しました");
        }

        // Update is called once per frame
        void Update()
        {

        }

    }
}