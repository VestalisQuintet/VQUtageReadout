using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LoadWavFromFile : MonoBehaviour
{
    public AudioSource audioSource; // Inspectorで設定する

    // 音声ファイルのパスを指定する
    private string filePath = "F:\\workspace\\音声\\voicevox\\ずんだもん涙目\\002_ずんだもん（ヘロヘロ）_もう眠すぎてネコミ….wav";

    void Start()
    {
        StartCoroutine(LoadAudio());
    }

    IEnumerator LoadAudio()
    {
        string fileURI = FilePathConverter.ConvertWindowsPathToFileUri(filePath);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileURI, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"{www.error}, URL:{www.url}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("音声再生しました");
            }
        }
    }
}
