![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/hexbyt3/Pkhex.templateregen/total?color=violet)


# PKHeX Template Regenerator

Hey there! 👋 This tool helps keep your PKHeX legality templates up-to-date by automatically pulling the latest data from EventsGallery and PoGoEncTool. No more manual copying or wondering if you have the latest events!
![image](https://github.com/user-attachments/assets/d2f553c1-7308-4070-884d-34656f4b4073)

![PKHeX Template Regenerator](https://img.shields.io/badge/version-2.0.3-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Windows](https://img.shields.io/badge/platform-Windows-0078D6.svg)

## What Does This Do?

If you're using PKHeX (and let's be honest, if you're here, you probably are), you know that keeping the legality data current can be a bit of a pain. This tool automates that whole process:

1. **Pulls the latest event data** from the EventsGallery repository
2. **Grabs encounter data** from PoGoEncTool  
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

### 🤖 **Automatic PoGoEncTool Management** (NEW!)
No need to manually clone or maintain PoGoEncTool anymore! The app now:
- Automatically clones the official PoGoEncTool repository if missing
- Updates to the latest commits from the official repo
- Builds the executable automatically when updates are available
- Skips unnecessary rebuilds when everything's up-to-date
- Shows commit hash and messages so you know exactly what version you're using

## Getting Started

### What You'll Need

- **Windows 10 or 11** (sorry Mac/Linux folks!)
- **[.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)** - For running the app
- **[.NET SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** - Only needed if using auto-build for PoGoEncTool (recommended!)
- **Git** - If you've cloned repos, you're good
- The following repos cloned somewhere on your system:
  - [PKHeX-ALL-IN-ONE](https://github.com/bdawg1989/PKHeX-ALL-IN-ONE)
  - [EventsGallery](https://github.com/projectpokemon/EventsGallery)
  - ~~[PoGoEncTool](https://github.com/projectpokemon/PoGoEncTool)~~ - **Optional!** The app auto-manages this now

### Installation

1. Grab the latest release from the [Releases](https://github.com/bdawg1989/PKHeX.TemplateRegen/releases) page
2. Unzip it somewhere convenient
3. Double-click `PKHeX.TemplateRegen.exe`
4. That's it! No installer needed 🚀

## First Time Setup

When you first run the app:

1. **Let it find your repos automatically**
   - Click "🔍 Auto Detect"
   - Pick the right paths from the dropdowns (if it found multiple)
   - Hit OK

2. **Or set them up manually**
   - Click "⚙ Settings"
   - Browse to where you cloned PKHeX and EventsGallery
   - For PoGoEncTool: Either browse to an existing location or just specify where you want it cloned
   - Make sure "Automatically clone, update, and build PoGoEncTool" is checked (it is by default!)
   - Save your settings

3. **Run your first update**
   - Click "Update Now"
   - The app will auto-clone PoGoEncTool if needed (this takes 1-2 minutes the first time)
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

**Q: Do I need to manually clone PoGoEncTool anymore?**
A: Nope! As of v2.0.3, the app automatically clones, updates, and builds PoGoEncTool from the official repository. Just make sure you have the .NET SDK installed and the "Automatically clone, update, and build PoGoEncTool" checkbox is enabled in settings.

**Q: What if I have my own fork of PoGoEncTool?**
A: You can disable auto-management in Settings by unchecking "Automatically clone, update, and build PoGoEncTool". Then the app will use your existing fork instead.

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

### v2.0.3 (Latest)
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
