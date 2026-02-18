using UnityEngine;

namespace Riomaster.Analytics
{
    /// <summary>
    /// Auto-initializes RiomasterAnalytics from a ScriptableObject config.
    /// Attach to a GameObject in your first scene.
    /// </summary>
    public class RiomasterAnalyticsAutoInit : MonoBehaviour
    {
        [SerializeField] private RiomasterAnalyticsConfig _config;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[RiomasterAnalytics] Config asset not assigned!");
                return;
            }

            var analytics = RiomasterAnalytics.Instance;
            analytics.Initialize(_config.serverUrl, _config.apiKey);
        }
    }
}