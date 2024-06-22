using System;
using System.Collections.Generic;
using UnityEngine;
using Utage;
using VestalisQuintet.VQUtageReadout;
using System.Linq;
using UnityEngine.UI;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using static VestalisQuintet.VQUtageReadout.UtageSettings;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;

public class GenerateVoiceFiles : MonoBehaviour
{
    public const string DESCRIPTIVE = "【地の文】";
    public const string DEFAULT_SPEAKER = "【デフォルト】";
    private const float UPDATE_INTERVAL = 0.03f; // 進捗を更新する間隔
    public Slider progressBar;

    [SerializeField]
    protected AdvEngine _engine;

    public AdvEngine Engine { get => _engine; }

    public GameObject canvasAdvUi; // UtageのUI

    public GameObject loadingUi; // プログレスバーのUI

    private ReadOutServiceManager _readOutServiceManager; // 読み上げサービス管理クラス

    private VoiceVoxClientService _voiceVoxClientService = null; // VOICEVOXのクライアント

    private RemoteTalkClientService _remoteTalkClientService = null; // RemoteTalkのクライアント
    private NoneReadOutService _noneReadOutService; // 読み上げなしのサービス

    private StyleBertVits2ClientService _styleBertVits2ClientService = null; // StyleBertVits2のクライアント
    private CancellationTokenSource _voiceGenerationCancellationTokenSource;
    public RemoteTalkExporter _remoteTalkExporter = null; // RemoteTalkExporterへの参照

    private float recentWaitProgress = 0f; // 最後にwaitした進捗

    [HideInInspector]
    public bool generateFinished = false; // 音声生成が完了したかどうか


    // Start is called before the first frame update
    async void Start()
    {
        if(UtageSettings.Instance.useGenerateVoice)
        {
            _readOutServiceManager = new ReadOutServiceManager();

            // 設定ファイルを確認し、話者ごとに異なる音声合成サービスを登録する
            RegisterReadoutService();

            // 全てのシナリオデータが読み込まれたかを確認する
            while (Engine.IsWaitBootLoading)
            {
                await UniTask.DelayFrame(1);
            }
            await generateVoiceData();
        }

        // 読み上げ機能を使用する設定の場合、音声ファイルを登録する
        if(UtageSettings.Instance.useReadOut)
        {
            // 全てのシナリオデータが読み込まれたかを確認する
            while (Engine.IsWaitBootLoading)
            {
                await UniTask.DelayFrame(1);
            }
            // コマンドに音声ファイルを登録する
            registerGeneratedVoiceData();
        }

        // 終了したらプログレスバーを非表示にする
        loadingUi.SetActive(false);

        // UTAGEのUIを表示する
        canvasAdvUi.SetActive(true);
    }

