using System.Buffers;
using System.Windows;
using Concentus;
using Concentus.Enums;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Desktop.Modules.Dynamic;

/// <summary>
/// Represents a dynamic audio module that captures and decodes audio using NAudio libraries.
/// </summary>
public class DynamicAudioModule : IDisposable
{
    internal readonly MMDevice AudioCaptureDevice;
    internal readonly WasapiCapture AudioCaptureSource;
    private readonly MMDevice _audiPlaybackDevice;
    private readonly WasapiOut _audioPlaybackSource;
    private readonly IOpusEncoder _audioEncoder;
    private readonly IOpusDecoder _audioDecoder;
    private readonly BufferedWaveProvider _audioPlaybackBuffer;
    internal readonly WaveFormat AudioFormat;
    private readonly int _opusFrameSize;

    /// <summary>
    /// Event triggered when a new audio packet is available after being processed and encoded.
    /// This event provides the encoded audio packet data in byte array format.
    /// </summary>
    public EventHandler<byte[]> PacketAvailable;

    /// <summary>
    /// Occurs when the audio level has been calculated, providing the calculated audio level value as a float.
    /// This event is triggered during audio processing in the dynamic audio module.
    /// </summary>
    public EventHandler<float> AudioLevelCalculated;

    /// <summary>
    /// Represents a dynamic audio module that captures, decodes and encodes audio using NAudio libraries. It also handles the playback of received audio data.
    /// </summary>
    public DynamicAudioModule()
    {
        _opusFrameSize = 960;
        _audioEncoder = OpusCodecFactory.CreateEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        _audioEncoder.Bitrate = 12000;
        _audioEncoder.Complexity = 6;
        _audioEncoder.UseVBR = true;
        _audioDecoder = OpusCodecFactory.CreateDecoder(48000, 1);
        AudioFormat = new WaveFormat(48000, 16, 1); // 48kHz, 16-bit, mono
        AudioCaptureDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        AudioCaptureSource = new WasapiCapture(AudioCaptureDevice);
        AudioCaptureSource.WaveFormat = AudioFormat;
        AudioCaptureSource.ShareMode = AudioClientShareMode.Shared;
        _audiPlaybackDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _audioPlaybackSource = new WasapiOut(_audiPlaybackDevice, AudioClientShareMode.Shared, false, 32);
        _audioPlaybackBuffer = new BufferedWaveProvider(AudioFormat);
        _audioPlaybackBuffer.BufferDuration = TimeSpan.FromSeconds(2);
        _audioPlaybackBuffer.DiscardOnBufferOverflow = true;
        _audioPlaybackSource.Init(new MediaFoundationResampler(_audioPlaybackBuffer, _audiPlaybackDevice.AudioClient.MixFormat) { ResamplerQuality = 60});
        _audioPlaybackSource.Play();
        
        AudioCaptureSource.DataAvailable += async (_, e) =>
        {
            var pcm16 = ConvertBytesToPcm16(e.Buffer.AsSpan(0, e.BytesRecorded), AudioFormat);
            try
            {
                var voicePacket = Encode16BitWithOpus(pcm16);
                PacketAvailable?.Invoke(this, voicePacket);
            }
            catch (Exception ex)
            {
                StaticLogModule.LogError("Error during audio encode", ex);
            }
        };
        StaticNetClientEvents.ReceivedAudio += (_, e) =>
        {
            if (e.Length == 0) return;
            DecodeAndPlay(e);
        };
    }

    /// <summary>
    /// Encodes 16-bit PCM audio data using the Opus codec, splitting it into frames of a predefined size.
    /// </summary>
    /// <param name="pcm">The PCM audio data represented as an array of 16-bit signed integers.</param>
    /// <returns>A byte array containing the last encoded Opus packet, or an empty array if no data was encoded.</returns>
    private byte[] Encode16BitWithOpus(short[] pcm)
    {
        var offset = 0;
        var outBuf = ArrayPool<byte>.Shared.Rent(1276); // max Opus-Paketgröße
        var packets = new List<byte[]>();

        while (offset + _opusFrameSize <= pcm.Length)
        {
            // Frame korrekt aus dem PCM extrahieren
            var frame = new short[_opusFrameSize];
            Array.Copy(pcm, offset, frame, 0, _opusFrameSize);

            var encoded = _audioEncoder.Encode(frame, _opusFrameSize, outBuf, outBuf.Length);
            var packet = new byte[encoded];
            Array.Copy(outBuf, packet, encoded);
            packets.Add(packet);

            offset += _opusFrameSize;
        }
        ArrayPool<byte>.Shared.Return(outBuf);
        return packets.LastOrDefault() ?? [];
    }

    /// <summary>
    /// Decodes an Opus-encoded audio packet and plays the resulting PCM audio through the audio playback buffer.
    /// </summary>
    /// <param name="packet">The Opus-encoded audio packet to decode.</param>
    /// <param name="useFec">Specifies whether to use Forward Error Correction (FEC) when decoding the packet. Default is false.</param>
    private void DecodeAndPlay(byte[] packet, bool useFec = false)
    {
        var pcmBuffer = new short[_opusFrameSize];
        var decodedSamples = _audioDecoder.Decode(new ReadOnlySpan<byte>(packet), new Span<short>(pcmBuffer), _opusFrameSize, decode_fec: useFec);
        if (decodedSamples == 0) return;

        // Konvertiere die dekodierten Shorts (Little Endian) in Bytes
        var byteBuf = new byte[decodedSamples * sizeof(short)];
        Buffer.BlockCopy(pcmBuffer, 0, byteBuf, 0, byteBuf.Length);

        _audioPlaybackBuffer.AddSamples(byteBuf, 0, byteBuf.Length);
    }

    /// <summary>
    /// Konvertiert einen Aufnahmepuffer in 16-bit PCM (short[]), abhängig vom tatsächlichen WaveFormat.
    /// Unterstützt 16-bit PCM und 32-bit IEEE Float.
    /// </summary>
    private static short[] ConvertBytesToPcm16(ReadOnlySpan<byte> buffer, WaveFormat format)
    {
        if (format is { Encoding: WaveFormatEncoding.IeeeFloat, BitsPerSample: 32 })
        {
            if (buffer.Length % sizeof(float) != 0)
                throw new ArgumentException("Float-Pufferlänge ist nicht durch 4 teilbar.", nameof(buffer));

            var floatSamples = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(buffer);
            var result = new short[floatSamples.Length];
            const float int16Max = 32767f;

            for (int i = 0; i < floatSamples.Length; i++)
            {
                var f = floatSamples[i];
                if (float.IsNaN(f) || float.IsInfinity(f))
                    f = 0f;

                var clamped = Math.Clamp(f, -1f, 1f);
                result[i] = unchecked((short)(clamped * int16Max));
            }
            return result;
        }

        if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
        {
            if (buffer.Length % sizeof(short) != 0)
                throw new ArgumentException("PCM16-Pufferlänge ist nicht durch 2 teilbar.", nameof(buffer));

            var sampleCount = buffer.Length / sizeof(short);
            var result = new short[sampleCount];

            Buffer.BlockCopy(buffer.ToArray(), 0, result, 0, buffer.Length);
            return result;
        }

        throw new NotSupportedException($"Nicht unterstütztes Aufnahmeformat: Encoding={format.Encoding}, Bits={format.BitsPerSample}.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AudioCaptureDevice.Dispose();
        AudioCaptureSource.Dispose();
        _audiPlaybackDevice.Dispose();
        _audioPlaybackSource.Dispose();
        _audioEncoder.Dispose();
        _audioDecoder.Dispose();
        GC.SuppressFinalize(this);
    }
}