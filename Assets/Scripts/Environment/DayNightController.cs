using System.Collections.Generic;
using UnityEngine;

namespace VigiFleet.Environment
{
    public class DayNightController : MonoBehaviour
    {
        [Header("Lighting References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private List<Light> harbourFloodlights = new List<Light>();

        [Header("Time Settings")]
        [Range(0f, 24f)]
        [SerializeField] private float timeOfDayHours = 14.0f; // Default 2:00 PM daytime
        [SerializeField] private bool autoAdvanceTime = false;
        [SerializeField] private float dayCycleMinutes = 12.0f;

        [Header("Color Palettes")]
        [SerializeField] private Color daySunColor = new Color(1.0f, 0.96f, 0.90f);
        [SerializeField] private Color duskSunColor = new Color(1.0f, 0.45f, 0.15f);
        [SerializeField] private Color nightSunColor = new Color(0.15f, 0.20f, 0.35f);

        private void Start()
        {
            if (sunLight == null)
            {
                sunLight = RenderSettings.sun;
            }
            FindAllHarbourFloodlights();
            UpdateLighting();
        }

        public void FindAllHarbourFloodlights()
        {
            harbourFloodlights.Clear();
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Spot || (l.type == LightType.Point && l != sunLight))
                {
                    harbourFloodlights.Add(l);
                }
            }
        }

        private void Update()
        {
            if (autoAdvanceTime)
            {
                timeOfDayHours += (Time.deltaTime / (dayCycleMinutes * 60f)) * 24f;
                if (timeOfDayHours >= 24f) timeOfDayHours = 0f;
                UpdateLighting();
            }
        }

        public void SetTimeOfDay(float hours)
        {
            timeOfDayHours = Mathf.Clamp(hours, 0f, 24f);
            UpdateLighting();
        }

        public void ToggleDayNight()
        {
            SetTimeOfDay(timeOfDayHours > 18f || timeOfDayHours < 6f ? 14f : 21f);
        }

        private void UpdateLighting()
        {
            if (sunLight == null) return;

            // Sun rotation around X (pitch) and Y (yaw)
            float sunPitch = Mathf.Lerp(-90f, 270f, timeOfDayHours / 24f);
            sunLight.transform.rotation = Quaternion.Euler(sunPitch, 45f, 0f);

            bool isNight = timeOfDayHours < 6f || timeOfDayHours > 18.5f;
            bool isDusk = (timeOfDayHours >= 17.5f && timeOfDayHours <= 18.5f) || (timeOfDayHours >= 5.5f && timeOfDayHours <= 6.5f);

            if (isNight)
            {
                sunLight.intensity = 0.15f;
                sunLight.color = nightSunColor;
                RenderSettings.ambientLight = new Color(0.10f, 0.12f, 0.18f);
            }
            else if (isDusk)
            {
                sunLight.intensity = 0.85f;
                sunLight.color = duskSunColor;
                RenderSettings.ambientLight = new Color(0.35f, 0.25f, 0.22f);
            }
            else
            {
                sunLight.intensity = 1.35f;
                sunLight.color = daySunColor;
                RenderSettings.ambientLight = new Color(0.40f, 0.44f, 0.48f);
            }

            // Toggle harbour floodlights
            foreach (var fl in harbourFloodlights)
            {
                if (fl != null) fl.enabled = isNight || isDusk;
            }
        }
    }
}
