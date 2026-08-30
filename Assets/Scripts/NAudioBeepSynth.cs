using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using UnityEngine;

public class NAudioBeepSynth : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public static void PlayToneNAudio(double frequency, int durationMs, double volume, string waveType = "Sine")
    {
        NAudio.Wave.SampleProviders.SignalGeneratorType signalGeneratorType = waveType switch
        {
            "Sine" => NAudio.Wave.SampleProviders.SignalGeneratorType.Sin,
            "Square" => NAudio.Wave.SampleProviders.SignalGeneratorType.Square,
            "Triangle" => NAudio.Wave.SampleProviders.SignalGeneratorType.Triangle,
            "Sawtooth" => NAudio.Wave.SampleProviders.SignalGeneratorType.SawTooth,
            "Noise" => NAudio.Wave.SampleProviders.SignalGeneratorType.White,
            _ => NAudio.Wave.SampleProviders.SignalGeneratorType.Sin
        };
        var signalGen = new NAudio.Wave.SampleProviders.SignalGenerator()
        {
            Gain = volume,
            Frequency = frequency,
            Type = signalGeneratorType
        };
        var sampleProvider = signalGen.Take(TimeSpan.FromMilliseconds(durationMs));
        // Convert ISampleProvider to IWaveProvider for DirectSoundOut
        var waveProvider = sampleProvider.ToWaveProvider();
        var output = new NAudio.Wave.DirectSoundOut();
        output.Init(waveProvider);
        output.Play();
        // Wait for duration
        Thread.Sleep(durationMs);
    }
    private int MidiNoteToFrequency(int noteNumber)
    {
        // MIDI Note 69 = 440 Hz (A4)
        return (int)(440.0 * Math.Pow(2, (noteNumber - 69) / 12.0));
    }
}