    private void registerGeneratedVoiceData()
    {
        var scenarioTable = Engine.DataManager.ScenarioDataTbl;
        foreach (var scenarioItem in scenarioTable)
        {
            // シナリオごとのTEXTコマンドを読み取る
			foreach (var keyValue in scenarioItem.Value.ScenarioLabels)
			{
				// シナリオラベルごとのTEXTコマンドを読み取る
                foreach(var command in keyValue.Value.CommandList)
                {
                    // command.GetType()の結果を確認し、VQAdvCommandTextReadoutであるかを確認する
                    if(command.GetType() == typeof(VQAdvCommandTextReadout))
                    {
                        var customCommand = (VQAdvCommandTextReadout)command;
                        if(customCommand.VoiceFile == null)
                        {
                            customCommand.SetGeneratedVoice();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 設定ファイルに登録された話者名と音声合成サービスを登録する
    /// </summary>
    private void RegisterReadoutService()
    {
        UtageSettings.Instance.castReaderIDList.ForEach(castSetting =>
        {
            switch (castSetting.readerType)
            {
                case ReaderType.VoiceVox:
                    // VOICEVOX
                    // 話者名と読み上げサービスを登録
                    _readOutServiceManager.RegisterService(castSetting.castName, getVoiceVoxClientSingleton(Instance.voiceVoxBaseUrl, Instance.voiceVoxPort));
                    Debug.Log($"{castSetting.castName}の読み上げエンジンにVOICEVOXのクライアントを登録しました");
                    break;
                case ReaderType.RemoteTalk:
                    // VOICEROID等
                    // 話者名と読み上げサービスを登録
                    _readOutServiceManager.RegisterService(castSetting.castName, getRemoteTalkClientSingleton(castSetting.readerExePath));
                    Debug.Log($"{castSetting.castName}の読み上げエンジンにRemoteTalkのクライアントを登録しました");
                    break;
                case ReaderType.StyleBertVits2:
                    // StyleBertVits2
                    // 話者名と読み上げサービスを登録
                    _readOutServiceManager.RegisterService(castSetting.castName, getStyleBertVits2ClientSingleton(Instance.styleBertVits2BaseUrl, Instance.styleBertVits2Port, castSetting));
                    Debug.Log($"{castSetting.castName}の読み上げエンジンにStyleBertVits2のクライアントを登録しました");
                    break;
                case ReaderType.None:
                    // 読み上げなし
                    // 話者名と読み上げサービスを登録
                    _readOutServiceManager.RegisterService(castSetting.castName, getNoneReadOutSingleton());
                    Debug.Log($"{castSetting.castName}の読み上げエンジンに読み上げなしのサービスを登録しました");
                    break;
                default:
                    Debug.Assert(false, $"Unknown reader type: {castSetting.readerType}");
                    break;
            }
        });

        // castSetting.castNameに【デフォルト】が未設定の場合は、デフォルトの話者設定を登録する
        if(UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DEFAULT_SPEAKER) == null)
        {
            // VOICEVOX
            // 話者名と読み上げサービスを登録
            _readOutServiceManager.RegisterService(DEFAULT_SPEAKER, getVoiceVoxClientSingleton(UtageSettings.Instance.voiceVoxBaseUrl, UtageSettings.Instance.voiceVoxPort));
            Debug.Log($"デフォルトの読み上げエンジンにVOICEVOXのクライアントを登録しました");
        }

        // castSetting.castNameに【地の文】が未設定の場合は、デフォルトの話者設定を登録する
        if(UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DESCRIPTIVE) == null)
        {
            // 話者名と読み上げサービスを登録
            // DEFAULT_SPEAKERと同じエンジンを使用する
            IReadOutService defaultCastService = _readOutServiceManager.GetService(DEFAULT_SPEAKER, out var getServiceResult);
            Debug.Assert(getServiceResult, $"No service found for character: {DEFAULT_SPEAKER}");
            _readOutServiceManager.RegisterService(DESCRIPTIVE, defaultCastService);
            Debug.Log($"地の文の読み上げエンジンに{DEFAULT_SPEAKER}のエンジンを登録しました");
        }
    }

    /// <summary>
    /// 読み上げなしのサービスをシングルトン形式で取得する
    /// </summary>
    /// <returns></returns>
    private IReadOutService getNoneReadOutSingleton()
    {
        if(_noneReadOutService == null)
        {
            _noneReadOutService = new NoneReadOutService();
        }
        return _noneReadOutService;
    }

    /// <summary>
    /// StyleBertVits2のクライアントをシングルトン形式で取得する
    /// </summary>
    /// <param name="castSetting"></param>
    /// <returns></returns>
    private StyleBertVits2ClientService getStyleBertVits2ClientSingleton(string baseUrl, int port, CastSettings castSetting)
    {
        if(_styleBertVits2ClientService == null)
        {
            _styleBertVits2ClientService = new StyleBertVits2ClientService(baseUrl, port, new UnityLogger());
        }
        return _styleBertVits2ClientService;
    }

    /// <summary>
    /// VOICEVOXのクライアントをシングルトン形式で取得する
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    private VoiceVoxClientService getVoiceVoxClientSingleton(string baseUrl, int port)
    {
        if(_voiceVoxClientService == null)
        {
            _voiceVoxClientService = new VoiceVoxClientService(baseUrl, port, new UnityLogger());
        }
        
        return _voiceVoxClientService;
    }

    /// <summary>
    /// RemoteTalkのクライアントをシングルトン形式で取得する
    /// </summary>
    /// <returns></returns>
    private RemoteTalkClientService getRemoteTalkClientSingleton(string voiceroidPath)
    {
        if(_remoteTalkClientService == null)
        {
            _remoteTalkClientService = new RemoteTalkClientService(_remoteTalkExporter, new UnityLogger());
        }
        
        return _remoteTalkClientService;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// シナリオのテキストコマンドを元にボイスデータを生成する
    /// </summary>
    /// <returns></returns>
    public async UniTask generateVoiceData()
    {
        var textCommandList = GetTextCommandListWithLinq();
        int textCommandTotal = textCommandList.Count;
        Debug.Log("テキストコマンド数:" + textCommandTotal);

        // textCommandListを読み上げサービス種別ごとにソートする
        textCommandList.Sort((a, b) => {
                string readOutSettingIDStr_a = getReadOutSettingIDStr(a.talkChar);
                string readOutSettingIDStr_b = getReadOutSettingIDStr(b.talkChar);
                return readOutSettingIDStr_a.CompareTo(readOutSettingIDStr_b);
            });

        // デバッグのため、ユニークなgetReadOutSettingIDStrの結果文字列とその出現回数を表示する
/*         var readOutSettingIDStrList = textCommandList.Select(textCommand => getReadOutSettingIDStr(textCommand.talkChar)).ToList();
        var readOutSettingIDStrCount = readOutSettingIDStrList.GroupBy(x => x).Select(x => new { Key = x.Key, Count = x.Count() }).ToList();
        readOutSettingIDStrCount.ForEach(x => Debug.Log($"Key: {x.Key}, Count: {x.Count}")); */

        // ユニークなgetReadOutSettingIDStrの結果文字列別にまとめた(string hash, string talkChar, string message)形式の
        // タプルのディクショナリ（キーはgetReadOutSettingIDStrの結果）としてtextCommandListを分割する
        var textCommandListSplitted = textCommandList.GroupBy(textCommand => getReadOutSettingIDStr(textCommand.talkChar))
            .ToDictionary(group => group.Key, group => group.ToList());

        // デフォルトの出力先としてUnityの実行ファイルのあるディレクトリ以下を指定
        string exportDir = getOutputDir();

        // 出力ディレクトリが存在しないなら作る
        if (!Directory.Exists(exportDir))
        {
            Directory.CreateDirectory(exportDir);
        }

        // プログレスバーを出す
        progressBar.value = 0;

        // 音声を生成する
        //var messages = textCommandList.ToUniTaskAsyncEnumerable();
        // textCommandListSplittedの要素単位で音声生成を行う
        Debug.Log("音声生成開始");
        var token = this.GetCancellationTokenOnDestroy();
        int generatedVoiceCount = 0;
        recentWaitProgress = 0f;
        await textCommandListSplitted.ToList().ToUniTaskAsyncEnumerable().ForEachAwaitAsync(async textCommandListSplittedItem =>
        {
            var messages = textCommandListSplittedItem.Value.ToUniTaskAsyncEnumerable();

            // RemoteTalkの場合は、Voiceroidの起動を試みる
            if (textCommandListSplittedItem.Key.StartsWith("RemoteTalk"))
            {
                // _readOutServiceManagerからRemoteTalkのvoiceroidPathを取得する
                string voiceroidPath = UtageSettings.Instance.castReaderIDList.Find(
                    castSetting => getReadOutSettingIDStr(castSetting.castName) == textCommandListSplittedItem.Key)?.readerExePath;
                if(!_remoteTalkExporter.launchReader(voiceroidPath))
                {
                    Debug.Assert(false, "Failed to launch RemoteTalk");
                    return;
                }
            }

            await messages.ForEachAwaitAsync(async command =>
            {
                string outFileName = Path.Combine(exportDir, command.hash + ".wav");
                Debug.Log("出力ファイル名:" + outFileName);
                Debug.Log("テキストコマンドメッセージ:" + command.message);
                Debug.Log("テキストコマンドハッシュ:" + command.hash);
                Debug.Log("テキストコマンド話者:" + command.talkChar);

                // ファイルが既に存在するならスキップ
                if (File.Exists(outFileName))
                {
                    generatedVoiceCount = await UpdateProgressBar(textCommandTotal, generatedVoiceCount);
                    Debug.Log("ファイルが既に存在するためスキップ:" + outFileName);
                    return;
                }

                // メッセージが空の場合はスキップ
                if (string.IsNullOrEmpty(command.message))
                {
                    generatedVoiceCount = await UpdateProgressBar(textCommandTotal, generatedVoiceCount);
                    await UniTask.DelayFrame(1);
                    Debug.Log("メッセージが空のためスキップ:" + outFileName);
                    return;
                }

                await GenerateVoiceFilesAsync(command, outFileName, token);

                // update progress bar
                generatedVoiceCount = await UpdateProgressBar(textCommandTotal, generatedVoiceCount);
                Debug.Log("音声生成進捗:" + progressBar.value);
            }, token);

            // RemoteTalkの場合は、Voiceroidの終了をユーザーに促す
            if (textCommandListSplittedItem.Key.StartsWith("RemoteTalk"))
            {
                string castCharacterName = UtageSettings.Instance.castReaderIDList.Find(
                    castSetting => getReadOutSettingIDStr(castSetting.castName) == textCommandListSplittedItem.Key)?.castName;
                string textMessage = 
                    $"キャラクター[{castCharacterName}]分の音声生成が終了しました。\n" +
                    "Voiceroidが起動している場合は、Voiceroidを終了してください。\n" +
                    "Voiceroidを終了したら、OKボタンを押してください。";
                // ボタン押し待ちがキャンセルされた場合、ループを抜ける
                try
                {
                    await WaitForUserConfirmation(textMessage, token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("User cancelled the operation [WaitForUserConfirmation]");
                    return;
                }
            }
        }, token);

        generateFinished = true;
        Debug.Log("音声生成完了");
    }

    /// <summary>
    /// メッセージを確認する事をユーザーに促す
    /// </summary>
    /// <param name="talkChar"></param>
    /// <param name="textCommandListSplittedItem"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask WaitForUserConfirmation(string textMessage, CancellationToken token)
    {
        var transformUIRef = GameObject.Find("Canvas-System UI");
        Debug.Assert(transformUIRef != null, "Canvas-System UI not found");
        var soundGenerateMessageObjRef = transformUIRef.transform.Find("SoundGenerateMessage");
        Debug.Assert(soundGenerateMessageObjRef != null, "SoundGenerateMessage not found");
        var messageTMPRef = soundGenerateMessageObjRef.GetComponent<TextMeshProUGUI>();
        Debug.Assert(messageTMPRef != null, "TextMeshProUGUI not found");

        messageTMPRef.text = textMessage;

        // ボタンの設定
        var soundGenerateMessageOKButtonObjRef = transformUIRef.transform.Find("SoundGenerateOKButton");
        Debug.Assert(soundGenerateMessageOKButtonObjRef != null, "SoundGenerateOKButton not found");
        var okButtonRef = soundGenerateMessageOKButtonObjRef.GetComponent<Button>();
        Debug.Assert(okButtonRef != null, "Button not found");

        bool okButtonClicked = false;

        okButtonRef.onClick.RemoveAllListeners();
        okButtonRef.onClick.AddListener(() =>
        {
            // ボタンが押された変数を有効化する
            okButtonClicked = true;
        });

        // メッセージとボタンを表示する
        soundGenerateMessageObjRef.gameObject.SetActive(true);
        soundGenerateMessageOKButtonObjRef.gameObject.SetActive(true);

        // ボタンが押されるまでUniTaskで待つ
        await UniTask.WaitUntil(() => okButtonClicked, PlayerLoopTiming.Update, token);

        // メッセージとボタンを消す
        soundGenerateMessageObjRef.gameObject.SetActive(false);
        soundGenerateMessageOKButtonObjRef.gameObject.SetActive(false);
    }

    /// <summary>
    /// 音声ファイルを生成する
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="talkChar"></param>
    /// <param name="command"></param>
    /// <param name="outFileName"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask GenerateVoiceFilesAsync((string hash, string talkChar, string message) command, string outFileName, CancellationToken token)
    {
        // 話者名が空の場合は【地の文】を設定する
        string talkChar = command.talkChar;
        if (string.IsNullOrEmpty(talkChar))
        {
            talkChar = DESCRIPTIVE;
        }

        bool getServiceResult;
        var voiceService = _readOutServiceManager.GetService(talkChar, out getServiceResult);
        if (!getServiceResult)
        {
            // 設定ファイルに個別キャラ設定が無い場合、規定の話者を設定して音声合成サービスを取得する
            voiceService = _readOutServiceManager.GetService(DEFAULT_SPEAKER, out getServiceResult);
            Debug.Assert(getServiceResult, $"No service found for character: {DEFAULT_SPEAKER}");
        }

        ISpeaker speaker;
        var castSetting = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == talkChar);
        // 話者が登録済みでも地の文でもない場合、デフォルトの話者を設定する
        if (castSetting == null)
        {
            castSetting = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DEFAULT_SPEAKER);
        }

        speaker = getSpeakerFromCastSetting(castSetting);

        // 読み上げサービスを利用して音声を生成する
        int maxRetries = 2;
        int retries = 0;
        bool success = false;
        while (retries <= maxRetries && !success)
        {
            try
            {
                // 1音声読み上げごとに、30秒のタイムアウトを設定する
                var timeoutController = new TimeoutController();
                var timeoutToken = timeoutController.Timeout(TimeSpan.FromSeconds(30));
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken).Token;

                await voiceService.GenerateVoiceAsync(command.message, outFileName, speaker, linkedToken);

                timeoutController.Reset();
                success = true;
            }
            catch (OperationCanceledException) when (retries < maxRetries)
            {
                // タイムアウト時のリトライ
                retries++;
                Debug.LogWarning($"Retrying voice generation ({retries}/{maxRetries}) due to timeout.");
            }
            catch (Exception e)
            {
                retries++;
                Debug.LogError($"Failed to generate voice: {e.Message} ({retries}/{maxRetries}) for {command.message}");
                // その他のエラーの時、ユーザー向けメッセージを出す
                // retries <= maxRetriesの時と、そうでない時でメッセージを変える
                string textMessage;
                if(retries <= maxRetries)
                {
                    textMessage = 
                        $"音声生成に失敗しました。{retries}/{maxRetries}回目のリトライを行います。\n" +
                        "音声生成サービスが正常に動作しているか確認してください。\n" +
                        "OKボタンを押してください。";
                }
                else
                {
                    textMessage = 
                        $"音声生成に失敗しました。該当メッセージの音声生成をスキップし、次に移ります。\n" +
                        "音声生成サービスが正常に動作しているか確認してください。\n" +
                        "OKボタンを押してください。";
                }
                // ボタン押し待ちがキャンセルされた場合、ループを抜ける
                try
                {
                    await WaitForUserConfirmation(textMessage, token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("User cancelled the operation [WaitForUserConfirmation]");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 話者名から、音声生成サービスと発音モデル名を識別可能な文字列を取得する
    /// </summary>
    /// <param name="talkChar">宴上の話者名</param>
    /// <returns></returns>
    private string getReadOutSettingIDStr(string talkChar)
    {
        // まずは話者名から、音声生成サービスを表す文字列を取得する
        string serviceName = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == talkChar)?.readerType.ToString();
        if (string.IsNullOrEmpty(serviceName))
        {
            // 話者名からサービスを取得できない場合は、デフォルトのサービス名称を設定する
            serviceName = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DEFAULT_SPEAKER)?.readerType.ToString();
            if (string.IsNullOrEmpty(serviceName))
            {
                // デフォルトのサービス名称も取得できない場合は、不明を設定する
                serviceName = "Unknown";
            }
        }

        // 次に、発音モデル名を取得する
        string modelName = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == talkChar)?.modelName;
        if (string.IsNullOrEmpty(modelName))
        {
            // 発音モデル名が取得できない場合は、デフォルトの発音モデル名を設定する
            modelName = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DEFAULT_SPEAKER)?.modelName;
            if (string.IsNullOrEmpty(modelName))
            {
                // デフォルトの発音モデル名も取得できない場合は、不明を設定する
                modelName = "Unknown";
            }
        }

        // 次に、Speaker名を取得する
        string speakerName = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == talkChar)?.speakerName;
        if (string.IsNullOrEmpty(speakerName))
        {
            // Speaker名が取得できない場合は、デフォルトのSpeaker名を設定する
            speakerName = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DEFAULT_SPEAKER)?.speakerName;
            if (string.IsNullOrEmpty(speakerName))
            {
                // デフォルトのSpeaker名も取得できない場合は、不明を設定する
                speakerName = "Unknown";
            }
        }

        // 次に、実行ファイルパスを取得する
        string readerExePath = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == talkChar)?.readerExePath;
        if (string.IsNullOrEmpty(readerExePath))
        {
            // 実行ファイルパスが取得できない場合は、デフォルトの実行ファイルパスを設定する
            readerExePath = UtageSettings.Instance.castReaderIDList.Find(castSetting => castSetting.castName == DEFAULT_SPEAKER)?.readerExePath;
            if (string.IsNullOrEmpty(readerExePath))
            {
                // デフォルトの実行ファイルパスも取得できない場合は、不明を設定する
                readerExePath = "Unknown";
            }
        }

        // 最後に、サービス名、発音モデル名、Speaker名を組み合わせて識別可能な文字列を生成する
        // 区切り文字は_とする
        return $"{serviceName}_{modelName}_{speakerName}_{readerExePath}";
    }

    private ISpeaker getSpeakerFromCastSetting(CastSettings castSetting)
    {
        switch(castSetting.readerType)
        {
            case ReaderType.VoiceVox:
                return new VoiceVoxSpeaker(UtageSettings.Instance.voiceVoxBaseUrl, UtageSettings.Instance.voiceVoxPort, castSetting.speakerName);
            case ReaderType.RemoteTalk:
                return new RemoteTalkSpeaker();
            case ReaderType.StyleBertVits2:
                return new StyleBertVits2Speaker(
                    UtageSettings.Instance.styleBertVits2BaseUrl,
                    UtageSettings.Instance.styleBertVits2Port,
                    castSetting.modelName, castSetting.speakerName, castSetting.styleName);
            case ReaderType.None:
                return new NoneSpeaker();
            default:
                Debug.Assert(false, $"Unknown reader type: {castSetting.readerType}");
                return null;
        }
    }

    public static string getOutputDir()
    {
        string outdir = "voicewavout";
        string exportDir = Path.Combine(Application.dataPath, "..", outdir).Replace("\\", "/");
        return exportDir;
    }

    /// <summary>
    /// プログレスバーを更新する
    /// </summary>
    /// <param name="textCommandTotal"></param>
    /// <param name="generatedVoiceCount"></param>
    /// <returns></returns>
    private async UniTask<int> UpdateProgressBar(int textCommandTotal, int generatedVoiceCount)
    {
        int newGeneratedVoiceCount = generatedVoiceCount + 1;
        progressBar.value = newGeneratedVoiceCount / (float)textCommandTotal;

        // 前回waitした進捗から指定%以上進んだらwaitし、進捗を更新する
        if ((progressBar.value - recentWaitProgress) > UPDATE_INTERVAL)
        {
            recentWaitProgress = progressBar.value;
            await UniTask.DelayFrame(1);
            Debug.Log("DelayFrame実施:" + progressBar.value);
        }
        return newGeneratedVoiceCount;
    }

    /// <summary>
    /// シナリオのテキストコマンドを取得する
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="talkChar"></param>
    /// <param name="GetTextCommandListWithLinq("></param>
    /// <returns></returns>
    public List<(string hash, string talkChar, string message)> GetTextCommandListWithLinq()
    {
        var scenarioTable = Engine.DataManager.ScenarioDataTbl;
        Debug.Log("シナリオ数:" + scenarioTable.Count);

        return scenarioTable.Values
            .SelectMany(scenarioItem => scenarioItem.ScenarioLabels.Values)
            .SelectMany(label => label.CommandList)
            .OfType<AdvCommandText>()
            .Select(command =>
            {
                // CharacterOverride列に話者名が設定されている場合は、その話者名を取得する
                string overrideTalkChar = command.TryParseCell<String>("CharacterOverride", out var tmpOverrideChar) ? tmpOverrideChar : null;
                string talkChar = command.TryParseCell<String>("Arg1", out var tmpTalkChar) ? tmpTalkChar : DESCRIPTIVE;
                if(overrideTalkChar != null && overrideTalkChar != "")
                {
                    talkChar = overrideTalkChar;
                }
                string message = command.ParseCellLocalizedText();
                string combinedText = talkChar + "_" + message;
                string hash = ComputeHash.ComputeSha256Hash(combinedText);

                //リッチテキストのタグを削除する
                string planeTextMessage = Regex.Replace(message, "<[^>]*?>", string.Empty);

                return (hash, talkChar, planeTextMessage);
            }).ToList();
    }

}
