using TMPro;
using UnityEngine;

namespace RealtimePatient
{
    /// <summary>
    /// Optional status display. Delete this script if TextMeshPro is not needed.
    /// </summary>
    public sealed class RealtimeStatusText : MonoBehaviour
    {
        [SerializeField] private OpenAIRealtimeClient realtimeClient;
        [SerializeField] private CtrlPushToTalk pushToTalk;
        [SerializeField] private TMP_Text statusText;

        private string networkStatus = "Starting...";

        private void Reset()
        {
            realtimeClient = FindFirstObjectByType<OpenAIRealtimeClient>();
            pushToTalk = FindFirstObjectByType<CtrlPushToTalk>();
            statusText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (realtimeClient != null)
                realtimeClient.StatusChanged += SetNetworkStatus;
        }

        private void OnDisable()
        {
            if (realtimeClient != null)
                realtimeClient.StatusChanged -= SetNetworkStatus;
        }

        private void Update()
        {
            if (statusText == null)
                return;

            statusText.text =
                pushToTalk != null && pushToTalk.IsRecording
                    ? "RECORDING — release Ctrl to send"
                    : networkStatus;
        }

        private void SetNetworkStatus(string value)
        {
            networkStatus = value;
        }
    }
}
