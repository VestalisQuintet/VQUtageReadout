using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utage;

/// <summary>
/// 宴のCustomLoadManagerに読み上げボイスファイルを登録する
/// </summary>
public class UtageCustomLoadVoiceFiles : MonoBehaviour
{
    void Awake()
    {
        // ファイルのロードを上書きするコールバックを登録.
        AssetFileManager.GetCustomLoadManager().OnFindAsset += FindGenerateVoiceAsset;

        Debug.Log("UtageCustomLoadVoiceFiles によるファイルロードコールバックの登録完了");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// GenerateVoiceFilesで生成したアセットを取得する処理
    /// 取得できなかった場合はassetにnullを入れる
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="fileInfo"></param>
    /// <param name="settingData"></param>
    /// <param name="asset"></param>
    private void FindGenerateVoiceAsset(AssetFileManager manager, AssetFileInfo fileInfo, IAssetFileSettingData settingData, ref AssetFileBase asset)
    {
        if(fileInfo.FileType != AssetFileType.Sound)
        {
            asset = null;
            return;
        }
        string outdir = GenerateVoiceFiles.getOutputDir();
        // アセットのファイルがoutdirにあるなら対象とする
        if (fileInfo.FileName.Contains(outdir))
        {
            asset = new GeneratedVoiceCustomFile(manager, fileInfo, settingData);
        }
        else
        {
            asset = null;
        }
    }
}
