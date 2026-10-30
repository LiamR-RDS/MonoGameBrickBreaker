# MonoGame Brick Breaker

A 2D Brick Breaker game built in C# using [MonoGame](https://www.monogame.net/).

---

## About The Project

This project is primarily created as a way to learn MonoGame and game development fundamentals in .NET. It features classic brick breaker mechanics built with procedural geometry rendering, responsive paddle physics, scoring, lives, and cross-platform desktop support.

---

## Getting Started

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download) (or the .NET version matching the project target)
* [MonoGame](https://docs.monogame.net/) prerequisites for your operating system (or an IDE like JetBrains Rider / Visual Studio with .NET support)

### Running the Game

You can run the project directly from the command line or from your IDE.

#### Via .NET CLI

1. Clone or open the repository.
2. From the root directory, run the DesktopGL project:
   ```bash
   dotnet run --project MonoGameBrickBreaker.DesktopGL
   ```

#### Via JetBrains Rider / Visual Studio

1. Open `MonoGameBrickBreaker.sln`.
2. Set `MonoGameBrickBreaker.DesktopGL` as the startup project.
3. Build and run (or press `F5` / `Ctrl+F5`).

---

## Controls

* **Move Paddle:** Left / Right arrow keys, `A` / `D`, Mouse movement, or GamePad left stick
* **Launch Ball / Action:** `Space`, `Enter`, Left mouse click, or GamePad `A` button
* **Main Menu:** `Up` / `Down` and `Enter`, or click a button
* **Level Select:** Arrow keys and `Enter`, or click a level
* **Level Editor:** Arrow keys move the cursor, `Space` toggles a brick, `Enter` saves, and `Page Up` / `Page Down` switches levels
* **Restart / Next Level:** `Space` (on Game Over or Victory screens)
* **Exit:** `Escape` or GamePad `Back` button

## Levels

Levels are stored as readable text files in `Content/Levels` with the `.level` extension. Each file contains six rows of ten characters: `#` is a brick and `.` is empty space. The in-game level editor saves changes back to the selected file.

---

## License & Copyright

* **Source Code:** Licensed under the [GNU General Public License v3.0 (GPLv3)](LICENSE).
* **Game Assets & Branding:** All current and future game assets (including artwork, logos, icons, audio, music, and branding) are **Copyright © 2026. All Rights Reserved.**
