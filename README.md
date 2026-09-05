## Tunetoon

Tunetoon is an alternative multi toon launcher for Toontown Rewritten and Corporate Clash. 

Runs on both Windows and MacOS.

<img width="800" height="600" alt="image" src="https://github.com/user-attachments/assets/16dc9259-7506-48af-97ef-0dff5a8bbbb8" />

Download from [Releases](https://github.com/DioExtreme/Tunetoon/releases)

## Main Features
- Login multiple accounts at once (2FA supported)
- Organize accounts in groups
- Game window arrangement for grouped logins
- Game patching for both servers
- Account login status indicator
- End game instances with a click
- Toon select for Corporate Clash
- Accounts are permanently encrypted (with optional password protection)

## Compatibility

Windows 10 or newer is required.

Any Apple Silicon device for MacOS. Macs with Intel processors will not be supported.

A Linux port is currently not in the plans. You can try using Wine/Proton.

## Development
Tunetoon v3 is a full rework and now utilizes Electron, Svelte and Tailwind. The legacy implementation used .NET and WinForms.

The bare minimum commands are:

```
npm install
npm run dev
```

## License
See the LICENSE.txt file.

## AI Disclosure
Beginning with v3, AI tools have been used to guide the code implementation, purely as an exercise to get accustomed to using GenAI tools. Only Fable 5 has been used in order to get acceptable code output. Code output was fully reviewed, hand adjusted when needed, and tested extensively.

The legacy C# versions (v2 or lower) did not utilize AI tools.
