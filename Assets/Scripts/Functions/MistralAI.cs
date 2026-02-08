using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class MistralAI : MonoBehaviour
{
    public static MistralAI Instance;
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
    string apiKey = "5gKaRbbhqajo4CE0A0PZPettwX2ryPvW";
    private string apiUrl = "https://api.mistral.ai/v1/audio/transcriptions";
    public void StartTranscription(byte[] audioBytes, Action<string> callback = null)
    {
        StartCoroutine(UploadAudioRoutine(audioBytes, callback));
    }

    IEnumerator UploadAudioRoutine(byte[] audioBytes, Action<string> callback = null)
    {
        // 1. Chuẩn bị Form dữ liệu
        WWWForm form = new WWWForm();
        // 'file' là field name mà Mistral yêu cầu
        form.AddBinaryData("file", audioBytes, "user_speech.wav", "audio/wav");
        form.AddField("model", "voxtral-mini-2602");
        form.AddField("language", "en");
        // form.AddField("language", "vi"); // Thêm nếu bạn chỉ nói tiếng Việt

        // 2. Tạo Request
        using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
        {
            // THIẾT LẬP HEADER XÁC THỰC
            www.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("Đang gửi lên Mistral...");

            // 3. Gửi và đợi phản hồi
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Lỗi gửi request: " + www.error);
            }
            else
            {
                // 4. NHẬN KẾT QUẢ VỀ
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("JSON nhận về: " + jsonResponse);

                // Chuyển JSON thành Object để lấy text
                MistralResponse responseData = JsonUtility.FromJson<MistralResponse>(jsonResponse);

                string ketQuaDaDich = responseData.text;
                onTextReceived(ketQuaDaDich, callback);
                Debug.Log("<color=cyan>Nội dung thu được: </color>" + ketQuaDaDich);

            }
        }
    }

    void onTextReceived(string text, Action<string> callback = null)
    {

        Debug.Log("Văn bản nhận được từ Mistral: " + text);
        if (callback != null)
        {
            callback.Invoke(text);
        }
    }
}

[System.Serializable]
public class MistralResponse
{
    public string text;
}