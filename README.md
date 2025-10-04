[![Qodana](https://github.com/LeonTutte/ZimCom/actions/workflows/qodana_code_quality.yml/badge.svg?branch=main)](https://github.com/LeonTutte/ZimCom/actions/workflows/qodana_code_quality.yml)
[![.NET Server](https://github.com/LeonTutte/ZimCom/actions/workflows/dotnet-server.yml/badge.svg)](https://github.com/LeonTutte/ZimCom/actions/workflows/dotnet-server.yml)
[![.NET Core Desktop](https://github.com/LeonTutte/ZimCom/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/LeonTutte/ZimCom/actions/workflows/dotnet-desktop.yml)

# ZimCom

ZimCom is primarily a voice software like Teamspeak or Discord. It is designed for small communities and can be self-hosted.

ZimCom stands out thanks to its simple configuration and setup. Both the server and client are written in C# with .NET and can be configured via JSON files.

All that is required for the server is port forwarding, and both the server and client can be run without installation. (Either with .NET 9 installed or via the self-contained package, where everything is included).
All settings are stored uniformly in the user's roaming folder.
All logs are stored directly in the executable folder under the subfolder "logs".

## Currently work in progress / done

- [x] Basic Client
- [x] Basic Server
- [x] Channels
- [x] Accessmanagement
- [x] Chat
- [x] Voice
- [ ] Rich Text Chat
- [ ] Screen sharing
- [ ] QUIC Protocol
- [ ] File Upload & Download

## License

ZimCom is licensed under the EUPL-1.2

## Additional Request Features

- [ ] IP Ban combined with user ban
- [ ] Channel description based on RTF
- [ ] Channel banners & channel icon
- [ ] Slots for channels
- [ ] Persistent chat on client
- [ ] Strength for seeing active channel user
- [ ] Strength for modifying channel
- [ ] Server favorites
- [ ] Mark Users as friends via hash of id
