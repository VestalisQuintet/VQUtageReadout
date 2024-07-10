using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VestalisQuintet.VQUtageReadout;
using Utage;
using Cysharp.Threading.Tasks;

/// <summary>
/// VQUtageReadOutを宴シーンに自動設定するためのエディタ拡張
/// </summary>
public class AutoSetting : EditorWindow
{
    [MenuItem("VestalisQuintet/VQUtageReadOut/コンポーネント自動設定")]
    private static void ShowWindow()
    {
        var window = GetWindow<AutoSetting>("AutoSetting");
        window.titleContent = new GUIContent("VQUtageReadOut自動設定");
        window.Show();
    }

    private const string SETTING_COMPLETED = "設定済";
    private const string SETTING_NOT_FINISHED = "未設定";
    [SerializeField] private VisualTreeAsset _rootVisualTreeAsset;

    private GameObject[] sceneObjects;

    private void CreateGUI()
    {
        _rootVisualTreeAsset.CloneTree(rootVisualElement);

        var updateCheckStatusButton = rootVisualElement.Q<Button>("UpdateCheckStatus");
        updateCheckStatusButton.clicked += () =>
        {
            UpdateComponentExistsCheck();
        };

        // 自動設定実行ボタン
        var execAutoSettingButton = rootVisualElement.Q<Button>("AutoSettingButton");
        execAutoSettingButton.clicked += async () =>
        {
            await ExecAutoSetting();
        };
    }

    private void UpdateComponentExistsCheck()
    {
        sceneObjects = null;

        /*
            対象となるコントロール名:
            GenerateVoiceFiles_Check
            UtageSettings_Check
            RemoteTalkAudio_Check
            UtageCustomLoadVoiceFiles_Check
            RemoteTalkExporter_Check
            SoundGenerateSlider_Check
            SoundGenerateMessage_Check
            SoundGenerateOKButton_Check
            GenerateSoundCounterText_Check
            VQAdvCommandTextReadoutCustomCommand_Check
        */
        var generateVoiceFilesCheck = rootVisualElement.Q<TextField>("GenerateVoiceFiles_Check");
        var utageSettingsCheck = rootVisualElement.Q<TextField>("UtageSettings_Check");
        var remoteTalkAudioCheck = rootVisualElement.Q<TextField>("RemoteTalkAudio_Check");
        var utageCustomLoadVoiceFilesCheck = rootVisualElement.Q<TextField>("UtageCustomLoadVoiceFiles_Check");
        var remoteTalkExporterCheck = rootVisualElement.Q<TextField>("RemoteTalkExporter_Check");
        var soundGenerateSliderCheck = rootVisualElement.Q<TextField>("SoundGenerateSlider_Check");
        var soundGenerateMessageCheck = rootVisualElement.Q<TextField>("SoundGenerateMessage_Check");
        var soundGenerateOKButtonCheck = rootVisualElement.Q<TextField>("SoundGenerateOKButton_Check");
        var generateSoundCounterTextCheck = rootVisualElement.Q<TextField>("GenerateSoundCounterText_Check");
        var vqAdvCommandTextReadoutCustomCommandCheck = rootVisualElement.Q<TextField>("VQAdvCommandTextReadoutCustomCommand_Check");

        // GenerateVoiceFilesゲームオブジェクト及びコンポーネントの存在確認
        CheckGenerateVoiceFiles(generateVoiceFilesCheck);

        // UtageSettingsゲームオブジェクト及びコンポーネントの存在確認
        CheckUtageSettings(utageSettingsCheck);

        // RemoteTalkAudioゲームオブジェクト及び、それにAudio Sourceコンポーネントがついているかの存在確認
        CheckRemoteTalkAudio(remoteTalkAudioCheck);

        // UtageCustomLoadVoiceFilesゲームオブジェクト及びコンポーネントの存在確認
        UtageCustomLoadVoiceFilesCheck(utageCustomLoadVoiceFilesCheck);

        // RemoteTalkExporterゲームオブジェクト及びコンポーネントの存在確認
        RemoteTalkExporterCheck(remoteTalkExporterCheck);

        // SoundGenerateSliderゲームオブジェクト及び、それにSliderコンポーネントがついているかの存在確認
        SoundGenerateSliderCheck(soundGenerateSliderCheck);

        // SoundGenerateMessageゲームオブジェクト及び、それにTextMeshProUGUIコンポーネントがついているかの存在確認
        SoundGenerateMessageCheck(soundGenerateMessageCheck);

        // SoundGenerateOKButtonゲームオブジェクト及び、それにButtonコンポーネントがついているかの存在確認
        SoundGenerateOKButtonCheck(soundGenerateOKButtonCheck);

        // GenerateSoundCounterTextゲームオブジェクト及び、それにTextMeshProUGUIコンポーネントがついているかの存在確認
        GenerateSoundCounterTextCheck(generateSoundCounterTextCheck);

        // AdvEngineゲームオブジェクト及び、それにVQAdvCommandTextReadoutCustomCommandコンポーネントがついているかの存在確認
        VQAdvCommandTextReadoutCustomCommandCheck(vqAdvCommandTextReadoutCustomCommandCheck);

        // ここからGenerateVoiceFilesの参照関係を確認する
        CheckGenerateVoiceFilesReferences(generateVoiceFilesCheck);
    }

