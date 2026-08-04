using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class EventService : IEventService
    {
        private readonly IStoreService _storeService;

        private string _currentSessionId;
        private DateTime? _sessionStartTime;

        public EventService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public string CurrentSessionId
        {
            get
            {
                if (string.IsNullOrEmpty(_currentSessionId))
                {
                    _currentSessionId = Guid.NewGuid().ToString();
                }
                return _currentSessionId;
            }
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService?.GetAppState();
                if (s == null || string.IsNullOrEmpty(s.accessToken)) return string.Empty;
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        public async Task<(UserEvent Result, ApiErrorResponse Error)> RecordClientEventAsync(CreateClientEventRequest request)
        {
            if (request == null || request.metadata == null) return (null, null);

            string authHeader = AuthHeader;
            if (string.IsNullOrEmpty(authHeader))
            {
                return (null, null);
            }

            byte[] body = request.ToJsonBody();
            string url = $"{ApiConfig.BaseUrl}/api/v1/events";
            Debug.Log($"[{GetType().Name}] Recording client event: {request.eventType} to {url} with body: {JsonUtility.ToJson(request)}");

            using UnityWebRequest req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Authorization", authHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] RecordClientEventAsync"));
            }

            return (JsonUtility.FromJson<UserEvent>(req.downloadHandler.text), null);
        }

        public async Task TrackSessionStartAsync()
        {
            _currentSessionId = Guid.NewGuid().ToString();
            _sessionStartTime = DateTime.UtcNow;

            var request = new CreateClientEventRequest
            {
                eventType = ClientEventTypes.AppSessionOpened,
                metadata = new ClientEventMetadata
                {
                    sessionId = CurrentSessionId,
                    platform = Application.platform.ToString().ToLower(),
                    appVersion = Application.version
                }
            };

            await RecordClientEventAsync(request);
        }

        public async Task TrackSessionEndAsync()
        {
            if (string.IsNullOrEmpty(_currentSessionId) || !_sessionStartTime.HasValue)
            {
                return;
            }

            int durationSeconds = (int)Math.Max(0, (DateTime.UtcNow - _sessionStartTime.Value).TotalSeconds);

            var request = new CreateClientEventRequest
            {
                eventType = ClientEventTypes.AppSessionEnded,
                metadata = new ClientEventMetadata
                {
                    sessionId = CurrentSessionId,
                    platform = Application.platform.ToString().ToLower(),
                    appVersion = Application.version,
                    durationSeconds = durationSeconds
                }
            };

            await RecordClientEventAsync(request);
        }
    }
}
