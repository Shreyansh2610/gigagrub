using UnityEngine;

namespace GigaGrub.Audio
{
    public static class SoundEffectGenerator
    {
        private static AudioClip cachedEatClip;
        private static AudioClip cachedDeathClip;

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

        public static AudioClip GetOrCreateDeathSoundClip()
        {
            if (cachedDeathClip != null) return cachedDeathClip;

            // Generate a 0.28s deep explosion/pop crunch tone
            int sampleRate = 44100;
            float duration = 0.28f;
            int totalSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(340f, 60f, t * t);
                float phase = 2f * Mathf.PI * currentFreq * (i / (float)sampleRate);

                float attack = Mathf.Clamp01((float)i / (sampleRate * 0.003f));
                float decay = Mathf.Exp(-t * 8f);
                float env = attack * decay;

                // Tone + noise burst
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                float sample = (Mathf.Sin(phase) + noise) * env;

                samples[i] = Mathf.Clamp(sample * 0.7f, -1f, 1f);
            }

            cachedDeathClip = AudioClip.Create("DeathCrunchSound", totalSamples, 1, sampleRate, false);
            cachedDeathClip.SetData(samples, 0);
            return cachedDeathClip;
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

        public static void PlayDeathSound(AudioSource source, float volume = 0.8f)
        {
            if (source == null) return;

            AudioClip clip = GetOrCreateDeathSoundClip();
            if (clip != null)
            {
                source.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
