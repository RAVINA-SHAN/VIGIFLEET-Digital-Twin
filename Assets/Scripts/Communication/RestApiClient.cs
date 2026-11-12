using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VigiFleet.Telemetry;

namespace VigiFleet.Communication
{
    /// <summary>
    /// REST API Client connecting to the VIGIFLEET FastAPI backend at http://localhost:8000.
    /// Handles fallback polling, geofence fetching, scenario triggering, and mode toggling.
    /// </summary>
    public class RestApiClient : MonoBehaviour
    {
        [Header("Backend REST Configuration")]
        [SerializeField] private string backendBaseUrl = "http://localhost:8000";

        public string BaseUrl
        {
            get => backendBaseUrl;
            set => backendBaseUrl = value?.TrimEnd('/');
        }

        public async Task<string> GetVehiclesJsonAsync()
        {
            return await SendGetRequestAsync($"{BaseUrl}/api/vehicles");
        }

        public async Task<string> GetGeofencesJsonAsync()
        {
            return await SendGetRequestAsync($"{BaseUrl}/api/geofence");
        }

        public async Task<string> GetCurrentRiskJsonAsync()
        {
            return await SendGetRequestAsync($"{BaseUrl}/api/risk/current");
        }

        public async Task<string> GetEventsJsonAsync(int limit = 50)
        {
            return await SendGetRequestAsync($"{BaseUrl}/api/events?limit={limit}");
        }

        public async Task<bool> SetOperatingModeAsync(string mode)
        {
            string url = $"{BaseUrl}/api/system/mode?mode={UnityWebRequest.EscapeURL(mode)}";
            string res = await SendPostRequestAsync(url, "{}");
            return res != null;
        }

        public async Task<bool> SetScenarioAsync(string scenario)
        {
            string url = $"{BaseUrl}/api/system/scenario?scenario={UnityWebRequest.EscapeURL(scenario)}";
            string res = await SendPostRequestAsync(url, "{}");
            return res != null;
        }

        public async Task<bool> PostTelemetryPacketAsync(ESP32TelemetryPacket packet)
        {
            if (packet == null) return false;
            string json = packet.ToJson();
            string res = await SendPostRequestAsync($"{BaseUrl}/api/telemetry", json);
            return res != null;
        }

        private async Task<string> SendGetRequestAsync(string url)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 4;
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    return req.downloadHandler.text;
                }
                return null;
            }
        }

        private async Task<string> SendPostRequestAsync(string url, string jsonBody)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 4;

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    return req.downloadHandler.text;
                }
                return null;
            }
        }
    }
}
