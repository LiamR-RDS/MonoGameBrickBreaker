using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace MonoGameBrickBreaker.Core;

public enum ProceduralSound
{
    Launch,
    PaddleHit,
    WallHit,
    BrickBreak,
    LoseLife,
    Win,
    Lose
}

public sealed class ProceduralSoundEffects : IDisposable
{
    private const int SampleRate = 22050;
    private readonly Dictionary<ProceduralSound, SoundEffect> sounds = new();

    public ProceduralSoundEffects()
    {
        sounds[ProceduralSound.Launch] = CreateSweep(220, 520, 0.16f);
        sounds[ProceduralSound.PaddleHit] = CreateSweep(460, 720, 0.08f);
        sounds[ProceduralSound.WallHit] = CreateTone(280, 0.06f);
        sounds[ProceduralSound.BrickBreak] = CreateTone(760, 0.1f);
        sounds[ProceduralSound.LoseLife] = CreateSweep(420, 100, 0.35f);
        sounds[ProceduralSound.Win] = CreateMelody(new[] { 523f, 659f, 784f }, 0.12f);
        sounds[ProceduralSound.Lose] = CreateSweep(300, 80, 0.45f);
    }

    public void Play(ProceduralSound sound, float pitch = 0f)
    {
        sounds[sound].Play(1f, pitch, 0f);
    }

    public void Dispose()
    {
        foreach (SoundEffect sound in sounds.Values)
            sound.Dispose();
        sounds.Clear();
    }

    private static SoundEffect CreateSweep(float startFrequency, float endFrequency, float duration)
    {
        return CreateWave(duration, (time, _) =>
        {
            float progress = time / (SampleRate * duration);
            float frequency = MathHelper.Lerp(startFrequency, endFrequency, progress);
            return MathF.Sin(2 * MathF.PI * frequency * time / SampleRate);
        });
    }

    private static SoundEffect CreateTone(float frequency, float duration, bool addNoise = false)
    {
        return CreateWave(duration, (time, random) =>
        {
            float tone = MathF.Sin(2 * MathF.PI * frequency * time / SampleRate);
            float noise = addNoise ? (float)(random.NextDouble() * 2 - 1) * (1 - time) : 0;
            return tone * 0.75f + noise * 0.25f;
        });
    }

    private static SoundEffect CreateMelody(float[] frequencies, float noteDuration)
    {
        int samplesPerNote = (int)(SampleRate * noteDuration);
        short[] samples = new short[samplesPerNote * frequencies.Length];
        for (int note = 0; note < frequencies.Length; note++)
        {
            for (int i = 0; i < samplesPerNote; i++)
            {
                float envelope = 1 - i / (float)samplesPerNote;
                float value = MathF.Sin(2 * MathF.PI * frequencies[note] * i / SampleRate) * envelope;
                samples[note * samplesPerNote + i] = ToSample(value);
            }
        }

        return CreateSoundEffect(samples);
    }

    private static SoundEffect CreateWave(float duration, Func<int, Random, float> sampleFactory)
    {
        int sampleCount = (int)(SampleRate * duration);
        short[] samples = new short[sampleCount];
        Random random = new();
        for (int i = 0; i < sampleCount; i++)
        {
            float envelope = MathF.Min(1, i / (SampleRate * 0.01f)) *
                             MathF.Min(1, (sampleCount - i) / (SampleRate * 0.04f));
            samples[i] = ToSample(sampleFactory(i, random) * envelope);
        }

        return CreateSoundEffect(samples);
    }

    private static SoundEffect CreateSoundEffect(short[] samples)
    {
        byte[] buffer = new byte[samples.Length * sizeof(short)];
        Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);
        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static short ToSample(float value)
    {
        return (short)(Math.Clamp(value, -1f, 1f) * short.MaxValue);
    }
}