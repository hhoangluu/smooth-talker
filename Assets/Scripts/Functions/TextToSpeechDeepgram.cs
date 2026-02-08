using System.Collections;
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

    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        StartCoroutine(GetSpeech(text));
    }

    IEnumerator GetSpeech(string text)
{
    // 1. Cấu hình Model và URL
    // Lưu ý: Dùng aura-asteria-en (v1) để ổn định nhất, hoặc aura-2-asteria-en nếu bạn muốn v2
    string url = $"https://api.deepgram.com/v1/speak?model={modelName}&encoding=linear16&container=wav";

    // 2. Tạo Body theo đúng định dạng JSON
    string jsonPayload = "{\"text\":\"" + text.Replace("\"", "\\\"") + "\"}";
    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

    using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
    {
        // Gán dữ liệu thô
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
        
        // Header BẮT BUỘC
        www.SetRequestHeader("Authorization", "Token " + apiKey);
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // Nếu lỗi 400, chúng ta cần đọc nội dung lỗi từ Buffer
            // Vì DownloadHandlerAudioClip không cho đọc text, ta nên dùng trick sau để debug:
            Debug.LogError($"Deepgram Error: {www.responseCode} | Msg: {www.error}");
            
            // Nếu muốn xem lỗi chi tiết từ Deepgram (như sai field nào):
            // Hãy tạm thời đổi downloadHandler thành DownloadHandlerBuffer để thấy JSON lỗi
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