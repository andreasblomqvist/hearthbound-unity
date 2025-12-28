using System;
using UnityEngine;

namespace Hearthbound.Managers
{
    /// <summary>
    /// Time Manager for day/night cycle
    /// Tracks in-game time and manages lighting
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        #region Singleton
        private static TimeManager _instance;
        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TimeManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TimeManager");
                        _instance = go.AddComponent<TimeManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Time Settings
        [Header("Time Configuration")]
        [SerializeField] private float dayLengthInMinutes = 20f; // Real-time minutes for one full day
        [SerializeField] private float startTimeOfDay = 6f; // Start at 6:00 AM
        
        [Header("Current Time")]
        [SerializeField] private float currentTimeOfDay = 6f; // 0-24 hours
        [SerializeField] private int currentDay = 1;
        
        private float timeMultiplier;
        #endregion

        #region Lighting
        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Gradient lightColorGradient;
        [SerializeField] private AnimationCurve lightIntensityCurve;
        #endregion

        #region Events
        public event Action<int> OnNewDay;
        public event Action<float> OnTimeChanged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                return;

            UpdateTime();
            UpdateLighting();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            currentTimeOfDay = startTimeOfDay;
            
            // Calculate time multiplier (how fast time passes)
            // 24 in-game hours / (dayLengthInMinutes * 60 seconds)
            timeMultiplier = 24f / (dayLengthInMinutes * 60f);

            // Find directional light if not assigned
            if (directionalLight == null)
            {
                directionalLight = FindObjectOfType<Light>();
                if (directionalLight != null && directionalLight.type != LightType.Directional)
                {
                    directionalLight = null;
                }
            }

            // Create default gradients if not set
            if (lightColorGradient == null)
            {
                CreateDefaultGradient();
            }

            if (lightIntensityCurve == null)
            {
                CreateDefaultIntensityCurve();
            }

            Debug.Log($"⏰ Time Manager initialized. Day length: {dayLengthInMinutes} minutes");
        }

        private void CreateDefaultGradient()
        {
            lightColorGradient = new Gradient();
            
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 0.0f);    // Night
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);     // Sunrise
            colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f);        // Day
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);     // Sunset
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 1.0f);    // Night

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            lightColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        private void CreateDefaultIntensityCurve()
        {
            lightIntensityCurve = new AnimationCurve();
            lightIntensityCurve.AddKey(0.0f, 0.1f);   // Night
            lightIntensityCurve.AddKey(0.25f, 0.8f);  // Sunrise
            lightIntensityCurve.AddKey(0.5f, 1.0f);   // Day
            lightIntensityCurve.AddKey(0.75f, 0.8f);  // Sunset
            lightIntensityCurve.AddKey(1.0f, 0.1f);   // Night
        }
        #endregion

        #region Time Update
        private void UpdateTime()
        {
            currentTimeOfDay += Time.deltaTime * timeMultiplier;

            if (currentTimeOfDay >= 24f)
            {
                currentTimeOfDay -= 24f;
                currentDay++;
                OnNewDay?.Invoke(currentDay);
                Debug.Log($"🌅 Day {currentDay} has begun");
            }

            OnTimeChanged?.Invoke(currentTimeOfDay);
        }
        #endregion

        #region Lighting Update
        private void UpdateLighting()
        {
            if (directionalLight == null) return;

            // Calculate time as 0-1 value
            float timePercent = currentTimeOfDay / 24f;

            // Update light color
            directionalLight.color = lightColorGradient.Evaluate(timePercent);

            // Update light intensity
            directionalLight.intensity = lightIntensityCurve.Evaluate(timePercent);

            // Rotate light to simulate sun movement
            float sunAngle = (currentTimeOfDay / 24f) * 360f - 90f; // -90 to start at horizon
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        }
        #endregion

        #region Public API
        public float GetCurrentTimeOfDay()
        {
            return currentTimeOfDay;
        }

        public int GetCurrentDay()
        {
            return currentDay;
        }

        public string GetFormattedTime()
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay);
            int minutes = Mathf.FloorToInt((currentTimeOfDay - hours) * 60f);
            return $"{hours:00}:{minutes:00}";
        }

        public string GetTimeOfDayPeriod()
        {
            if (currentTimeOfDay >= 5f && currentTimeOfDay < 12f)
                return "Morning";
            else if (currentTimeOfDay >= 12f && currentTimeOfDay < 17f)
                return "Afternoon";
            else if (currentTimeOfDay >= 17f && currentTimeOfDay < 21f)
                return "Evening";
            else
                return "Night";
        }

        public bool IsNight()
        {
            return currentTimeOfDay < 5f || currentTimeOfDay >= 21f;
        }

        public bool IsDay()
        {
            return currentTimeOfDay >= 6f && currentTimeOfDay < 18f;
        }

        public void SetTimeOfDay(float time)
        {
            currentTimeOfDay = Mathf.Clamp(time, 0f, 24f);
            Debug.Log($"⏰ Time set to {GetFormattedTime()}");
        }

        public void SetDay(int day)
        {
            currentDay = Mathf.Max(1, day);
        }
        #endregion

        #region Debug
        [ContextMenu("Set to Morning")]
        private void SetToMorning() => SetTimeOfDay(6f);

        [ContextMenu("Set to Noon")]
        private void SetToNoon() => SetTimeOfDay(12f);

        [ContextMenu("Set to Evening")]
        private void SetToEvening() => SetTimeOfDay(18f);

        [ContextMenu("Set to Night")]
        private void SetToNight() => SetTimeOfDay(0f);
        #endregion
    }
}
