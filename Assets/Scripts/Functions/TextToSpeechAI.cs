using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class TextToSpeechAI : MonoBehaviour
{
    public static TextToSpeechAI Instance;
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
    public string apiKey = "AIzaSyCucrSR6H5c0A1QD-iVF2y40740brfWorQ";
    public AudioSource audioSource;

    // Hàm gọi từ bên ngoài
    public void Speak(string text)
    {
        StartCoroutine(RequestGeminiTTS(text));
    }

    IEnumerator RequestGeminiTTS(string text)
    {
        string modelId = "gemini-2.5-flash-preview-tts";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

        // Sử dụng cấu trúc nghiêm ngặt hơn: Chỉ cung cấp văn bản cần đọc
        // Tránh dùng các từ như "Hãy trả lời", "Hãy nói" trong text nếu không cần thiết
        string cleanText = text.Replace("\"", "\\\""); // Bảo vệ dấu nháy kép

        string jsonPayload = "{" +
            "\"contents\": [{\"parts\": [{\"text\": \"" + cleanText + "\"}]}]," +
            "\"generationConfig\": {" +
                "\"response_modalities\": [\"AUDIO\"]," +
                "\"speechConfig\": {" +
                    "\"voiceConfig\": {" +
                        "\"prebuiltVoiceConfig\": {\"voiceName\": \"Puck\"}" +
                    "}" +
                "}" +
            "}" +
        "}";

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Trích xuất Base64 (giống script cũ của bạn)
                string json = www.downloadHandler.text;
                string base64Data = ExtractBase64(json);

                if (!string.IsNullOrEmpty(base64Data))
                {
                    byte[] pcmBytes = Convert.FromBase64String(base64Data);
                    PlayRawPCM(pcmBytes);
                }
            }
            else
            {
                Debug.LogError("Lỗi API: " + www.downloadHandler.text);
            }
        }
    }

    // CHÌA KHÓA: Chuyển Byte PCM sang AudioClip
    void PlayRawPCM(byte[] pcmBytes)
    {
        // 1. Gemini trả về 16-bit PCM => mỗi sample là 2 bytes
        int sampleCount = pcmBytes.Length / 2;
        float[] audioData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Chuyển 2 byte thành 1 số short (16-bit)
            short sample = BitConverter.ToInt16(pcmBytes, i * 2);
            // Chuyển sang float (-1.0f đến 1.0f) để Unity hiểu
            audioData[i] = sample / 32768f;
        }

        // 2. Tạo AudioClip (Gemini TTS thường dùng 24000Hz, Mono)
        AudioClip clip = AudioClip.Create("GeminiVoice", sampleCount, 1, 24000, false);
        clip.SetData(audioData, 0);

        audioSource.clip = clip;
        audioSource.Play();
    }

    private string ExtractBase64(string json)
    {
        try
        {
            string searchTag = "\"data\": \"";
            int start = json.IndexOf(searchTag) + searchTag.Length;
            int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start);
        }
        catch { return null; }
    }
}