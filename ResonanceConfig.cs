using ResoniteModLoader;
using FrooxEngine;
using Elements.Core;

namespace Resonance;

public partial class Resonance : ResoniteMod
{
    [Range(0f, 0.95f)]
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<float> smoothing = 
        new(
            "FFT Smoothing",
            "Controls how smoothly the FFT appears to change", 
            () => 0.35f
        );
    public static float Smoothing => Config!.GetValue(smoothing);
    private static readonly Action<ConfigurationChangedEvent> smoothing_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
        {
            handler.SmoothSpeed = Smoothing;
        }
    };

    [Range(0f, 96f, "0db")]
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<float> noisefloor = 
        new(
            "Noise Floor",
            "Determines the noise floor for the input signal (60db is low enough for most music)",
            () => 60f
        );
    
    public static float NoiseFloor => Config!.GetValue(noisefloor);
    private static readonly Action<ConfigurationChangedEvent> noisefloor_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
        {
            handler.NoiseFloor = NoiseFloor;
        }
    };

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<float> gain =
        new(
            "Gain",
            "Applies a static gain to the FFT signal - useful if the FFT is clipping (does nothing if auto levelling is enabled)",
            () => 0.5f
        );
    public static float Gain => Config!.GetValue(gain);
    private static readonly Action<ConfigurationChangedEvent> gain_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
        {
            handler.NoiseFloor = Gain;
        }
    };

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> autolevel =
        new(
            "Auto Level",
            "When enabled: Automatically manages the gain of the FFT output to always avoid clipping",
            () => true
        );
    
    public static bool AutoLevel => Config!.GetValue(autolevel);
    private static readonly Action<ConfigurationChangedEvent> autolevel_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
        {
            handler.AutoLevel = AutoLevel;
        }
    };

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<float> autolevelspeed =
        new(
            "Auto Level Speed",
            "Factor of smoothing to apply to auto leveling - lower is slower",
            () => 0.001f
        );
    
    public static float AutoLevelSpeed => Config!.GetValue(autolevelspeed); 
    private static readonly Action<ConfigurationChangedEvent> autolevelspeed_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
        {
            handler.AutoLevelSpeed = AutoLevelSpeed;
        }
    };

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> hiresfft =
        new(
            "High-Resolution FFT",
            "Changes the FFT bin width from 2048 to 4096 - useful for expanding detail in the lower ranges (requires stream respawn)",
            () => false
        );

    public static bool HiResFft => Config!.GetValue(hiresfft); 

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> lowlatencyaudio =
        new(
            "Low-Latency Audio",
            "If other people see the FFT desync often, turn this on to reduce audio stream latency (May degrade stream audio quality!!)",
            () => false
        );

    public static bool LowLatencyAudio => Config!.GetValue(lowlatencyaudio);
    private static readonly Action<ConfigurationChangedEvent> lowlatencyaudio_changed = e =>
    {
        foreach (var stream in FFTStreamHandler.FFTDict.Keys)
        {
            var audioStream = stream.Stream.Target;
            if (audioStream != null)
            {
                audioStream.MinimumBufferDelay.Value = LowLatencyAudio ? 0.05f : 0.2f;
                audioStream.BufferSize.Value = LowLatencyAudio ? 12000 : 24000;
            }
        }
    };

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<int> visiblebins =
        new(
            "Visible bins",
            "How many FFT bins are accessible via dynamic variables (don't change unless instructed or are building a visualizer, requires stream respawn)",
            () => 256,
            true
        );
    
    public static int VisibleBins => Config!.GetValue(visiblebins);

    // Advanced keys

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> QUANTIZE_BINS =
        new(
            "Quantize bins",
            "Turning this off will stream all FFT values at full bit-depth (VERY BAD FOR NETWORK, ONLY ENABLE IF YOU ABSOLUTELY NEED TO!!!!)",
            () => true,
            true
        );
    
    public static bool Quantize_Bins => Config!.GetValue(QUANTIZE_BINS);  
    private static readonly Action<ConfigurationChangedEvent> QUANTIZE_BINS_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
            handler.Quantized = Quantize_Bins;
    };

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<int> HIGH_RESOLUTION_FFT_OVERRIDE =
        new(
            "Hi-Res FFT override",
            "Will override the 'High-Resolution FFT' setting (if enabled) and change the FFT width to whatever this setting is instead (requires stream respawn)",
            () => 4096,
            true
        );
    
    public static int High_Resolution_Fft_Override => (int)Math.Pow(2, Math.Ceiling(Math.Log(Config!.GetValue(HIGH_RESOLUTION_FFT_OVERRIDE), 2))); // Ensure a power of 2

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> NORMALIZE_FFT =
        new(
            "FFT Normalization",
            "Enables FFT normalization (false gives you the raw FFT energies, leave true if you want pretty visuals)",
            () => true,
            true
        );
    
    public static bool Normalize_Fft => Config!.GetValue(NORMALIZE_FFT);
    private static readonly Action<ConfigurationChangedEvent> NORMALIZE_FFT_changed = e =>
    {
        foreach (var handler in FFTStreamHandler.FFTDict.Values)
            handler.Normalized = Normalize_Fft;
    };


    private static void HandleChanges(ConfigurationChangedEvent c)
    {
        if (ModConfigurationExtensions.ConfigKeyEvents.TryGetValue(c.Key, out var action))
        {
            Engine.Current.WorldManager.FocusedWorld.RunSynchronously(() =>
            {
                action(c);
            });
        }
    }
}
