using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Utage;

/// <summary>
/// 生成した音声を扱うカスタム宴アセットファイル
/// </summary>
public class GeneratedVoiceCustomFile : AssetFileBase
{
    private UnityWebRequest www;

    private CancellationTokenSource cancellationTokenSource;

    public GeneratedVoiceCustomFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
    : base(mangager, fileInfo, settingData)
    {
    }

    //ローカルまたはキャッシュあるか（つまりサーバーからDLする必要があるか）
    public override bool CheckCacheOrLocal() { return false; }

    /// <summary>
    /// ロード処理
    /// </summary>
    /// <param name="onComplete"></param>
    /// <param name="onFailed"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override IEnumerator LoadAsync(Action onComplete, Action onFailed)
    {

        cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        var Task = InitFromCustomFileManager(cancellationToken);
        yield return new WaitUntil(() => Task.GetAwaiter().IsCompleted);
        onComplete();
        yield break;
    }

    /// <summary>
    /// 自作のファイルマネージャーから、オブジェクトの参照を行う
    /// </summary>
    private async UniTask InitFromCustomFileManager(CancellationToken token)
    {
        if(FileType != AssetFileType.Sound)
        {
            // 音声のみを扱う
            Debug.Assert(false, "FileType is not Sound");
            return;
        }

        string path = FilePathConverter.ConvertWindowsPathToFileUri(FileInfo.FileName);
        
        www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
        await www.SendWebRequest().ToUniTask(cancellationToken: token);

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"{www.error}, URL:{www.url}");
            www.Dispose();
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            Debug.Assert(clip != null, "clip is null");
            Sound = clip;
            Debug.Log($"ロード対象clip：{FileInfo.FileName}");
            IsLoadEnd = true;
        }

        return;
    }

    /// <summary>
    /// アンロード処理
    /// </summary>
    public override void Unload()
    {
        if (IsLoadEnd != true)
        {
            return;
        }

        www.Dispose();
        cancellationTokenSource.Cancel();
        IsLoadEnd = false;
    }
}