using UnityEngine;
using System;
using System.IO;

public class VoiceInput : MonoBehaviour
{
    public static VoiceInput Instance;
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
    private AudioClip recording;
    private int sampleRate = 16000; // Mistral hoạt động tốt ở 16kHz
    private bool isRecording = false;

    public void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            isRecording = true;
            recording = Microphone.Start(null, false, 10, sampleRate);
            Debug.Log("Đang ghi âm...");
        }
        else
        {
            Debug.LogError("Không tìm thấy Microphone!");
        }
    }

    // Dừng ghi âm và trả về mảng byte (WAV)
    public byte[] StopRecording()
    {
        Debug.Log("Dừng ghi âm.");
        if (!isRecording) return null;

        isRecording = false;
        int lastPos = Microphone.GetPosition(null);
        Microphone.End(null);

        if (lastPos == 0) return null;

        // Cắt bớt phần im lặng thừa nếu ghi âm chưa tới 10s
        float[] samples = new float[lastPos * recording.channels];
        recording.GetData(samples, 0);

        // Chuyển đổi sang file WAV để gửi lên Mistral
        return ConvertToWav(samples, recording.channels, sampleRate);
    }

    private byte[] ConvertToWav(float[] samples, int channels, int rate)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Viết Header của file WAV
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples.Length * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)channels);
                writer.Write(rate);
                writer.Write(rate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16); // Bits per sample
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samples.Length * 2);

                // Viết dữ liệu âm thanh (chuyển float sang short)
                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * 32767f));
                }
            }
            return stream.ToArray();
        }
    }

    // Thêm vào trong class VoiceInput
    public AudioClip GetLastRecording()
    {
        return recording; // Trả về biến AudioClip mà bạn đã dùng trong Microphone.Start
    }
}