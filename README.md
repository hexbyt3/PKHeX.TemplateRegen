# PKHeX.TemplateRegen
Regenerates legality binaries for PKHeX based on a configured settings.json file.

Usage:
1. Clone PKHeX, PoGoEncTool, and EventsGallery repositories. They can be cloned to the same parent folder, or separate parent folders.
2. Run the executable once to generate the json.
3. Edit the json to point to your repository folders. If they are in the same parent folder, you can use relative paths and just change the RepoFolder property.
4. Run the application.
5. The application will regenerate the .pkl files and copy them to PKHeX's corresponding legality path.
