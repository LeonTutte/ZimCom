using NAudio.CoreAudioApi;
using NAudio.Wave;
using ZimCom.Core.Modules.Dynamic.IO;
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
    internal readonly WaveFormat AudioFormat;
    private double _audioLevel;
    internal double VadThreshold = 0.025;

    /// <summary>
    /// Gets or sets a value that determines whether the audio captured by the module should be played back locally.
    /// When set to <c>true</c>, the module will route the incoming audio stream to the local output device in addition to any remote transmission logic.
    /// Setting this property to <c>false</c> disables local playback, allowing the capture source to operate solely as an input for further processing or network transmission.
    /// </summary>
    public bool LocalPlayback { get; set; }
#pragma warning disable CA1051
    /// <summary>
    /// Event triggered when a new audio packet is available after being processed and encoded.
    /// This event provides the encoded audio packet data in byte array format.
    /// </summary>
    public EventHandler<byte[]>? PacketAvailable;

    /// <summary>
    /// Occurs when the audio level has been calculated, providing the calculated audio level value as a float.
    /// This event is triggered during audio processing in the dynamic audio module.
    /// </summary>
    public EventHandler<float>? AudioLevelCalculated;
#pragma warning restore CA1051

    /// <summary>
    /// Represents a dynamic audio module that captures, decodes and encodes audio using NAudio libraries. It also handles the playback of received audio data.
    /// </summary>
    public DynamicAudioModule()
    {
        AudioFormat = new WaveFormat(48000, 16, 1); // 48kHz, 16-bit, mono
        AudioCaptureDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        AudioCaptureSource = new WasapiCapture(AudioCaptureDevice);
        AudioCaptureSource.WaveFormat = AudioFormat;
        AudioCaptureSource.ShareMode = AudioClientShareMode.Shared;
        _audiPlaybackDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _audioPlaybackSource =
            new WasapiOut(_audiPlaybackDevice, AudioClientShareMode.Shared, false, 20); // 20 ms latency
        var audioPlaybackBuffer = new BufferedWaveProvider(AudioFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(1),
            DiscardOnBufferOverflow = true
        };
        _audioPlaybackSource.Init(
            new MediaFoundationResampler(audioPlaybackBuffer, _audiPlaybackDevice.AudioClient.MixFormat)
                { ResamplerQuality = 60 });
        _audioPlaybackSource.Play();

        AudioCaptureSource.DataAvailable += (_, e) =>
        {
            if (!IsVoiceActive(e.Buffer, e.BytesRecorded))
            {
                return;
            }
            try
            {
                if (LocalPlayback)
                {
                    audioPlaybackBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                }
                var compressedBuffer = StaticNetCompressor.BrotliCompress(e.Buffer);
                var voicePacket = new DynamicPacketBuilderModule();
                voicePacket.WriteOperationCode((byte)StaticNetCodes.VoiceCode);
                voicePacket.WriteAudioBytes(compressedBuffer, e.BytesRecorded);
                PacketAvailable?.Invoke(this, voicePacket.GetPacketBytes());
                StaticLogModule.LogDebug("Compressd audio from " + e.Buffer.Length + "(" + e.BytesRecorded + ")" + " bytes to " + compressedBuffer.Length + " bytes");
            }
            catch (Exception ex)
            {
                StaticLogModule.LogError("Error during audio encode", ex);
            }
        };
        StaticNetClientEvents.ReceivedAudio += (_, e) =>
        {
            if (e.Length == 0) return;
            // Later decode and Play
            var packetData = DynamicPacketReaderModule.ReadAudioBytes(e);
            var decompressedBuffer = StaticNetCompressor.BrotliDecompress(packetData.Item1);
            audioPlaybackBuffer.AddSamples(decompressedBuffer, 0, packetData.Item2);
        };
    }

    /// <summary>
    /// Bestimmt, ob im angegebenen Audio‑Puffer Sprachaktivität vorhanden ist.
    /// </summary>
    /// <param name="buffer">Roh‑Audio‑Bytes (PCM 16‑Bit).</param>
    /// <param name="bytesRecorded">Anzahl der tatsächlich aufgezeichneten Bytes.</param>
    /// <returns>True, wenn Sprache erkannt wurde; sonst False.</returns>
    private bool IsVoiceActive(byte[] buffer, int bytesRecorded)
    {
        // Nur volle Samples berücksichtigen (2 Byte pro Sample bei 16‑Bit PCM)
        int samples = bytesRecorded / 2;
        if (samples == 0) return false;

        double sumSquares = 0.0;
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            // PCM‑Sample als signed Int16 interpretieren
            short sample = BitConverter.ToInt16(buffer, i);
            double normalized = sample / 32768.0; // in [-1, 1]
            sumSquares += normalized * normalized;
        }

        double rms = Math.Sqrt(sumSquares / samples); // Root‑Mean‑Square

        // Wenn RMS über dem Schwellenwert liegt → Sprache
        _audioLevel = rms;
        AudioLevelCalculated?.Invoke(this, (float)_audioLevel);
        return rms > VadThreshold;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AudioCaptureDevice.Dispose();
        AudioCaptureSource.Dispose();
        _audiPlaybackDevice.Dispose();
        _audioPlaybackSource.Dispose();
        GC.SuppressFinalize(this);
    }
}