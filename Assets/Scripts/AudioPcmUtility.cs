using System;
using UnityEngine;

namespace RealtimePatient
{
    public static class AudioPcmUtility
    {
        public static float[] ToMono(float[] interleaved, int channels)
        {
            if (interleaved == null)
                throw new ArgumentNullException(nameof(interleaved));

            if (channels <= 1)
                return interleaved;

            int frameCount = interleaved.Length / channels;
            float[] mono = new float[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                float sum = 0f;
                int baseIndex = frame * channels;

                for (int channel = 0; channel < channels; channel++)
                    sum += interleaved[baseIndex + channel];

                mono[frame] = sum / channels;
            }

            return mono;
        }

        public static float[] ResampleLinear(
            float[] input,
            int sourceRate,
            int destinationRate)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Length == 0 || sourceRate == destinationRate)
                return input;

            if (sourceRate <= 0 || destinationRate <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(sourceRate),
                    "Sample rates must be positive.");

            // input.Length * destinationRate must not be evaluated as int:
            // it overflows past 89,478 samples (1.86 s at 48 kHz), wraps
            // negative, and the Max clamps the result to a single sample.
            int outputLength = Mathf.Max(
                1,
                (int)System.Math.Round(
                    (double)input.Length *
                    destinationRate /
                    sourceRate));

            float[] output = new float[outputLength];
            float sourceStep = sourceRate / (float)destinationRate;

            for (int i = 0; i < outputLength; i++)
            {
                float sourcePosition = i * sourceStep;
                int left = Mathf.Min(
                    Mathf.FloorToInt(sourcePosition),
                    input.Length - 1);
                int right = Mathf.Min(left + 1, input.Length - 1);
                float fraction = sourcePosition - left;

                output[i] = Mathf.Lerp(
                    input[left],
                    input[right],
                    fraction);
            }

            return output;
        }

        /// <summary>
        /// Loudest absolute sample in the buffer, 0 to 1. A value at or near
        /// zero means the capture device produced no signal, which is not the
        /// same as producing no samples.
        /// </summary>
        public static float PeakAmplitude(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;

            float peak = 0f;

            for (int i = 0; i < samples.Length; i++)
            {
                float magnitude = Mathf.Abs(samples[i]);

                if (magnitude > peak)
                    peak = magnitude;
            }

            return peak;
        }

        public static byte[] FloatToPcm16(float[] samples)
        {
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));

            byte[] output = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                float sample = Mathf.Clamp(samples[i], -1f, 1f);

                short value = sample < 0f
                    ? (short)(sample * 32768f)
                    : (short)(sample * 32767f);

                output[i * 2] = (byte)(value & 0xFF);
                output[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }

            return output;
        }

        public static float[] Pcm16ToFloat(byte[] pcm16)
        {
            if (pcm16 == null)
                throw new ArgumentNullException(nameof(pcm16));

            int sampleCount = pcm16.Length / 2;
            float[] output = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                int low = pcm16[i * 2];
                int high = pcm16[i * 2 + 1];
                short value = (short)(low | (high << 8));
                output[i] = value / 32768f;
            }

            return output;
        }
    }
}