    private async UniTask ExecAutoSetting()
    {
        // まず最新の状態に更新する
        UpdateComponentExistsCheck();

        // GenerateVoiceFilesが無ければプレハブを配置する
        var generateVoiceFilesCheck = rootVisualElement.Q<TextField>("GenerateVoiceFiles_Check");
        if (generateVoiceFilesCheck.value == SETTING_NOT_FINISHED)
        {
            var generateVoiceFilesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/GenerateVoiceFiles.prefab");
            var generateVoiceFiles = Instantiate(generateVoiceFilesPrefab);
            generateVoiceFiles.name = "GenerateVoiceFiles";
        }

        // SettingSingletonが無ければプレハブを配置する
        var utageSettingsCheck = rootVisualElement.Q<TextField>("UtageSettings_Check");
        if (utageSettingsCheck.value == SETTING_NOT_FINISHED)
        {
            var utageSettingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageSettings/prefab/SettingsSingleton.prefab");
            var utageSettings = Instantiate(utageSettingsPrefab);
            utageSettings.name = "SettingsSingleton";
        }

        // RemoteTalkAudioが無ければ、自動配置処理を呼び出す
        var remoteTalkAudioCheck = rootVisualElement.Q<TextField>("RemoteTalkAudio_Check");
        if (remoteTalkAudioCheck.value == SETTING_NOT_FINISHED)
        {
            // RemoteTalkEditorUtils.CreateRemoteTalkClientを実行
            IST.RemoteTalkEditor.RemoteTalkEditorUtils.CreateRemoteTalkClient(null);

            // この時生成されたRemoteTalkClientは無効化する(1フレーム経過後に無効化する)
            await UniTask.Create(async () =>
            {
                await UniTask.DelayFrame(1);
                var remoteTalkClient = GameObject.Find("RemoteTalkClient");
                remoteTalkClient.SetActive(false);
            });
        }

        // UtageCustomLoadVoiceFilesが無ければプレハブを配置する
        var utageCustomLoadVoiceFilesCheck = rootVisualElement.Q<TextField>("UtageCustomLoadVoiceFiles_Check");
        if (utageCustomLoadVoiceFilesCheck.value == SETTING_NOT_FINISHED)
        {
            var utageCustomLoadVoiceFilesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/UtageCustomLoadVoiceFiles.prefab");
            var utageCustomLoadVoiceFiles = Instantiate(utageCustomLoadVoiceFilesPrefab);
            utageCustomLoadVoiceFiles.name = "UtageCustomLoadVoiceFiles";
        }

        // RemoteTalkExporterが無ければプレハブを配置する
        var remoteTalkExporterCheck = rootVisualElement.Q<TextField>("RemoteTalkExporter_Check");
        if (remoteTalkExporterCheck.value == SETTING_NOT_FINISHED)
        {
            var remoteTalkExporterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/RemoteTalkExporter.prefab");
            var remoteTalkExporter = Instantiate(remoteTalkExporterPrefab);
            remoteTalkExporter.name = "RemoteTalkExporter";
        }

        // SoundGenerateSliderが無ければプレハブを配置する
        var soundGenerateSliderCheck = rootVisualElement.Q<TextField>("SoundGenerateSlider_Check");
        if (soundGenerateSliderCheck.value == SETTING_NOT_FINISHED)
        {
            // Canvas-System UIの子オブジェクトとして配置する
            var soundGenerateSliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/SoundGenerateSlider.prefab");
            var soundGenerateSlider = Instantiate(soundGenerateSliderPrefab);
            soundGenerateSlider.name = "SoundGenerateSlider";
            Transform transform = GameObject.Find("Canvas-System UI").transform;
            if (transform != null)
            {
                soundGenerateSlider.transform.SetParent(transform);
            }
            else
            {
                Debug.LogError("Canvas-System UIが見つかりませんでした。");
            }
        }

        // SoundGererateMessageが無ければプレハブを配置する
        var soundGenerateMessageCheck = rootVisualElement.Q<TextField>("SoundGenerateMessage_Check");
        if (soundGenerateMessageCheck.value == SETTING_NOT_FINISHED)
        {
            // Canvas-System UIの子オブジェクトとして配置する
            var soundGenerateMessagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/SoundGenerateMessage.prefab");
            var soundGenerateMessage = Instantiate(soundGenerateMessagePrefab);
            soundGenerateMessage.name = "SoundGenerateMessage";
            Transform transform = GameObject.Find("Canvas-System UI").transform;
            if (transform != null)
            {
                soundGenerateMessage.transform.SetParent(transform);
            }
            else
            {
                Debug.LogError("Canvas-System UIが見つかりませんでした。");
            }
        }

        // SoundGenerateOKButtonが無ければプレハブを配置する
        var soundGenerateOKButtonCheck = rootVisualElement.Q<TextField>("SoundGenerateOKButton_Check");
        if (soundGenerateOKButtonCheck.value == SETTING_NOT_FINISHED)
        {
            // Canvas-System UIの子オブジェクトとして配置する
            var soundGenerateOKButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/SoundGenerateOKButton.prefab");
            var soundGenerateOKButton = Instantiate(soundGenerateOKButtonPrefab);
            soundGenerateOKButton.name = "SoundGenerateOKButton";
            Transform transform = GameObject.Find("Canvas-System UI").transform;
            if (transform != null)
            {
                soundGenerateOKButton.transform.SetParent(transform);
            }
            else
            {
                Debug.LogError("Canvas-System UIが見つかりませんでした。");
            }
        }

        // GenerateSoundCounterTextが無ければプレハブを配置する
        var generateSoundCounterTextCheck = rootVisualElement.Q<TextField>("GenerateSoundCounterText_Check");
        if (generateSoundCounterTextCheck.value == SETTING_NOT_FINISHED)
        {
            // Canvas-System UIの子オブジェクトとして配置する
            var generateSoundCounterTextPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VestalisQuintet/VQUtageReadout/prefab/GenerateSoundCounterText.prefab");
            var generateSoundCounterText = Instantiate(generateSoundCounterTextPrefab);
            generateSoundCounterText.name = "GenerateSoundCounterText";
            Transform transform = GameObject.Find("Canvas-System UI").transform;
            if (transform != null)
            {
                generateSoundCounterText.transform.SetParent(transform);
            }
            else
            {
                Debug.LogError("Canvas-System UIが見つかりませんでした。");
            }
        }

        // VQAdvCommandTextReadoutCustomCommandが無ければAdvEngineにコンポーネントを追加する
        var vqAdvCommandTextReadoutCustomCommandCheck = rootVisualElement.Q<TextField>("VQAdvCommandTextReadoutCustomCommand_Check");
        if (vqAdvCommandTextReadoutCustomCommandCheck.value == SETTING_NOT_FINISHED)
        {
            var advEngine = findObjectFromScene("AdvEngine");
            if (advEngine != null)
            {
                advEngine.AddComponent<VQAdvCommandTextReadoutCustomCommand>();
            }
            else
            {
                Debug.LogError("AdvEngineが見つかりませんでした。");
            }
        }

        // オブジェクト一覧キャッシュを削除する
        sceneObjects = null;

        // 現時点での配置状態を確認する
        UpdateComponentExistsCheck();

        // GenerateVoiceFilesの参照関係を設定する
        SetGenerateVoiceFilesComponents();

        // 最終的に問題なく配置できたか再度確認する
        await UniTask.Create(async () =>
        {
            await UniTask.DelayFrame(1);
            UpdateComponentExistsCheck();
        });
    }

