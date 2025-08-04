# ZimCom

ZimCom is primarily a voice software like Teamspeak or Discord. It is designed for small communities and can be self-hosted.

ZimCom stands out thanks to its simple configuration and setup. Both the server and client are written in C# with .NET and can be configured via JSON files.

All that is required for the server is port forwarding, and both the server and client can be run without installation. (Either with .NET 9 installed or via the self-contained package, where everything is included).
All settings are stored uniformly in the user's roaming folder.
All logs are store directly in the executable folder under the subfolder logs.

## Currently work in progress / done

- [x] Basic Client
- [x] Basic Server
- [x] Channels
- [x] Accessmanagement
- [ ] Chat
- [ ] Voice
- [ ] Screen sharing

## License

ZimCom is licensed under the EUPL-1.2