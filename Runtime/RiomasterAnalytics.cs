using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Riomaster.Analytics
{
    /// <summary>
    /// Main analytics manager. Attach to a GameObject or use RiomasterAnalytics.Instance.
    /// Survives scene loads via DontDestroyOnLoad.
    /// </summary>
    public class RiomasterAnalytics : MonoBehaviour
    {
        private static RiomasterAnalytics _instance;
        public static RiomasterAnalytics Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[RiomasterAnalytics]");
                    _instance = go.AddComponent<RiomasterAnalytics>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Server Configuration")]
        [SerializeField] private string _serverUrl = "http://localhost";
        [SerializeField] private string _apiKey = "";

        [Header("Batching")]
        [SerializeField] private float _flushInterval = 10f;
        [SerializeField] private int _batchSize = 25;
        [SerializeField] private int _maxQueueSize = 500;

        [Header("Debug")]
        [SerializeField] private bool _enableLogging = true;

        private string _playerId = "";
        private string _sessionId = "";
        private string _appVersion = "";
        private string _platform = "";

        private readonly List<AnalyticsEvent> _eventQueue = new();
        private bool _isFlushing;
        private bool _initialized;
        private int _eventsSent;
        private int _eventsDropped;

        /// <summary>Total events successfully sent this session</summary>
        public int EventsSent => _eventsSent;

        /// <summary>Total events dropped (queue overflow) this session</summary>
        public int EventsDropped => _eventsDropped;

        /// <summary>Current queue size</summary>
        public int QueueSize => _eventQueue.Count;

        /// <summary>Whether the SDK is initialized and ready</summary>
        public bool IsInitialized => _initialized;

        /// <summary>Current session ID</summary>
        public string SessionId => _sessionId;

        #region Initialization

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-initialize if API key is set via inspector
            if (!string.IsNullOrEmpty(_apiKey))
            {
                InitializeInternal();
            }
        }

        /// <summary>
        /// Initialize the SDK with server URL and API key.
        /// Call this once at game startup.
        /// </summary>
        public void Initialize(string serverUrl, string apiKey)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _apiKey = apiKey;
            InitializeInternal();
        }

        private void InitializeInternal()
        {
            if (_initialized) return;

            if (string.IsNullOrEmpty(_apiKey))
            {
                LogError("API Key is not set! Call Initialize() first.");
                return;
            }

            _sessionId = Guid.NewGuid().ToString();
            _appVersion = Application.version;
            _platform = GetPlatform();
            _initialized = true;

            StartCoroutine(AutoFlushLoop());

            Log($"Initialized — session: {_sessionId.Substring(0, 8)}..., platform: {_platform}");
        }

        /// <summary>
        /// Set the current player ID. Call after player login/authentication.
        /// For Steam games, use SteamClient.SteamId.ToString().
        /// </summary>
        public void SetPlayerId(string playerId)
        {
            _playerId = playerId ?? "";
            Log($"Player ID set: {_playerId}");
        }

        /// <summary>
        /// Start a new session manually (e.g., after returning from pause).
        /// Normally sessions are created automatically on Initialize().
        /// </summary>
        public void NewSession()
        {
            _sessionId = Guid.NewGuid().ToString();
            Log($"New session: {_sessionId.Substring(0, 8)}...");
        }

        #endregion

        #region Core Track Methods

        /// <summary>Track a simple named event</summary>
        public void Track(string eventName)
        {
            TrackInternal(eventName, "general", null, 0, "");
        }

        /// <summary>Track an event with a category</summary>
        public void Track(string eventName, string category)
        {
            TrackInternal(eventName, category, null, 0, "");
        }

        /// <summary>Track an event with a numeric value</summary>
        public void TrackValue(string eventName, float numericValue)
        {
            TrackInternal(eventName, "general", null, numericValue, "");
        }

        /// <summary>Track an event with a string value</summary>
        public void TrackValue(string eventName, string stringValue)
        {
            TrackInternal(eventName, "general", null, 0, stringValue);
        }

        /// <summary>Track an event with custom properties</summary>
        public void Track(string eventName, Dictionary<string, object> properties)
        {
            TrackInternal(eventName, "general", properties, 0, "");
        }

        /// <summary>Track an event with all parameters</summary>
        public void Track(string eventName, string category,
            Dictionary<string, object> properties,
            float numericValue = 0f, string stringValue = "")
        {
            TrackInternal(eventName, category, properties, numericValue, stringValue);
        }

        private void TrackInternal(string eventName, string category,
            Dictionary<string, object> properties, float numericValue, string stringValue)
        {
            if (!_initialized)
            {
                LogWarning("Not initialized, event dropped. Call Initialize() first.");
                return;
            }

            if (string.IsNullOrEmpty(eventName))
            {
                LogWarning("Event name cannot be empty.");
                return;
            }

            // Queue overflow protection
            if (_eventQueue.Count >= _maxQueueSize)
            {
                _eventsDropped++;
                LogWarning($"Queue full ({_maxQueueSize}), event dropped: {eventName}");
                return;
            }

            var evt = new AnalyticsEvent
            {
                event_name = eventName,
                event_category = category ?? "general",
                player_id = _playerId,
                session_id = _sessionId,
                properties = properties ?? new Dictionary<string, object>(),
                numeric_value = numericValue,
                string_value = stringValue ?? "",
                platform = _platform,
                app_version = _appVersion,
                client_ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            };

            _eventQueue.Add(evt);

            if (_eventQueue.Count >= _batchSize)
            {
                Flush();
            }
        }

        #endregion

        #region Convenience Methods — Session

        /// <summary>Track game/app start</summary>
        public void TrackSessionStart()
        {
            Track("session_start", "session");
        }

        /// <summary>Track game/app end with total duration</summary>
        public void TrackSessionEnd(float durationSeconds = 0f)
        {
            Track("session_end", "session", null, durationSeconds);
        }

        #endregion

        #region Convenience Methods — Progression

        /// <summary>Track level start</summary>
        public void TrackLevelStart(string levelName)
        {
            Track("level_start", "progression", null, 0, levelName);
        }

        /// <summary>Track level complete with time spent</summary>
        public void TrackLevelComplete(string levelName, float timeSeconds = 0f)
        {
            Track("level_complete", "progression", null, timeSeconds, levelName);
        }

        /// <summary>Track level fail</summary>
        public void TrackLevelFail(string levelName, float timeSeconds = 0f)
        {
            Track("level_fail", "progression", null, timeSeconds, levelName);
        }

        #endregion

        #region Convenience Methods — Economy

        /// <summary>Track currency earned</summary>
        public void TrackCurrencyEarned(string currency, float amount, string source = "")
        {
            var props = new Dictionary<string, object>
            {
                { "currency", currency },
                { "source", source },
            };
            Track("currency_earned", "economy", props, amount);
        }

        /// <summary>Track currency spent</summary>
        public void TrackCurrencySpent(string currency, float amount, string item = "")
        {
            var props = new Dictionary<string, object>
            {
                { "currency", currency },
                { "item", item },
            };
            Track("currency_spent", "economy", props, amount);
        }

        /// <summary>Track item acquired</summary>
        public void TrackItemAcquired(string itemId, string itemType = "", string source = "")
        {
            var props = new Dictionary<string, object>
            {
                { "item_id", itemId },
                { "item_type", itemType },
                { "source", source },
            };
            Track("item_acquired", "economy", props);
        }

        #endregion

        #region Convenience Methods — Errors

        /// <summary>Track an error or exception</summary>
        public void TrackError(string errorType, string message)
        {
            var props = new Dictionary<string, object>
            {
                { "error_type", errorType },
                { "message", message.Length > 500 ? message.Substring(0, 500) : message },
            };
            Track("error", "error", props, 0, errorType);
        }

        /// <summary>Track an exception automatically</summary>
        public void TrackException(Exception ex)
        {
            TrackError(ex.GetType().Name, ex.Message);
        }

        #endregion

        #region Convenience Methods — Custom

        /// <summary>Track a UI interaction</summary>
        public void TrackUI(string action, string element = "")
        {
            var props = new Dictionary<string, object>
            {
                { "action", action },
                { "element", element },
            };
            Track("ui_interaction", "ui", props, 0, action);
        }

        /// <summary>Track a tutorial step</summary>
        public void TrackTutorial(string step, bool completed = false)
        {
            var props = new Dictionary<string, object>
            {
                { "step", step },
                { "completed", completed },
            };
            Track("tutorial", "onboarding", props, 0, step);
        }

        #endregion

        #region Flush & Network

        /// <summary>Manually flush all queued events to the server</summary>
        public void Flush()
        {
            if (_eventQueue.Count == 0 || _isFlushing) return;
            StartCoroutine(FlushCoroutine());
        }

        private IEnumerator AutoFlushLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(_flushInterval);
                if (_eventQueue.Count > 0 && !_isFlushing)
                {
                    yield return FlushCoroutine();
                }
            }
        }

        private IEnumerator FlushCoroutine()
        {
            if (_isFlushing || _eventQueue.Count == 0) yield break;
            _isFlushing = true;

            // Take current batch
            var count = Mathf.Min(_eventQueue.Count, _batchSize);
            var batch = new List<AnalyticsEvent>(_eventQueue.GetRange(0, count));
            _eventQueue.RemoveRange(0, count);

            string json = BuildBatchJson(batch);

            var url = $"{_serverUrl}/v1/events";
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-API-Key", _apiKey);
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                _eventsSent += batch.Count;
                Log($"Sent {batch.Count} events (total: {_eventsSent})");
            }
            else
            {
                LogWarning($"Send failed: {request.error} — re-queuing {batch.Count} events");
                _eventQueue.InsertRange(0, batch);
            }

            request.Dispose();
            _isFlushing = false;

            // If there are more events, keep flushing
            if (_eventQueue.Count >= _batchSize)
            {
                StartCoroutine(FlushCoroutine());
            }
        }

        #endregion

        #region JSON Builder

        private string BuildBatchJson(List<AnalyticsEvent> events)
        {
            var sb = new StringBuilder(events.Count * 256);
            sb.Append("{\"events\":[");

            for (int i = 0; i < events.Count; i++)
            {
                if (i > 0) sb.Append(',');
                AppendEventJson(sb, events[i]);
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private void AppendEventJson(StringBuilder sb, AnalyticsEvent evt)
        {
            sb.Append('{');
            AppendJsonString(sb, "event_name", evt.event_name); sb.Append(',');
            AppendJsonString(sb, "event_category", evt.event_category); sb.Append(',');
            AppendJsonString(sb, "player_id", evt.player_id); sb.Append(',');
            AppendJsonString(sb, "session_id", evt.session_id); sb.Append(',');
            sb.Append("\"numeric_value\":").Append(evt.numeric_value.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(',');
            AppendJsonString(sb, "string_value", evt.string_value); sb.Append(',');
            AppendJsonString(sb, "platform", evt.platform); sb.Append(',');
            AppendJsonString(sb, "app_version", evt.app_version); sb.Append(',');
            AppendJsonString(sb, "client_ts", evt.client_ts); sb.Append(',');

            // Properties
            sb.Append("\"properties\":{");
            int propIndex = 0;
            foreach (var kvp in evt.properties)
            {
                if (propIndex > 0) sb.Append(',');
                sb.Append('"').Append(Escape(kvp.Key)).Append("\":");

                if (kvp.Value == null)
                    sb.Append("null");
                else if (kvp.Value is string strVal)
                    sb.Append('"').Append(Escape(strVal)).Append('"');
                else if (kvp.Value is bool boolVal)
                    sb.Append(boolVal ? "true" : "false");
                else if (kvp.Value is float or double or int or long or decimal)
                    sb.Append(Convert.ToString(kvp.Value, System.Globalization.CultureInfo.InvariantCulture));
                else
                    sb.Append('"').Append(Escape(kvp.Value.ToString())).Append('"');

                propIndex++;
            }
            sb.Append('}');

            sb.Append('}');
        }

        private void AppendJsonString(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"").Append(Escape(value ?? "")).Append('"');
        }

        private string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        #endregion

        #region Platform Detection

        private string GetPlatform()
        {
            return Application.platform switch
            {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "windows",
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => "macos",
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor => "linux",
                RuntimePlatform.Android => "android",
                RuntimePlatform.IPhonePlayer => "ios",
                RuntimePlatform.WebGLPlayer => "webgl",
                RuntimePlatform.PS4 => "ps4",
                RuntimePlatform.PS5 => "ps5",
                RuntimePlatform.Switch => "switch",
                RuntimePlatform.XboxOne => "xboxone",
                _ => Application.platform.ToString().ToLower(),
            };
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_enableLogging) Debug.Log($"[RiomasterAnalytics] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableLogging) Debug.LogWarning($"[RiomasterAnalytics] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[RiomasterAnalytics] {message}");
        }

        #endregion

        #region Lifecycle

        private void OnApplicationPause(bool paused)
        {
            if (paused && _initialized) Flush();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _initialized) Flush();
        }

        private void OnApplicationQuit()
        {
            if (!_initialized) return;
            TrackSessionEnd(Time.realtimeSinceStartup);
            Flush();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #endregion
    }

    [Serializable]
    public class AnalyticsEvent
    {
        public string event_name;
        public string event_category;
        public string player_id;
        public string session_id;
        public Dictionary<string, object> properties;
        public float numeric_value;
        public string string_value;
        public string platform;
        public string app_version;
        public string client_ts;
    }
}