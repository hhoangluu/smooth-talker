using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;

public class TextToSpeechDeepgram : MonoBehaviour
{
    public static TextToSpeechDeepgram Instance;
    public string modelName = "aura-2-odysseus-en";
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private string apiKey = "69952b74f2bf1b3c255c067e9762b11216ba26a1"; // Thay bằng key của bạn

    public AudioSource audioSource;


    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Định dạng chuẩn để lấy file WAV
        string url = $"https://api.deepgram.com/v1/speak?model={modelName}&encoding=linear16&container=wav";
        
        string jsonPayload = "{\"text\":\"" + text.Replace("\"", "\\\"") + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);

            www.SetRequestHeader("Authorization", "Token " + apiKey);
            www.SetRequestHeader("Content-Type", "application/json");

            // THAY ĐỔI QUAN TRỌNG: Dùng Async Operation thay vì yield
            var operation = www.SendWebRequest();

            // Chờ cho đến khi request xong mà không block thread chính
            while (!operation.isDone)
                await Task.Yield(); 

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Deepgram Error: {www.responseCode} | Msg: {www.error}");
                // Nếu lỗi 400 về format, hãy thử bỏ "&container=wav" trong URL
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log("TTS Success!");
                }
            }
        }
    }

    [System.Serializable]
    private class TTSPayload
    {
        public string text;
    }
}