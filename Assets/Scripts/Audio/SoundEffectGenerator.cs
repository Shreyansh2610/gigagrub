using UnityEngine;

namespace GigaGrub.Audio
{
    public static class SoundEffectGenerator
    {
        private static AudioClip cachedEatClip;

        public static AudioClip GetOrCreateEatSoundClip()
        {
            if (cachedEatClip != null) return cachedEatClip;

            // Generate a 0.09s crisp, punchy pop/munch tone
            int sampleRate = 44100;
            float duration = 0.09f;
            int totalSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float startFreq = 580f;
            float endFreq = 220f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t * t);
                float phase = 2f * Mathf.PI * currentFreq * (i / (float)sampleRate);

                // Exponential decay envelope with snappy attack
                float attack = Mathf.Clamp01((float)i / (sampleRate * 0.005f));
                float decay = Mathf.Exp(-t * 14f);
                float env = attack * decay;

                float sample = Mathf.Sin(phase) * env;
                // Add warm sub-harmonic overtone
                sample += Mathf.Sin(phase * 0.5f) * env * 0.35f;

                samples[i] = Mathf.Clamp(sample * 0.6f, -1f, 1f);
            }

            cachedEatClip = AudioClip.Create("EatPopSound", totalSamples, 1, sampleRate, false);
            cachedEatClip.SetData(samples, 0);
            return cachedEatClip;
        }

        public static void PlayEatSound(AudioSource source, float volume = 0.7f, float pitchVariation = 0.15f)
        {
            if (source == null) return;

            AudioClip clip = GetOrCreateEatSoundClip();
            if (clip != null)
            {
                source.pitch = UnityEngine.Random.Range(1f - pitchVariation, 1f + pitchVariation);
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
