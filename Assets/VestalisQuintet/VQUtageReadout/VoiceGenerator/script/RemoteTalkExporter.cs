using UnityEngine;
using System.Collections;
using IST.RemoteTalk;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace VestalisQuintet.VQUtageReadout
{
    public class RemoteTalkExporter : MonoBehaviour
    {
        [SerializeField] AudioSource m_output;
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 8100;
        [SerializeField] private string exportPath = "ExportedAudio";
        [SerializeField] private AudioFileFormat exportFormat = AudioFileFormat.Wave;
        [SerializeField] private TalkParam[] m_talkParams;
        [SerializeField] [Range(1024, 65536)] private int m_sampleGranularity = 8192;

        private Cast[] m_casts = new Cast[0] { };
        private int m_castID;

        bool m_isServerReady = false;
        bool m_isServerTalking = false;
        Talk m_currentTalk = new Talk();
        private rtHTTPClient httpClient;
        private rtAsync m_asyncStats;
        private rtAsync m_asyncExport;
        private rtAsync m_asyncTalk;
        private string m_hostName;

        public static event Action<Talk> onTalkStart;
        public static event Action<Talk, bool> onTalkFinish;

        void Start()
        {
            
        }

        void Awake()
        {
            MakeClient();
        }

        void Update()
        {
            UpdateState();
        }
        protected static void FireOnTalkStart(Talk t)
        {
            if (onTalkStart != null)
                onTalkStart(t);
        }
        protected static void FireOnTalkFinish(Talk t, bool succeeded)
        {
            if (onTalkFinish != null)
                onTalkFinish(t, succeeded);
        }

        public Cast[] casts { get { return m_casts; } }

        public string castName
        {
            get {
                if (m_castID >= 0 && m_castID < m_casts.Length)
                    return m_casts[m_castID].name;
                else
                    return "";
            }
        }
        public bool isServerReady
        {
            get { return m_isServerReady; }
        }
        public bool isServerTalking
        {
            get { return m_isServerTalking; }
        }

        public bool isReady
        {
            get { return isActiveAndEnabled && isServerReady && !isServerTalking && !isPlaying; }
        }

        public int castID
        {
            get { return m_castID; }
            set
            {
                m_castID = value;
                if (m_castID >= 0 && m_castID < m_casts.Length)
                {
                    var prev = m_talkParams;
                    m_talkParams = TalkParam.Clone(m_casts[m_castID].paramInfo);
                    TalkParam.Merge(m_talkParams, prev);
                }
                else
                    m_talkParams = new TalkParam[0];
            }
        }

        public bool isPlaying
        {
            get {
                bool ret = false;
                UseOutput(audio =>
                {
                    if (audio.isPlaying)
                        ret = true;
                });
                return ret;
            }
        }

        bool SyncBuffers()
        {
            httpClient.SyncBuffers();
            return !m_isServerTalking;
        }


        public async UniTask ReadAndExportFile(string talkText, string path, CancellationToken token)
        {
            // RemoteTalkClientRef.isReadyがTrueになるまで待つ
            await UniTask.WaitUntil(() => isReady, PlayerLoopTiming.Update, token);

            // 音声読み上げ
            Debug.Log("RemoteTalk音声読み上げ開始");
            Play(talkText);
            Debug.Log("RemoteTalk音声読み上げ完了");

            await UniTask.WaitUntil(() => m_asyncTalk.isFinished, PlayerLoopTiming.Update, token);
            await UniTask.WaitUntil(() => isReady, PlayerLoopTiming.Update, token);

            // 書き出し実行
            Debug.Log("RemoteTalk音声書き出し開始");
            ExportWave(talkText, path);
            Debug.Log("RemoteTalk音声書き出し完了");
        }

        /// <summary>
        /// 読み上げソフトを起動する
        /// </summary>
        /// <returns></returns>
        public bool launchReader(string kiritanPath)
        {
            // 東北きりたんを起動
            string exePath;
            // kiritanPathにファイルがあるなら、このパスを採用する
            if(string.IsNullOrEmpty(kiritanPath) || !File.Exists(kiritanPath))
            {
                var castSettingKiritanResult = UtageSettings.Instance.castReaderIDList.Where(x => x.castName == "きりたん");
                if (castSettingKiritanResult.Count() == 0)
                {
                    exePath = kiritanPath;
                }
                else
                {
                    // 設定ファイルからきりたんのファイルパスを探して設定する
                    exePath = castSettingKiritanResult.First().readerExePath;
                }
            }
            else
            {
                exePath = kiritanPath;
            }

            if (exePath != null && exePath.Length > 0)
            {
                var ret = rtPlugin.LaunchVOICEROIDEx(exePath);
                if (ret > 0)
                {
                    serverPort = ret;
                    UpdateStats();
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        
        void UseOutput(Action<RemoteTalkAudio> act)
        {
            var rta = m_output != null ? Misc.GetOrAddComponent<RemoteTalkAudio>(m_output.gameObject) : null;
            if (rta != null)
                act(rta);
        }

        void MakeClient()
        {
            if (!httpClient)
            {
                httpClient = rtHTTPClient.Create();
                httpClient.Setup(serverAddress, serverPort);
                m_asyncStats = httpClient.UpdateServerStatus();
            }
        }

        // WAVファイルをエクスポートするためのメソッド
        public void ExportWave(string text, string path)
        {
            if(!httpClient)
            {
                Debug.LogError("HttpClient is not initialized.");
                return;
            }
            if (!isReady)
                return;
            if ((m_castID < 0 || m_castID >= m_casts.Length) ||
                text.Length == 0)
                return;

            m_currentTalk.castName = castName;
            m_currentTalk.text = text;
            m_currentTalk.param = m_talkParams;

            var tparam = rtTalkParams.defaultValue;
            tparam.cast = (short)m_castID;
            tparam.Assign(m_talkParams);

            // ここで、必要な設定を行った上でexportWaveメソッドを呼び出します
            switch(exportFormat){
                case AudioFileFormat.Wave:
                    m_asyncExport = httpClient.ExportWave(path);
                    break;
                case AudioFileFormat.Ogg:
                    var settings = new rtOggSettings(); // oggの設定が必要な場合はここで設定
                    m_asyncExport = httpClient.ExportOgg(path, ref settings);
                    break;
            }
        }

        /// <summary>
        /// 音声を再生する
        /// </summary>
        /// <param name="talkText"></param>
        /// <returns></returns>
        public bool Play(string talkText)
        {
            if (!isReady)
                return false;
            if ((m_castID < 0 || m_castID >= m_casts.Length) ||
                (talkText == null || talkText.Length == 0))
                return false;

            m_currentTalk.castName = castName;
            m_currentTalk.text = talkText;
            m_currentTalk.param = m_talkParams;

            m_isServerTalking = true;
            var tparam = rtTalkParams.defaultValue;
            tparam.cast = (short)m_castID;
            tparam.Assign(m_talkParams);
            m_asyncTalk = httpClient.Talk(ref tparam, talkText);
            return true;
        }

        void UpdateState()
        {
            bool talkSucceeded = false;

            if (m_asyncStats.isFinished)
            {
                m_asyncStats.Release();

                m_hostName = httpClient.host;
                m_casts = httpClient.casts;
                foreach (var c in m_casts)
                    c.hostName = m_hostName;

                if (m_casts.Length > 0)
                    castID = Mathf.Clamp(m_castID, 0, m_casts.Length - 1);
                else
                    castID = 0;

                m_isServerReady = m_hostName != "Server Not Found";
                Misc.RefreshWindows();
            }
            var playing = isPlaying;

            if (m_asyncTalk)
            {
                if (!playing)
                {
                    var buf = httpClient.SyncBuffers();
                    if (buf.sampleLength > m_sampleGranularity ||
                        (buf.sampleLength > 0 && m_asyncTalk.isFinished && m_asyncTalk.boolValue))
                    {
                        UseOutput(audio => { audio.Play(buf, SyncBuffers); });
                        FireOnTalkStart(m_currentTalk);
                    }
                }

                if (m_asyncTalk.isFinished)
                {
                    httpClient.SyncBuffers();
                    talkSucceeded = m_asyncTalk.boolValue;
                    FireOnTalkFinish(m_currentTalk, talkSucceeded);

                    m_asyncTalk.Release();
                    m_isServerTalking = false;
                }
            }
        }
        
        public void UpdateStats()
        {
            httpClient.Setup(serverAddress, serverPort);
            m_asyncStats = httpClient.UpdateServerStatus();
        }
    }
}
