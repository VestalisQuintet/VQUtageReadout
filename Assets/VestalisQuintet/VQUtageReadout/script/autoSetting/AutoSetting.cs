using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VestalisQuintet.VQUtageReadout;
using Utage;

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
            RemoteTalkExporter remoteTalkExporter = findObjectFromScene("RemoteTalkExporter").GetComponent<RemoteTalkExporter>();
            if (generateVoiceFilesObj._remoteTalkExporter != null && generateVoiceFilesObj._remoteTalkExporter == remoteTalkExporter)
            {
                remoteTalkExporterRefCheck.value = SETTING_COMPLETED;
            }
            else
            {
                remoteTalkExporterRefCheck.value = SETTING_NOT_FINISHED;
            }

            // GenerateSoundCounterTextの参照関係を確認
            TMPro.TextMeshProUGUI generateSoundCounterText = findObjectFromScene("GenerateSoundCounterText").GetComponent<TMPro.TextMeshProUGUI>();
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
