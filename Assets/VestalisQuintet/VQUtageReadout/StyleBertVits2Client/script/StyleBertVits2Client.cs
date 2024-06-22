using System.IO;
using UnityEngine;
namespace VestalisQuintet.VQUtageReadout
{
    public class StyleBertVits2Client : MonoBehaviour
    {
        [SerializeField]
        private string baseUrl = "http://127.0.0.1"; // StyleBertVits2 APIのベースURL

        [SerializeField]
        private int port = 5000; // StyleBertVits2 APIのポート番号

        public string BaseUrl { get => $"{baseUrl}:{port}"; }

        // Start is called before the first frame update
        async void Start()
        {
            UnityLogger logger = new UnityLogger();
            StyleBertVits2ClientService sbv2service = new StyleBertVits2ClientService(baseUrl, port, new UnityLogger());

            // 非同期で音声合成を開始
            string outdir = "wavout";
            var exportDir = Path.Combine(Application.dataPath, outdir);

            // publicの音声合成インターフェース呼び出し
            var outFilePath2 = Path.Combine(exportDir, "voice2.wav");
            //var speaker = new StyleBertVits2Speaker(baseUrl, port, "Syuji_voice_2023_e100_s9600", "Syuji_voice_2023", "Neutral");
            var speaker = new StyleBertVits2Speaker(baseUrl, port, "Azo_commando_e100_s10400", "Azo_commando", "Neutral");
            await sbv2service.GenerateVoiceAsync("こんばんは、スタイルバートビッツ2なのだ", outFilePath2, speaker, default);
            Debug.Log("GenerateVoiceメソッドによる音声合成が完了しました");
        }

        // Update is called once per frame
        void Update()
        {

        }

    }
}

