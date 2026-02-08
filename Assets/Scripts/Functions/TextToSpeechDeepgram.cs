using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class TextToSpeechDeepgram : MonoBehaviour
{
    public static TextToSpeechDeepgram Instance;
    public string modelName = "aura-2-asteria-en";
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

    public async Task SpeakAsync(string text, string voiceModel = "aura-asteria-en")
    {
        if (string.IsNullOrEmpty(text)) return;

        string model = voiceModel;
        string url = $"https://api.deepgram.com/v1/speak?model={model}&encoding=linear16&container=wav";

        string jsonPayload = "{\"text\":\"" + text.Replace("\"", "\\\"") + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using var www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
        www.SetRequestHeader("Authorization", "Token " + apiKey);
        www.SetRequestHeader("Content-Type", "application/json");

        var operation = www.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Deepgram Error: {www.responseCode} | Msg: {www.error}");
            return;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    [System.Serializable]
    private class TTSPayload
    {
        public string text;
    }
}