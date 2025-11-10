![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/hexbyt3/Pkhex.templateregen/total?color=violet)


# PKHeX Template Regenerator

Hey there! 👋 This tool helps keep your PKHeX legality templates up-to-date by automatically pulling the latest data from EventsGallery and PoGoEncTool. No more manual copying or wondering if you have the latest events!
![image](https://github.com/user-attachments/assets/d2f553c1-7308-4070-884d-34656f4b4073)

![PKHeX Template Regenerator](https://img.shields.io/badge/version-2.1.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Windows](https://img.shields.io/badge/platform-Windows-0078D6.svg)

## What Does This Do?

If you're using PKHeX (and let's be honest, if you're here, you probably are), you know that keeping the legality data current can be a bit of a pain. This tool automates that whole process:

1. **Automatically clones and updates** the latest event data from the official EventsGallery repository
2. **Automatically manages** PoGoEncTool (clones, updates, and builds)
3. **Packages everything** into those .pkl files PKHeX needs
4. **Puts them exactly where PKHeX expects them**

All with just one click! Or even better, set it to auto-update and forget about it. 🎉

## Cool Features

### 🎨 **Looks Good, Works Better**
We've gone full dark mode because who doesn't love a sleek UI? Everything's color-coded so you can see what's happening at a glance.

### 🔍 **Smart Repository Detection**
First time using this? Hit that "Auto Detect" button and watch it find your repos like magic. It searches all the usual spots where devs like to keep their code.

### ⚙️ **Flexible Configuration**
- Save different setups as profiles (maybe you have multiple PKHeX installations?)
- Import/export your settings to share with friends
- Everything's validated so you know if something's wrong before you run updates

### 🔄 **Set It and Forget It**
Enable auto-updates and pick how often (1-24 hours). The app runs quietly in your system tray, keeping everything fresh without you lifting a finger.

### 💾 **Automatic Backups**
Because we've all been there - something updates and suddenly nothing works. This tool keeps your last 10 updates backed up, just in case.

### 🏥 **Built-in Diagnostics**
Something not working? Hit the diagnostics button and get a full report of what's going on. Super helpful when asking for help!

### 🤖 **Automatic Repository Management**
No need to manually clone or maintain EventsGallery or PoGoEncTool anymore! The app now:
- **EventsGallery**: Automatically clones and updates from the official repository
- **PoGoEncTool**: Automatically clones, updates, and builds from the official repository
- Updates to the latest commits from the official repos automatically
- Builds executables when needed (for PoGoEncTool)
- Skips unnecessary rebuilds when everything's up-to-date
- Shows commit hash and messages so you know exactly what version you're using

## Getting Started

### What You'll Need

- **Windows 10 or 11** (sorry Mac/Linux folks!)
- **[.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)** - For running the app
- **[.NET SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** - Only needed for auto-building PoGoEncTool (recommended!)
- **Git** - Required for cloning and updating repositories
- The following repo cloned somewhere on your system:
  - [PKHeX-ALL-IN-ONE](https://github.com/bdawg1989/PKHeX-ALL-IN-ONE) or regular PKHeX
  - ~~[EventsGallery](https://github.com/projectpokemon/EventsGallery)~~ - **Auto-managed!** The app handles this now
  - ~~[PoGoEncTool](https://github.com/projectpokemon/PoGoEncTool)~~ - **Auto-managed!** The app handles this now

### Installation

1. Grab the latest release from the [Releases](https://github.com/bdawg1989/PKHeX.TemplateRegen/releases) page
2. Unzip it somewhere convenient
3. Double-click `PKHeX.TemplateRegen.exe`
4. That's it! No installer needed 🚀

## First Time Setup

When you first run the app:

1. **Let it find your PKHeX repo automatically**
   - Click "🔍 Auto Detect"
   - Pick the right PKHeX path from the dropdowns (if it found multiple)
   - Hit OK

2. **Or set it up manually**
   - Click "⚙ Settings"
   - Browse to where you cloned PKHeX
   - Optionally specify where you want EventsGallery and PoGoEncTool cloned (defaults to your repos folder)
   - Make sure "Automatically clone, update, and build PoGoEncTool" is checked (it is by default!)
   - Save your settings

3. **Run your first update**
   - Click "Update Now"
   - The app will auto-clone EventsGallery and PoGoEncTool if needed (takes 2-3 minutes the first time)
   - Watch the pretty progress bars
   - Check the log to see all the files being processed

That's literally it. You're done! 🎊

## Using the App

### The Main Window

The app shows you everything you need to know:

- **Repository Status** - Green checkmarks = good to go, red X's = something needs fixing
- **Update Controls** - Big friendly buttons that do what they say
- **Activity Log** - See exactly what's happening in real-time
- **Status Bar** - Current operation and progress

### Handy Keyboard Shortcuts

Because clicking is so 2023:
- `Ctrl+U` - Update now
- `Ctrl+S` - Open settings
- `Ctrl+D` - Auto detect repos
- `Ctrl+Shift+D` - Run diagnostics
- `Esc` - Hide to system tray

### System Tray

The app lives in your system tray when minimized:
- **Double-click** the icon to bring it back
- **Right-click** for quick actions
- Get notifications when updates complete

## Troubleshooting

### Something Not Working?

1. **Run the diagnostics tool** (`Ctrl+Shift+D`)
   - It checks everything: Git, permissions, repo status
   - Copy the output if you need to ask for help

2. **Common fixes:**
   - Make sure you've actually cloned the repos (not just downloaded ZIPs)
   - Run as administrator if you get permission errors
   - Check that Git is installed and in your PATH

3. **The nuclear option:**
   - Delete `settings.json` and start fresh
   - The app will recreate it with defaults

### FAQ

**Q: Why do I need Git?**  
A: The tool uses Git to pull updates from the repositories. It's like having a direct line to the latest data!

**Q: Can I use this with regular PKHeX?**  
A: The tool is designed for PKHeX-ALL-IN-ONE, but the legality files work with any PKHeX build.

**Q: How often should I update?**  
A: Events don't change that often. Daily updates are probably overkill, but weekly keeps you current.

**Q: Is this safe?**
A: Absolutely! It only touches the legality data files, never your save files or Pokemon data.

**Q: Do I need to manually clone EventsGallery or PoGoEncTool anymore?**
A: Nope! The app automatically clones and updates both from their official repositories. Just make sure you have Git installed, and for PoGoEncTool building, the .NET SDK is also required.

**Q: What if I have my own fork of EventsGallery or PoGoEncTool?**
A: For PoGoEncTool, you can disable auto-management in Settings by unchecking "Automatically clone, update, and build PoGoEncTool". Then the app will use your existing fork instead. EventsGallery is always auto-managed from the official repository to ensure legality data stays current.

**Q: Why do I need the .NET SDK now?**
A: The SDK is only needed for auto-building PoGoEncTool. If you disable auto-management and build it yourself, you don't need the SDK.

## For Developers

Want to contribute? Awesome! Here's what you need:

- Visual Studio 2022 or later
- .NET 9.0 SDK
- A good dark theme (just kidding... but not really)

The code's pretty straightforward:
- `Forms/MainForm` - The UI and coordination
- `Core/MGDBPickler` & `Core/PGETPickler` - The file processors
- `Core/RepoUpdater` - Git operations (clone, update, build)
- `Core/ProgramSettings` - Settings management
- `Managers/` - Logging, settings, and backup management

Feel free to open issues, submit PRs, or just say hi!

## Credits

This tool stands on the shoulders of giants:

- **[PKHeX](https://github.com/kwsch/PKHeX)** by Kurt - The whole reason we're here
- **[EventsGallery](https://github.com/projectpokemon/EventsGallery)** - Curated by the awesome Project Pokémon team
- **[PoGoEncTool](https://github.com/projectpokemon/PoGoEncTool)** - For all things Pokémon GO
- **[LibGit2Sharp](https://github.com/libgit2/libgit2sharp)** - Makes Git operations painless
- **[NLog](https://nlog-project.org/)** - Logging that doesn't suck

## License

GPL v3 - Which basically means: use it, share it, improve it, but keep it open source! Check the [LICENSE](LICENSE) file for the legal bits.

## What's New?

### v2.1.0 (Latest)
- **🤖 Automatic EventsGallery Management** - Zero maintenance required!
  - Auto-clones official EventsGallery repository if missing
  - Auto-updates to latest commits from official repo
  - No more need to maintain your own fork
  - Shows commit hash and message after updates
- EventsGallery is now completely auto-managed alongside PoGoEncTool
- Updated UI to show auto-managed status for both repositories
- Simplified first-time setup - only PKHeX path required
- Updated documentation to reflect auto-management changes

### v2.0.3
- **🤖 Automatic PoGoEncTool Management** - No more manual cloning or building!
  - Auto-clones official PoGoEncTool repository if missing
  - Auto-updates to latest commits from official repo
  - Auto-builds with dotnet CLI when updates detected
  - Smart rebuild skip when executable is current
  - Shows commit hash and message after updates
- Added "Automatically clone, update, and build PoGoEncTool" setting (enabled by default)
- Improved UX with better status indicators and progress messages
- Better error messages with helpful hints (e.g., .NET SDK download link)
- Fixed all 19 compiler warnings for cleaner builds

### v2.0.2
- Reorganized files into new "Core" folder
- Fetch data directly from PGET to ensure data.json is always up to date
- Remove unused methods

### v2.0.1
- Fixed those annoying repository detection errors
- Added the super helpful diagnostics tool
- Better error messages that actually tell you what's wrong
- Keyboard shortcuts for power users
- Tooltips everywhere because who doesn't like helpful hints?

### v2.0.0
- Complete rewrite with a shiny new UI
- Dark theme because your eyes deserve better
- Auto-detection so setup is a breeze
- Profile system for the pros
- System tray support for background ninjas
- Automatic backups because we're responsible like that

### v1.0.0
- The humble beginnings - a simple console app that got the job done

---

Got questions? Found a bug? Just want to chat? Open an issue! Happy updating! 🚀✨