    private void SetGenerateVoiceFilesComponents()
    {
        var engineRefCheck = rootVisualElement.Q<TextField>("EngineRef_Check");
        var progressBarRefCheck = rootVisualElement.Q<TextField>("ProgressBarRef_Check");
        var canvasAdvUIRefCheck = rootVisualElement.Q<TextField>("CanvasAdvUIRef_Check");
        var loadingUIRefCheck = rootVisualElement.Q<TextField>("LoadingUIRef_Check");
        var remoteTalkExporterRefCheck = rootVisualElement.Q<TextField>("RemoteTalkExporterRef_Check");
        var generateSoundCounterTextRefCheck = rootVisualElement.Q<TextField>("GenerateSoundCounterTextRef_Check");

        // GenerateVoiceFilesが無ければ中断
        var generateVoiceFilesCheck = rootVisualElement.Q<TextField>("GenerateVoiceFiles_Check");
        if (generateVoiceFilesCheck.value == SETTING_NOT_FINISHED)
        {
            Debug.LogError("GenerateVoiceFilesが見つかりませんでした。");
            return;
        }
        GenerateVoiceFiles generateVoiceFiles = findObjectFromScene("GenerateVoiceFiles")?.GetComponent<GenerateVoiceFiles>();
        if(generateVoiceFiles == null)
        {
            Debug.LogError("GenerateVoiceFilesが見つかりませんでした。");
            return;
        }

        // AdvEngineへの参照が無ければ設定する
        if (engineRefCheck.value == SETTING_NOT_FINISHED)
        {
            var advEngine = findObjectFromScene("AdvEngine")?.GetComponent<AdvEngine>();
            if (advEngine != null)
            {
                var serializedGenerateVoiceFiles = new SerializedObject(generateVoiceFiles);
                serializedGenerateVoiceFiles.FindProperty("_engine").objectReferenceValue = advEngine;

                serializedGenerateVoiceFiles.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError("AdvEngineが見つかりませんでした。");
            }
        }

        // progressBarへの参照が無ければ設定する
        if (progressBarRefCheck.value == SETTING_NOT_FINISHED)
        {
            var progressBar = findObjectFromScene("SoundGenerateSlider")?.GetComponent<UnityEngine.UI.Slider>();
            if (progressBar != null)
            {
                generateVoiceFiles.progressBar = progressBar;
            }
            else
            {
                Debug.LogError("SoundGenerateSliderが見つかりませんでした。");
            }
        }

        // canvasAdvUIへの参照が無ければ設定する
        if (canvasAdvUIRefCheck.value == SETTING_NOT_FINISHED)
        {
            var canvasAdvUI = findObjectFromScene("Canvas-AdvUI");
            if (canvasAdvUI != null)
            {
                generateVoiceFiles.canvasAdvUi = canvasAdvUI;
            }
            else
            {
                Debug.LogError("Canvas-AdvUIが見つかりませんでした。");
            }
        }

        // loadingUIへの参照が無ければ設定する
        if (loadingUIRefCheck.value == SETTING_NOT_FINISHED)
        {
            var loadingUI = findObjectFromScene("SoundGenerateSlider");
            if (loadingUI != null)
            {
                generateVoiceFiles.loadingUi = loadingUI;
            }
            else
            {
                Debug.LogError("SoundGenerateSliderが見つかりませんでした。");
            }
        }

        // RemoteTalkExporterへの参照が無ければ設定する
        if (remoteTalkExporterRefCheck.value == SETTING_NOT_FINISHED)
        {
            var remoteTalkExporter = findObjectFromScene("RemoteTalkExporter")?.GetComponent<RemoteTalkExporter>();
            if (remoteTalkExporter != null)
            {
                generateVoiceFiles._remoteTalkExporter = remoteTalkExporter;
            }
            else
            {
                Debug.LogError("RemoteTalkExporterが見つかりませんでした。");
            }
        }

        // GenerateSoundCounterTextへの参照が無ければ設定する
        if (generateSoundCounterTextRefCheck.value == SETTING_NOT_FINISHED)
        {
            var generateSoundCounterText = findObjectFromScene("GenerateSoundCounterText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (generateSoundCounterText != null)
            {
                generateVoiceFiles._generateSoundCounterText = generateSoundCounterText;
            }
            else
            {
                Debug.LogError("GenerateSoundCounterTextが見つかりませんでした。");
            }
        }
    }

    private void CheckGenerateVoiceFilesReferences(TextField generateVoiceFilesCheck)
    {

        // EngineRef_Check
        // ProgressBarRef_Check
        // CanvasAdvUIRef_Check
        // LoadingUIRef_Check
        // RemoteTalkExporterRef_Check
        // GenerateSoundCounterTextRef_Check

        var engineRefCheck = rootVisualElement.Q<TextField>("EngineRef_Check");
        var progressBarRefCheck = rootVisualElement.Q<TextField>("ProgressBarRef_Check");
        var canvasAdvUIRefCheck = rootVisualElement.Q<TextField>("CanvasAdvUIRef_Check");
        var loadingUIRefCheck = rootVisualElement.Q<TextField>("LoadingUIRef_Check");
        var remoteTalkExporterRefCheck = rootVisualElement.Q<TextField>("RemoteTalkExporterRef_Check");
        var generateSoundCounterTextRefCheck = rootVisualElement.Q<TextField>("GenerateSoundCounterTextRef_Check");
        if (generateVoiceFilesCheck.value == SETTING_COMPLETED)
        {
            // GenerateVoiceFiles設定済みであれば、各参照関係を確認する
            var generateVoiceFilesObj = findObjectFromScene("GenerateVoiceFiles").GetComponent<GenerateVoiceFiles>();
            var advEngineRef = findObjectFromScene("AdvEngine").GetComponent<AdvEngine>();

            //AdvEngineの参照関係を確認
            if (generateVoiceFilesObj.Engine != null && advEngineRef == generateVoiceFilesObj.Engine)
            {
                engineRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                engineRefCheck.value = SETTING_NOT_FINISHED;
            }

            // ProgressBarの参照関係を確認
            GameObject loadingUI = findObjectFromScene("SoundGenerateSlider");
            UnityEngine.UI.Slider slider;
            if (loadingUI == null)
            {
                slider = null;
            }
            else
            {
                slider = loadingUI.GetComponent<UnityEngine.UI.Slider>();
            }

            if (generateVoiceFilesObj.progressBar != null && generateVoiceFilesObj.progressBar == slider)
            {
                progressBarRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                progressBarRefCheck.value = SETTING_NOT_FINISHED;
            }

            // canvasAdvUIの参照関係を確認
            GameObject canvas = findObjectFromScene("Canvas-AdvUI");
            if (generateVoiceFilesObj.canvasAdvUi != null && generateVoiceFilesObj.canvasAdvUi == canvas)
            {
                canvasAdvUIRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                canvasAdvUIRefCheck.value = SETTING_NOT_FINISHED;
            }

            // loadingUIの参照関係を確認
            if (generateVoiceFilesObj.loadingUi != null && generateVoiceFilesObj.loadingUi == loadingUI)
            {
                loadingUIRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                loadingUIRefCheck.value = SETTING_NOT_FINISHED;
            }

            // RemoteTalkExporterの参照関係を確認
            RemoteTalkExporter remoteTalkExporter = findObjectFromScene("RemoteTalkExporter")?.GetComponent<RemoteTalkExporter>();
            if (generateVoiceFilesObj._remoteTalkExporter != null && generateVoiceFilesObj._remoteTalkExporter == remoteTalkExporter)
            {
                remoteTalkExporterRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                remoteTalkExporterRefCheck.value = SETTING_NOT_FINISHED;
            }

            // GenerateSoundCounterTextの参照関係を確認
            TMPro.TextMeshProUGUI generateSoundCounterText = findObjectFromScene("GenerateSoundCounterText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (generateVoiceFilesObj._generateSoundCounterText != null && generateVoiceFilesObj._generateSoundCounterText == generateSoundCounterText)
            {
                generateSoundCounterTextRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                generateSoundCounterTextRefCheck.value = SETTING_NOT_FINISHED;
            }
        }
        else
        {
            // GenerateVoiceFiles未設定であれば、以下のテキストフィールド全てを未設定にする
            engineRefCheck.value = SETTING_NOT_FINISHED;
            progressBarRefCheck.value = SETTING_NOT_FINISHED;
            canvasAdvUIRefCheck.value = SETTING_NOT_FINISHED;
            loadingUIRefCheck.value = SETTING_NOT_FINISHED;
            remoteTalkExporterRefCheck.value = SETTING_NOT_FINISHED;
            generateSoundCounterTextRefCheck.value = SETTING_NOT_FINISHED;
        }
    }

    private void CheckGenerateVoiceFiles(TextField generateVoiceFilesCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("GenerateVoiceFiles");
        if (objRef != null)
        {
            result = objRef.GetComponent<GenerateVoiceFiles>() != null;
        }

        generateVoiceFilesCheck.value = GetSettingStatus(result);
    }

    private void CheckUtageSettings(TextField utageSettingsCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("SettingsSingleton");
        if (objRef != null)
        {
            result = objRef.GetComponent<UtageSettings>() != null;
        }

        utageSettingsCheck.value = GetSettingStatus(result);
    }

    private void CheckRemoteTalkAudio(TextField remoteTalkAudioCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("RemoteTalkAudio");
        if (objRef != null)
        {
            result = objRef.GetComponent<AudioSource>() != null;
        }

        remoteTalkAudioCheck.value = GetSettingStatus(result);
    }

    private void UtageCustomLoadVoiceFilesCheck(TextField utageCustomLoadVoiceFilesCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("UtageCustomLoadVoiceFiles");
        if (objRef != null)
        {
            result = objRef.GetComponent<UtageCustomLoadVoiceFiles>() != null;
        }

        utageCustomLoadVoiceFilesCheck.value = GetSettingStatus(result);
    }

    private void RemoteTalkExporterCheck(TextField remoteTalkExporterCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("RemoteTalkExporter");
        if (objRef != null)
        {
            result = objRef.GetComponent<RemoteTalkExporter>() != null;
        }

        remoteTalkExporterCheck.value = GetSettingStatus(result);
    }
    private void SoundGenerateSliderCheck(TextField soundGenerateSliderCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("SoundGenerateSlider");
        if (objRef != null)
        {
            result = objRef.GetComponent<UnityEngine.UI.Slider>() != null;
        }

        soundGenerateSliderCheck.value = GetSettingStatus(result);
    }
    private void SoundGenerateMessageCheck(TextField soundGenerateMessageCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("SoundGenerateMessage");
        if (objRef != null)
        {
            result = objRef.GetComponent<TMPro.TextMeshProUGUI>() != null;
        }

        soundGenerateMessageCheck.value = GetSettingStatus(result);
    }
    private void SoundGenerateOKButtonCheck(TextField soundGenerateOKButtonCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("SoundGenerateOKButton");
        if (objRef != null)
        {
            result = objRef.GetComponent<UnityEngine.UI.Button>() != null;
        }

        soundGenerateOKButtonCheck.value = GetSettingStatus(result);
    }

    private void GenerateSoundCounterTextCheck(TextField generateSoundCounterTextCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("GenerateSoundCounterText");
        if (objRef != null)
        {
            result = objRef.GetComponent<TMPro.TextMeshProUGUI>() != null;
        }

        generateSoundCounterTextCheck.value = GetSettingStatus(result);
    }
    private void VQAdvCommandTextReadoutCustomCommandCheck(TextField vqAdvCommandTextReadoutCustomCommandCheck)
    {
        bool result = false;
        var objRef = findObjectFromScene("AdvEngine");
        if (objRef != null)
        {
            result = objRef.GetComponent<VQAdvCommandTextReadoutCustomCommand>() != null;
        }

        vqAdvCommandTextReadoutCustomCommandCheck.value = GetSettingStatus(result);
    }

    /// <summary>
    /// 設定済み状態に応じたテキストを返す
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private static string GetSettingStatus(bool result)
    {
        if (result)
        {
            return SETTING_COMPLETED;
        }
        else
        {
            return SETTING_NOT_FINISHED;
        }
    }

    /// <summary>
    /// シーン中の全オブジェクトから名称が一致するオブジェクトを取得する
    /// </summary>
    /// <param name="objectName"></param>
    /// <returns></returns>
    private GameObject findObjectFromScene(string objectName)
    {
        var sceneObjects = getSceneAllObjects();
        foreach (var obj in sceneObjects)
        {
            if (obj.name == objectName)
            {
                return obj;
            }
        }
        return null;
    }

    /// <summary>
    /// シーン中の全オブジェクトを取得する
    /// </summary>
    /// <returns></returns>
    private GameObject[] getSceneAllObjects()
    {
        if(sceneObjects == null)
        {
            sceneObjects = FindObjectsOfType<GameObject>(true);
        }
        return sceneObjects;
    }
}
