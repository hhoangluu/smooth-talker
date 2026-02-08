using TMPro;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public Transform copObject;
    public Transform playerObject;
    public TMP_Text textResponse;
    string apiKey = "5gKaRbbhqajo4CE0A0PZPettwX2ryPvW";
    private string apiUrl = "https://api.mistral.ai/v1/audio/transcriptions";

    public void CastPlayerTalk(string message) {
        SpeakText(message);
        textResponse.text = message;
        copObject.gameObject.SetActive(false);
        playerObject.gameObject.SetActive(true);
    }

    public void CastCopTalk(string message) {
        SpeakText(message);
        textResponse.text = message;
        copObject.gameObject.SetActive(true);
        playerObject.gameObject.SetActive(false);
    }

    public void OnPressRecord() {
        //Voice Input sẽ thực hiện thu âm khi gọi
        VoiceInput.Instance?.StartRecording();
    }

    public void OnReleaseRecord() {
        // Dữ liệu âm thanh sau khi dừng thu âm
        byte[] wavData = VoiceInput.Instance?.StopRecording();
        // Gửi âm thanh lên Mistral để chuyển thành văn bản, và đi kèm hàm callback sau khi nhận được văn bản
        MistralAI.Instance?.StartTranscription(wavData, CastPlayerTalk);
    }

    public void SpeakText(string text) {
        // Gọi TextToSpeechDeepgram để đọc văn bản
        TextToSpeechDeepgram.Instance?.Speak(text);
    }
}

