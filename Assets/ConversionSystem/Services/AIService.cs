using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ConversionSystem.Config;
using ConversionSystem.Data;

namespace ConversionSystem.Services
{
    public class AIService : MonoBehaviour
    {
        public static AIService Instance { get; private set; }

        [Header("AI Provider")]
        public AIProvider SelectedProvider = AIProvider.Gemini;
        public AIServiceConfig GeminiConfig;
        public AIServiceConfig ChatGPTConfig;
        public AIServiceConfig MistralConfig;

        [Header("Personality")]
        public PersonalityConfig Personality;

        [Header("Player")]
        public PlayerType CurrentPlayerType = PlayerType.Default;

        [Header("Game Settings")]
        public int MaxTurns = 3;

        private AIServiceConfig _config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _config = SelectedProvider switch
            {
                AIProvider.Gemini => GeminiConfig,
                AIProvider.ChatGPT => ChatGPTConfig,
                AIProvider.Mistral => MistralConfig,
                _ => GeminiConfig
            };
        }

        public async Task<AIResponseData> SendRequestAsync(AIRequestData request)
        {
            string prompt = BuildPrompt(request);
            string jsonBody = BuildRequestBody(prompt);
            string url = GetApiUrl();

            using var webRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(webRequest);

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"AI Service Error: {webRequest.error}\n{webRequest.downloadHandler.text}");
                return null;
            }

            return ParseResponse(webRequest.downloadHandler.text);
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioBytes)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", audioBytes, "user_speech.wav", "audio/wav");
            form.AddField("model", "voxtral-mini-2602");
            form.AddField("language", "en");

            using var webRequest = UnityWebRequest.Post("https://api.mistral.ai/v1/audio/transcriptions", form);
            webRequest.SetRequestHeader("Authorization", $"Bearer {MistralConfig.ApiKey}");

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Transcription Error: {webRequest.error}");
                return null;
            }

            var response = JsonUtility.FromJson<TranscriptionResponse>(webRequest.downloadHandler.text);
            return response.text;
        }

        #region Private Methods

        private string GetApiUrl()
        {
            return _config.Provider switch
            {
                AIProvider.Gemini => $"https://generativelanguage.googleapis.com/v1beta/models/{_config.ModelName}:generateContent",
                AIProvider.ChatGPT => "https://api.openai.com/v1/chat/completions",
                AIProvider.Mistral => "https://api.mistral.ai/v1/chat/completions",
                _ => throw new NotSupportedException($"Provider {_config.Provider} not supported")
            };
        }

        private void SetAuthHeader(UnityWebRequest webRequest)
        {
            switch (_config.Provider)
            {
                case AIProvider.Gemini:
                    webRequest.SetRequestHeader("x-goog-api-key", _config.ApiKey);
                    break;
                case AIProvider.ChatGPT:
                case AIProvider.Mistral:
                    webRequest.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");
                    break;
            }
        }

        private string BuildPrompt(AIRequestData request)
        {
            var historyText = new StringBuilder();
            foreach (var entry in request.History)
            {
                historyText.AppendLine(entry.ToString());
            }

            bool isFinalTurn = request.CurrentTurn >= request.MaxTurns;
            string finalTurnWarning = isFinalTurn
                ? "\n\nIMPORTANT: This is the FINAL turn. You MUST make a final decision and end conversation now. Your decision MUST be either \"TICKET\" or \"WARNING\". Do NOT use \"PENDING\"."
                : "";

            string prompt = _config.PromptTemplate
                .Replace("{PersonalityDescription}", request.PersonalityDescription ?? "")
                .Replace("{SpecificBehavior}", request.SpecificBehavior ?? "")
                .Replace("{PlayerCharacter}", request.PlayerCharacter ?? "Default")
                .Replace("{RaiseSuspicionTriggers}", request.RaiseSuspicionTriggers ?? "")
                .Replace("{LowerSuspicionTriggers}", request.LowerSuspicionTriggers ?? "")
                .Replace("{Catchphrases}", request.Catchphrases ?? "")
                .Replace("{CurrentTurn}", request.CurrentTurn.ToString())
                .Replace("{MaxTurns}", request.MaxTurns.ToString())
                .Replace("{History}", historyText.ToString())
                .Replace("{PlayerInput}", request.PlayerInput ?? "");

            if (isFinalTurn)
                prompt += finalTurnWarning;

            return prompt;
        }

        private string BuildRequestBody(string prompt)
        {
            string escapedPrompt = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

            return _config.Provider switch
            {
                AIProvider.Gemini => BuildGeminiBody(escapedPrompt),
                AIProvider.ChatGPT => BuildChatGPTBody(escapedPrompt),
                AIProvider.Mistral => BuildMistralBody(escapedPrompt),
                _ => throw new NotSupportedException($"Provider {_config.Provider} not supported")
            };
        }

        private string BuildGeminiBody(string escapedPrompt)
        {
            return $@"{{
    ""contents"": [{{
        ""parts"": [{{
            ""text"": ""{escapedPrompt}""
        }}]
    }}],
    ""generationConfig"": {{
        ""temperature"": {_config.Temperature},
        ""topP"": {_config.TopP},
        ""maxOutputTokens"": {_config.MaxTokens},
        ""responseMimeType"": ""application/json""
    }}
}}";
        }

        private string BuildChatGPTBody(string escapedPrompt)
        {
            return $@"{{
    ""model"": ""{_config.ModelName}"",
    ""messages"": [
        {{
            ""role"": ""system"",
            ""content"": ""You are an NPC in a game. Always respond with valid JSON only.""
        }},
        {{
            ""role"": ""user"",
            ""content"": ""{escapedPrompt}""
        }}
    ],
    ""temperature"": {_config.Temperature},
    ""top_p"": {_config.TopP},
    ""max_tokens"": {_config.MaxTokens},
    ""response_format"": {{ ""type"": ""json_object"" }}
}}";
        }

        private string BuildMistralBody(string escapedPrompt)
        {
            return $@"{{
    ""model"": ""{_config.ModelName}"",
    ""messages"": [
        {{
            ""role"": ""system"",
            ""content"": ""You are an NPC in a game. Always respond with valid JSON only.""
        }},
        {{
            ""role"": ""user"",
            ""content"": ""{escapedPrompt}""
        }}
    ],
    ""temperature"": {_config.Temperature},
    ""top_p"": {_config.TopP},
    ""max_tokens"": {_config.MaxTokens},
    ""response_format"": {{ ""type"": ""json_object"" }}
}}";
        }

        private AIResponseData ParseResponse(string json)
        {
            try
            {
                string content = _config.Provider switch
                {
                    AIProvider.Gemini => ParseGeminiResponse(json),
                    AIProvider.ChatGPT => ParseChatGPTResponse(json),
                    AIProvider.Mistral => ParseChatGPTResponse(json),
                    _ => throw new NotSupportedException($"Provider {_config.Provider} not supported")
                };

                return JsonUtility.FromJson<AIResponseData>(content);
            }
            catch (Exception e)
            {
                Debug.LogError($"Parse Error: {e.Message}\nRaw: {json}");
                return null;
            }
        }

        private string ParseGeminiResponse(string json)
        {
            var wrapper = JsonUtility.FromJson<GeminiResponse>(json);
            return wrapper.candidates[0].content.parts[0].text;
        }

        private string ParseChatGPTResponse(string json)
        {
            var wrapper = JsonUtility.FromJson<ChatGPTResponse>(json);
            return wrapper.choices[0].message.content;
        }

        #endregion

        #region Response Classes

        [Serializable] private class GeminiResponse { public GeminiCandidate[] candidates; }
        [Serializable] private class GeminiCandidate { public GeminiContent content; }
        [Serializable] private class GeminiContent { public GeminiPart[] parts; }
        [Serializable] private class GeminiPart { public string text; }

        [Serializable] private class ChatGPTResponse { public ChatGPTChoice[] choices; }
        [Serializable] private class ChatGPTChoice { public ChatGPTMessage message; }
        [Serializable] private class ChatGPTMessage { public string content; }

        [Serializable] private class TranscriptionResponse { public string text; }

        #endregion
    }
}
