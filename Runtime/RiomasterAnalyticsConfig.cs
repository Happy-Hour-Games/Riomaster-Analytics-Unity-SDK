using UnityEngine;

namespace Riomaster.Analytics
{
    /// <summary>
    /// ScriptableObject configuration for Riomaster Analytics.
    /// Create via Assets > Create > Riomaster > Analytics Config
    /// </summary>
    [CreateAssetMenu(fileName = "RiomasterAnalyticsConfig", menuName = "Riomaster/Analytics Config")]
    public class RiomasterAnalyticsConfig : ScriptableObject
    {
        [Header("Server")]
        [Tooltip("Analytics server URL (e.g., https://analytics.yourdomain.com)")]
        public string serverUrl = "http://localhost";

        [Tooltip("API Key for your game")]
        public string apiKey = "";

        [Header("Batching")]
        [Tooltip("Seconds between automatic flush")]
        [Range(5f, 60f)]
        public float flushInterval = 10f;

        [Tooltip("Events per batch")]
        [Range(5, 100)]
        public int batchSize = 25;

        [Tooltip("Maximum queued events before dropping")]
        [Range(100, 5000)]
        public int maxQueueSize = 500;

        [Header("Debug")]
        [Tooltip("Enable console logging")]
        public bool enableLogging = true;
    }
}