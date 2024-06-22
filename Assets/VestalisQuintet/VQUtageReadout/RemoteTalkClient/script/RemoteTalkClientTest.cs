using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VestalisQuintet.VQUtageReadout
{

    public class RemoteTalkClientTest : MonoBehaviour
    {
        private const string defaultKiritanPath = @"F:\tool\AHS\VOICEROID+\KiritanEX\VOICEROID.exe";
        public RemoteTalkExporter _remoteTalkExporter;

        // Start is called before the first frame update
        async void Start()
        {
            UnityLogger logger = new UnityLogger();
            if(_remoteTalkExporter == null)
            {
                Debug.Assert(false, "RemoteTalkExporter is null");
                return;
            }

            if(!_remoteTalkExporter.launchReader(defaultKiritanPath))
            {
                Debug.LogError("Failed to launch Voiceroid: " + defaultKiritanPath);
                return;
            }

            RemoteTalkClientService rtservice = new RemoteTalkClientService(_remoteTalkExporter, new UnityLogger());

            // 非同期で音声合成を開始
            string outdir = "wavout";
            var exportDir = Path.Combine(Application.dataPath, outdir);

            // publicの音声合成インターフェース呼び出し
            var outFilePath2 = Path.Combine(exportDir, "voiceRemotetalk.wav");
            var speaker = new RemoteTalkSpeaker();
            System.Threading.CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            await rtservice.GenerateVoiceAsync("こんばんは、RemoteTalkです", outFilePath2, speaker, cancellationToken);
            Debug.Log("GenerateVoiceメソッドによる音声合成が完了しました");
        }
    }
}
