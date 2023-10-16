﻿using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Reflection;
using FrooxEngine;
using Elements.Core;
using Elements.Assets;
using System.Runtime.Remoting.Messaging;

namespace Resonance;

public class Resonance : ResoniteMod
{
    public override string Name => "Resonance";
    public override string Author => "Cyro";
    public override string Version => "1.0.0";
    public override string Link => "resonite.com";
    public static ModConfiguration? Config;
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<float> Smoothing = new("FFT Smoothing", "Controls how smoothly the FFT appears to change", () => 0.35f);
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<bool> Normalize = new("FFT Normalization", "Controls whether the FFT is normalized or raw", () => true);
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<float> noiseFloor = new("Noise floor", "Determines the noise floor for the input signal", () => 64f);
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<float> logGain = new("Log Gain", "Applies a static gain to the logarithmic signal gain", () => 0.5f);

    public override void OnEngineInit()
    {
        Config = GetConfiguration();
        Config!.Save(true);
        Harmony harmony = new("net.Cyro.Resonance");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(UserAudioStream<StereoSample>))]
    static class UserAudioStreamPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnAwake")]
        public static void OnAwake_Postfix(UserAudioStream<StereoSample> __instance)
        {
            __instance.ReferenceID.ExtractIDs(out _, out byte user);

            if (__instance.LocalUser != __instance.World.GetUserByAllocationID(user))
                return;

            __instance.RunSynchronously(() => {
                var streamHandler = new FFTStreamHandler(__instance, samplingRate: Engine.Current.InputInterface.DefaultAudioInput.SampleRate, fftWidth: CSCore.DSP.FftSize.Fft2048);
                streamHandler.SetupStreams();

                FFTStreamHandler.FFTDict.Add(__instance, streamHandler);
                __instance.Destroyed += d =>
                {
                    if (FFTStreamHandler.FFTDict.TryGetValue(__instance, out FFTStreamHandler handler))
                    {
                        handler.DestroyStreams();
                        FFTStreamHandler.FFTDict.Remove(__instance);
                    }
                };
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnNewAudioData")]
        public static void OnNewAudioData_Postfix(UserAudioStream<StereoSample> __instance, Span<StereoSample> buffer, ref int ___lastDeviceIndex)
        {
            var world = __instance.World;
            if (world.Focus != World.WorldFocus.Focused || __instance.LocalUser.IsSilenced || (ContactsDialog.RecordingVoiceMessage && ___lastDeviceIndex == __instance.InputInterface.DefaultAudioInputIndex))
                return;
            
            if (FFTStreamHandler.FFTDict.TryGetValue(__instance, out FFTStreamHandler handler))
                handler.UpdateFFTData(buffer);
        }
    }
}
