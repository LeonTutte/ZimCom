using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Concentus;
using Concentus.Enums;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.Desktop.Windows;
using Assembly = System.Reflection.Assembly;

namespace ZimCom.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        // Testdata
        Server = DynamicManagerModule.InternalServer;
        User = User.Load() ?? new User("Default User");
        if (Server is not null)
        {
            var defaultChannel = GetDefaultChannel();
            defaultChannel.Participants.Add(User);
            CurrentChannel = defaultChannel;
        }

        SetupAudioDevices();
        SetupEncoderAndDecoder();
        User.IsMuted = true;
        AttachToClientEvents();
    }

    /// <summary>
    /// Provides an instance of the DynamicManagerModuleClientExtras, which acts as a specialized extension of the DynamicManagerModule.
    /// Used in communication with the server and managing dynamic functionality within the application.
    /// </summary>
    public DynamicManagerModuleClientExtras DynamicManagerModule { get; } = new();

    [ObservableProperty] public partial Server? Server { get; set; }
    [ObservableProperty] public partial User User { get; set; }
    [ObservableProperty] public partial Channel? SelectedChannel { get; set; }
    [ObservableProperty] public partial Channel? CurrentChannel { get; set; }
    [ObservableProperty] private partial Channel? PreviousChannel { get; set; }
    [ObservableProperty] public partial string? CurrentChatMessage { get; set; }
    [ObservableProperty] public partial string? AudioInformationText { get; set; }
    [ObservableProperty] public partial bool ChatEnabled { get; set; } = false;
    [ObservableProperty] public partial bool ConnectEnabled { get; set; } = true;
    [ObservableProperty] public partial bool DisconnectEnabled { get; set; } = false;
    [ObservableProperty] public partial bool ChannelExtrasEnabled { get; set; } = false;
    private User ServerUser { get; } = new("Server");
    [ObservableProperty] public partial MMDevice? AudiCaptureDevice { get; set; }
    [ObservableProperty] public partial WasapiCapture? AudioCaptureSource { get; set; }
    [ObservableProperty] public partial MMDevice? AudiPlaybackDevice { get; set; }
    [ObservableProperty] public partial WasapiOut? AudioPlaybackSource { get; set; }
    [ObservableProperty] public partial IOpusEncoder? AudioEncoder { get; set; }
    [ObservableProperty] public partial IOpusDecoder? AudioDecoder { get; set; }
    [ObservableProperty] public partial BufferedWaveProvider? AudioPlaybackBuffer { get; set; }
    [ObservableProperty] public partial WaveFormat? AudioFormat { get; set; }
    [ObservableProperty] public partial float AudioLevel { get; set; } = 0;

    private void SetupEncoderAndDecoder()
    {
        AudioEncoder = OpusCodecFactory.CreateEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        AudioEncoder.Bitrate = 12000; // You can tweak this
        AudioDecoder = OpusCodecFactory.CreateDecoder(48000, 1);
    }

    /// <summary>
    /// Attempts to switch the current user to the selected channel. Ensures the user has sufficient access rights to join
    /// the selected channel and updates the channel's participant list accordingly. If access is denied, a message dialog
    /// is displayed to notify the user.
    /// </summary>
    /// <remarks>
    /// This method first checks if the selected channel is not a title or spacer channel. Then, it verifies the user's
    /// access rights against the selected channel using the dynamic manager module. If access is granted, the method
    /// updates the current and previous channel states and manages the participants' lists. If access is denied,
    /// a message window is displayed indicating the denial.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if no channel has been selected (i.e., SelectedChannel is null) when the method is invoked.
    /// </exception>
    [RelayCommand]
    public void JoinChannel()
    {
        if (SelectedChannel?.TitleChannel is not false || SelectedChannel.SpacerChannel) return;
        if (DynamicManagerModule.CheckUserAgainstChannelStrength(Strength.ChannelAccess, User, SelectedChannel))
        {
            PreviousChannel = CurrentChannel;
            PreviousChannel!.Participants.Remove(User);
            PreviousChannel.CurrentChannel = false;
            SelectedChannel.CurrentChannel = true;
            SelectedChannel.Participants.Add(User);
            CurrentChannel = SelectedChannel;
            ChannelExtrasEnabled = !CurrentChannel.LocalChannel;
            StaticNetClientEvents.UserChangeChannel?.Invoke(this, (User, CurrentChannel.Label));
        }
        else
        {
            var messageWindow = new MessageWindow("Access denied",
                $"You are not permitted to access {SelectedChannel.Label}");
            messageWindow.ShowDialog();
        }
    }

    [RelayCommand]
    private void MuteInput()
    {
        User.IsMuted = !User.IsMuted;
        if (User.IsMuted)
        {
            if (AudiCaptureDevice is null || AudioCaptureSource is null) return;
            AudioCaptureSource?.StopRecording();
            StaticLogModule.LogInformation(
                $"Disabled audio input on {AudiCaptureDevice.FriendlyName} with {AudioCaptureSource!.WaveFormat.SampleRate} on {AudioCaptureSource.WaveFormat.Channels} channels.");
        }
        else
        {
            if (AudiCaptureDevice is null || AudioCaptureSource is null) return;
            AudioCaptureSource?.StartRecording();
            StaticLogModule.LogInformation($"Enabled audio input on {AudiCaptureDevice.FriendlyName}");
        }
    }

    [RelayCommand]
    private void MuteOutput() => User.HasOthersMuted = !User.HasOthersMuted;

    [RelayCommand]
    private void AwayUser()
    {
        User.IsAway = !User.IsAway;
        if (User.IsAway)
        {
            User.IsMuted = true;
            User.HasOthersMuted = true;
        }
        else
        {
            User.IsMuted = false;
            User.HasOthersMuted = false;
        }
    }

    [RelayCommand]
    private void DisconnectFromServer() => DynamicManagerModule.DisconnectFromServer();

    [RelayCommand]
    private static void OpenHelp()
    {
        MessageWindow messageWindow = new("Help",
            $"ZimCom Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}{Environment.NewLine}" +
            $"Build by L. Zimmermann");
        messageWindow.ShowDialog();
    }

    [RelayCommand]
    private static void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenConnect()
    {
        var connectWindow = new ConnectWindow();
        connectWindow.ViewModel.ConnectToAddress += (_, e) =>
        {
            try
            {
                DynamicManagerModule.ConnectToServer(e);
            }
            catch (Exception ex)
            {
                var messageWindow = new MessageWindow("Connect", ex.Message);
                messageWindow.ShowDialog();
            }

            DynamicManagerModule.SendPacketToServer(User.GetPacket()).ConfigureAwait(true);
        };
        connectWindow.ViewModel.CloseWindow += (_, _) => { connectWindow.Close(); };
        connectWindow.ShowDialog();
    }

    private Channel GetDefaultChannel()
    {
        return Server!.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First();
    }

    private void SetupAudioDevices()
    {
        AudioFormat = new WaveFormat(48000, 16, 1); // 48kHz, 16-bit, mono
        AudiCaptureDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        AudioCaptureSource = new WasapiCapture(AudiCaptureDevice)
        {
            WaveFormat = AudioFormat,
            ShareMode = AudioClientShareMode.Shared
        };
        AudiPlaybackDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        AudioPlaybackSource = new WasapiOut(AudiPlaybackDevice, AudioClientShareMode.Shared, false, 32);

        AudioPlaybackBuffer = new BufferedWaveProvider(AudioFormat);
        AudioPlaybackBuffer.BufferDuration = TimeSpan.FromSeconds(5);
        AudioPlaybackBuffer.DiscardOnBufferOverflow = true;

        AudioPlaybackSource.Init(AudioPlaybackBuffer);
        AudioPlaybackSource.Play();

        AudioInformationText = $"{AudioFormat.AverageBytesPerSecond} Bps as {AudioFormat.Encoding}";
    }

    private void AttachToClientEvents()
    {
        StaticNetClientEvents.ConnectedToServer += (_, _) =>
        {
            ConnectEnabled = false;
            DisconnectEnabled = true;
        };
        StaticNetClientEvents.ReceivedServerData += (_, e) =>
        {
            Server = e;
            PreviousChannel = null;
            CurrentChannel = GetDefaultChannel();
            CurrentChannel.Participants.Add(User);
            ChatEnabled = true;
        };
        StaticNetClientEvents.DisconnectedFromServer += (_, _) =>
        {
            var messageWindow = new MessageWindow("Disconnect", "Disconnected from Server!");
            messageWindow.ShowDialog();
            ConnectEnabled = true;
            DisconnectEnabled = false;
        };
        StaticNetClientEvents.SendMessageToServer += (_, e) => { CurrentChannel!.Chat.Add(e); };
        StaticNetClientEvents.ReceivedMessageFromServer += (_, e) =>
        {
            Server?.Channels.First(x => x.Label.Equals(e.ChannelLabel, StringComparison.Ordinal)).Chat.Add(e);
        };
        StaticNetClientEvents.OtherUserChangeChannel += (_, e) =>
        {
            if (e.Item1 is null || e.Item2 is null) return;
            var temp = DynamicManagerModule.FindUserInChannel(e!.Item1);
            if (temp is null) return;
            if (temp.Label != e!.Item2.Label)
                temp.Participants.Remove(temp.Participants.First(x => x.Id.Equals(e.Item1.Id)));

            var serverTemp =
                Server!.Channels.First(x => x.Label.Equals(e.Item2.Label, StringComparison.Ordinal));
            serverTemp.Participants.Add(e.Item1);
            CurrentChannel?.Chat.Add(new ChatMessage(ServerUser, $"{e.Item1.Label} joined Channel",
                CurrentChannel.Label));
        };
        StaticNetClientEvents.ReceivedAudio += (_, e) =>
        {
            var pcmBytes = new byte[e.Length * 2];
            Buffer.BlockCopy(e, 0, pcmBytes, 0, pcmBytes.Length);
            if (User.HasOthersMuted is false) AudioPlaybackBuffer?.AddSamples(pcmBytes, 0, pcmBytes.Length);
        };
        AudioCaptureSource?.DataAvailable += (s, e) =>
        {
            if (AudioEncoder is null) return;
            // Normalize audio
            int sampleCount = e.BytesRecorded / 4; // 4 bytes per float
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = BitConverter.ToSingle(e.Buffer, i * 4);
            }

            // Find peak
            float max = samples.Max(s => Math.Abs(s));
            if (max > 0 && max < 1.0f)
            {
                float gain = 1.0f / max;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= gain;
                }
            }

            // Convert back to byte[] if needed
            byte[] normalizedBuffer = new byte[sampleCount * 4]; // Not used yet
            for (int i = 0; i < sampleCount; i++)
            {
                Array.Copy(BitConverter.GetBytes(samples[i]), 0, normalizedBuffer, i * 4, 4);
            }

            // Process audio buffer
            var pcm = new short[e.BytesRecorded / 2];
            Buffer.BlockCopy(e.Buffer, 0, pcm, 0, e.BytesRecorded);
            var opusBuffer = new byte[262144];
            var encodedLength = AudioEncoder.Encode(pcm, 480, opusBuffer, opusBuffer.Length);
            var voicePacket = opusBuffer.Take(encodedLength).ToArray();

            // Playback audio
            if (User.HasOthersMuted is false) AudioPlaybackBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);

            // build and send UDP package
            var packetBuilder = new DynamicPacketBuilderModule();
            packetBuilder.WriteOperationCode((byte)StaticNetCodes.VoiceCode);
            packetBuilder.WriteCusomBytes(voicePacket);
            if (DynamicManagerModule.Registered)
                DynamicManagerModule.SendPacketToServer(packetBuilder.GetPacketBytes()).ConfigureAwait(false);
            AudioLevel = CalculateAudioLevel(e.Buffer, e.BytesRecorded);
        };
    }

    private static float CalculateAudioLevel(byte[] buffer, int bytesRecorded)
    {
        int samples = bytesRecorded / 2; // assuming 16-bit audio
        double sum = 0;
        for (int i = 0; i < samples; i++)
        {
            short sample = BitConverter.ToInt16(buffer, i * 2);
            sum += sample * sample;
        }

        return (float)Math.Sqrt(sum / samples) / short.MaxValue;
    }

    ~MainViewModel()
    {
        AudioCaptureSource?.Dispose();
    }
}