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
    private readonly BufferedWaveProvider _audioPlaybackBuffer;
    internal readonly WaveFormat AudioFormat;

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

    /// <summary>
    /// Represents a dynamic audio module that captures, decodes and encodes audio using NAudio libraries. It also handles the playback of received audio data.
    /// </summary>
    public DynamicAudioModule()
    {
        AudioFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono
        AudioCaptureDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        AudioCaptureSource = new WasapiCapture(AudioCaptureDevice);
        AudioCaptureSource.WaveFormat = AudioFormat;
        AudioCaptureSource.ShareMode = AudioClientShareMode.Shared;
        _audiPlaybackDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _audioPlaybackSource =
            new WasapiOut(_audiPlaybackDevice, AudioClientShareMode.Shared, false, 20); // 20 ms latency
        _audioPlaybackBuffer = new BufferedWaveProvider(AudioFormat);
        _audioPlaybackBuffer.BufferDuration = TimeSpan.FromSeconds(1);
        _audioPlaybackBuffer.DiscardOnBufferOverflow = true;
        _audioPlaybackSource.Init(
            new MediaFoundationResampler(_audioPlaybackBuffer, _audiPlaybackDevice.AudioClient.MixFormat)
                { ResamplerQuality = 60 });
        _audioPlaybackSource.Play();

        AudioCaptureSource.DataAvailable += async (_, e) =>
        {
            //var pcm16 = ConvertBytesToPcm16(e.Buffer.AsSpan(0, e.BytesRecorded), AudioFormat);
            try
            {
                //var voicePacket = Encode16BitWithOpus(pcm16);
                //PacketAvailable?.Invoke(this, voicePacket);
                // Direct Playback for check
                var compressedBuffer = StaticNetCompressor.BrotliCompress(e.Buffer);
                var voicePacket = new DynamicPacketBuilderModule();
                voicePacket.WriteOperationCode((byte)StaticNetCodes.VoiceCode);
                voicePacket.WriteAudioBytes(compressedBuffer, e.BytesRecorded);
                PacketAvailable?.Invoke(this, voicePacket.GetPacketBytes());
                StaticLogModule.LogDebug("Compressd audio from " + e.Buffer.Length + "(" + e.BytesRecorded + ")" + " bytes to " + compressedBuffer.Length + " bytes");
                //var decompressedBuffer = StaticNetCompressor.BrotliDecompress(compressedBuffer);
                //_audioPlaybackBuffer.AddSamples(decompressedBuffer, 0, e.BytesRecorded);
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
            _audioPlaybackBuffer.AddSamples(decompressedBuffer, 0, packetData.Item2);
        };
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