using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealtimePatient
{
    /// <summary>
    /// Hold Left Ctrl or Right Ctrl to record.
    /// Release Ctrl to submit the turn.
    /// </summary>
    public sealed class CtrlPushToTalk : MonoBehaviour
    {
        [SerializeField] private OpenAIRealtimeClient realtimeClient;
        [SerializeField] private PcmStreamPlayer patientAudioPlayer;

        [Header("Microphone")]
        [SerializeField] private string preferredMicrophone = "";
        [SerializeField] private int captureSampleRate = 48000;
        [SerializeField] private int maximumTurnSeconds = 30;

        [Header("Upload")]
        [Tooltip("Milliseconds of PCM audio placed in each WebSocket event.")]
        [SerializeField] private int uploadChunkMilliseconds = 100;

        public bool IsRecording { get; private set; }

        private AudioClip microphoneClip;
        private string activeMicrophone;

        private bool leftCtrlHeld;
        private bool rightCtrlHeld;
        private bool starting;
        private bool submitting;

        private void Reset()
        {
            realtimeClient = FindFirstObjectByType<OpenAIRealtimeClient>();
            patientAudioPlayer = FindFirstObjectByType<PcmStreamPlayer>();
        }

        private void Start()
        {
            PrintAvailableMicrophones();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.leftCtrlKey.wasPressedThisFrame)
            {
                leftCtrlHeld = true;
                TryBeginTurn();
            }

            if (keyboard.rightCtrlKey.wasPressedThisFrame)
            {
                rightCtrlHeld = true;
                TryBeginTurn();
            }

            if (keyboard.leftCtrlKey.wasReleasedThisFrame)
            {
                leftCtrlHeld = false;
                TryEndTurn();
            }

            if (keyboard.rightCtrlKey.wasReleasedThisFrame)
            {
                rightCtrlHeld = false;
                TryEndTurn();
            }
        }

        private bool AnyCtrlHeld => leftCtrlHeld || rightCtrlHeld;

        private async void TryBeginTurn()
        {
            if (IsRecording || starting || submitting)
                return;

            starting = true;

            try
            {
                if (realtimeClient == null || !realtimeClient.IsConnected)
                {
                    Debug.LogWarning(
                        "Realtime patient is not connected yet.");
                    return;
                }

                if (patientAudioPlayer != null &&
                    patientAudioPlayer.IsResponseActive)
                {
                    Debug.LogWarning(
                        "Wait until the patient finishes speaking.");
                    return;
                }

                activeMicrophone = ResolveMicrophone();

                if (string.IsNullOrEmpty(activeMicrophone))
                {
                    Debug.LogError("No microphone is available.");
                    return;
                }

                await realtimeClient.ClearInputAudioAsync();

                // Ctrl may have been released while ClearInputAudioAsync
                // was running.
                if (!AnyCtrlHeld)
                {
                    Debug.Log(
                        "Recording canceled because Ctrl was released.");
                    return;
                }

                microphoneClip = Microphone.Start(
                    activeMicrophone,
                    false,
                    maximumTurnSeconds,
                    captureSampleRate);

                if (microphoneClip == null)
                {
                    Debug.LogError(
                        "Microphone.Start returned null.");
                    return;
                }

                // Wait until the microphone actually starts producing
                // samples. Microphone.Start may not begin immediately.
                bool started = await WaitForMicrophoneAsync(
                    activeMicrophone,
                    1000);

                if (!started)
                {
                    Debug.LogError(
                        "The microphone did not start within one second.");

                    Microphone.End(activeMicrophone);
                    microphoneClip = null;
                    return;
                }

                // The user may have released Ctrl while the microphone
                // was initializing.
                if (!AnyCtrlHeld)
                {
                    Microphone.End(activeMicrophone);
                    microphoneClip = null;

                    Debug.Log(
                        "Recording canceled because Ctrl was released.");
                    return;
                }

                IsRecording = true;

                Debug.Log(
                    $"Recording started with microphone " +
                    $"'{activeMicrophone}'. Release Ctrl to send.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (!string.IsNullOrEmpty(activeMicrophone))
                    Microphone.End(activeMicrophone);

                microphoneClip = null;
                IsRecording = false;
            }
            finally
            {
                starting = false;
            }
        }

        private async void TryEndTurn()
        {
            if (AnyCtrlHeld || !IsRecording || submitting)
                return;

            submitting = true;

            try
            {
                await StopAndSubmitAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                submitting = false;
            }
        }

        private async Task StopAndSubmitAsync()
        {
            int recordedFrames =
                Microphone.GetPosition(activeMicrophone);

            Debug.Log(
                $"Microphone position before stopping: " +
                $"{recordedFrames} frames.");

            Microphone.End(activeMicrophone);
            IsRecording = false;

            if (microphoneClip == null || recordedFrames <= 0)
            {
                Debug.LogWarning(
                    "No microphone samples were recorded.");

                microphoneClip = null;
                return;
            }

            int channels = microphoneClip.channels;
            int sourceSampleRate = microphoneClip.frequency;

            float[] interleaved =
                new float[recordedFrames * channels];

            bool readSucceeded =
                microphoneClip.GetData(interleaved, 0);

            microphoneClip = null;

            if (!readSucceeded)
            {
                Debug.LogError(
                    "Could not read data from the microphone clip.");
                return;
            }

            float[] mono = AudioPcmUtility.ToMono(
                interleaved,
                channels);

            float[] pcm24k = AudioPcmUtility.ResampleLinear(
                mono,
                sourceSampleRate,
                24000);

            byte[] pcm16 =
                AudioPcmUtility.FloatToPcm16(pcm24k);

            // The Realtime API rejects a commit under 100 ms.
            // At 24 kHz mono PCM16: 0.1 * 24000 * 2 = 4800 bytes.
            const int minimumCommitBytes = 4800;

            float peak = AudioPcmUtility.PeakAmplitude(mono);

            Debug.Log(
                $"Recorded frames: {recordedFrames}, " +
                $"channels: {channels}, " +
                $"source rate: {sourceSampleRate}, " +
                $"peak level: {peak:F4}, " +
                $"generated PCM16: {pcm16.Length} bytes.");

            // Silence still commits, and response.create still fires,
            // so the avatar replies to an empty turn. That looks like
            // "it ignores me and talks to itself".
            if (peak < 0.005f)
            {
                Debug.LogWarning(
                    $"Captured audio is effectively silent " +
                    $"(peak {peak:F4}) from microphone " +
                    $"'{activeMicrophone}'. Check that this is the " +
                    $"device you are speaking into.");
            }

            if (pcm16.Length < minimumCommitBytes)
            {
                Debug.LogWarning(
                    $"Recording is too short to submit. " +
                    $"Generated: {pcm16.Length} bytes " +
                    $"({pcm16.Length / 48f:F0} ms). " +
                    $"The API needs at least 100 ms " +
                    $"({minimumCommitBytes} bytes). " +
                    $"Hold Ctrl a little longer.");

                await realtimeClient.ClearInputAudioAsync();
                return;
            }

            // PCM16 mono at 24 kHz:
            // 24 samples/ms × 2 bytes/sample.
            int chunkBytes = Mathf.Max(
                minimumCommitBytes,
                uploadChunkMilliseconds * 24 * 2);

            // Keep chunks aligned to complete 16-bit samples.
            if ((chunkBytes & 1) != 0)
                chunkBytes++;

            for (int offset = 0;
                 offset < pcm16.Length;
                 offset += chunkBytes)
            {
                int count = Math.Min(
                    chunkBytes,
                    pcm16.Length - offset);

                byte[] chunk = new byte[count];

                Buffer.BlockCopy(
                    pcm16,
                    offset,
                    chunk,
                    0,
                    count);

                await realtimeClient.AppendInputAudioAsync(chunk);
            }

            bool committed =
                await realtimeClient.CommitInputAudioAsync();

            if (!committed)
            {
                Debug.LogWarning(
                    "Audio was not committed. Response creation was canceled.");

                await realtimeClient.ClearInputAudioAsync();
                return;
            }

            await realtimeClient.CreateResponseAsync();

            Debug.Log(
                "Provider turn sent. Waiting for patient.");
        }

        private static async Task<bool> WaitForMicrophoneAsync(
            string microphoneName,
            int timeoutMilliseconds)
        {
            float startTime = Time.realtimeSinceStartup;

            while (Microphone.GetPosition(microphoneName) <= 0)
            {
                float elapsedMilliseconds =
                    (Time.realtimeSinceStartup - startTime) * 1000f;

                if (elapsedMilliseconds >= timeoutMilliseconds)
                    return false;

                await Task.Yield();
            }

            return true;
        }

        private string ResolveMicrophone()
        {
            string[] devices = Microphone.devices;

            if (devices == null || devices.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(preferredMicrophone))
            {
                foreach (string device in devices)
                {
                    if (string.Equals(
                        device,
                        preferredMicrophone,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return device;
                    }
                }

                Debug.LogWarning(
                    $"Preferred microphone " +
                    $"'{preferredMicrophone}' was not found. " +
                    $"Using '{devices[0]}'.");
            }

            return devices[0];
        }

        private static void PrintAvailableMicrophones()
        {
            string[] devices = Microphone.devices;

            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning(
                    "Unity detected no microphone devices.");
                return;
            }

            Debug.Log(
                $"Unity detected {devices.Length} microphone(s):");

            for (int i = 0; i < devices.Length; i++)
            {
                Microphone.GetDeviceCaps(
                    devices[i],
                    out int minimumFrequency,
                    out int maximumFrequency);

                Debug.Log(
                    $"Microphone [{i}]: '{devices[i]}' | " +
                    $"Frequency range: {minimumFrequency}–" +
                    $"{maximumFrequency} Hz");
            }
        }

        private void OnDisable()
        {
            leftCtrlHeld = false;
            rightCtrlHeld = false;
            starting = false;

            if ((IsRecording || microphoneClip != null) &&
                !string.IsNullOrEmpty(activeMicrophone))
            {
                Microphone.End(activeMicrophone);
            }

            microphoneClip = null;
            IsRecording = false;
        }
    }
}