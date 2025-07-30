# Deteriorate - Multiplayer Survival Game

A multiplayer survival game inspired by Rust, featuring online-only gameplay, base building, PvP combat, sleeper system, and HQM upgrades. Built for mobile devices with touch-optimized controls.

## Features

### Core Gameplay
- **Multiplayer Only**: Online persistent world with up to 100+ players
- **Sleeper System**: Offline players remain as sleeping NPCs that can be looted
- **Resource Gathering**: Collect wood, stone, metal ore, sulfur, and HQM
- **Advanced Building**: Multi-tier building system (Wood → Stone → Metal → HQM)
- **Base Raiding**: Attack and defend player bases with explosives and tools
- **PvP Combat**: Full player vs player combat with weapons and armor
- **Tool Cupboards**: Building authorization and decay prevention
- **Rust-like Survival**: Health, hunger, thirst, radiation, and cold mechanics

### Mobile Optimizations
- **Touch Controls**: Intuitive joystick and tap-based interface optimized for PvP
- **Performance Optimized**: Efficient networking and rendering for mobile
- **Battery Efficient**: Optimized for extended multiplayer sessions
- **Responsive UI**: Scales across different screen sizes
- **Server Browser**: Connect to official and community servers

## Getting Started

### Requirements
- Unity 2022.3 LTS or later
- Android SDK for Android builds
- Xcode for iOS builds

### Installation
1. Clone this repository
2. Open the project in Unity
3. Configure build settings for your target platform
4. Build and deploy to your mobile device

## Controls

### Touch Controls
- **Movement**: Virtual joystick (bottom-left)
- **Camera**: Drag to look around
- **Interact**: Tap on objects
- **Inventory**: Tap inventory button
- **Crafting**: Tap crafting button
- **Building**: Long press for build mode

## Game Systems

### Resource Types
- **Wood**: From trees and fallen logs
- **Stone**: From rocks and stone deposits
- **Metal Ore**: From metal nodes
- **Food**: From hunting animals and gathering plants
- **Water**: From rivers, lakes, and rain collection

### Crafting Recipes
- **Tools**: Hatchet, Pickaxe, Spear
- **Weapons**: Bow, Arrows, Melee weapons
- **Building**: Foundations, Walls, Doors, Roofs
- **Survival**: Campfire, Sleeping bag, Water collector

### Building System
- **Foundation-based**: Start with foundations, build up
- **Modular**: Snap-together building pieces
- **Upgradeable**: Upgrade materials from wood to stone to metal

## Development

### Project Structure
```
Assets/
├── Scripts/          # All C# scripts
├── Prefabs/          # Game object prefabs
├── Materials/        # 3D materials and textures
├── Audio/           # Sound effects and music
├── UI/              # User interface assets
└── Scenes/          # Unity scenes
```

### Key Scripts
- `PlayerController.cs`: Handle player movement and input
- `ResourceManager.cs`: Manage resource collection and storage
- `CraftingSystem.cs`: Handle recipe management and crafting
- `BuildingSystem.cs`: Manage structure placement and building
- `SurvivalManager.cs`: Track hunger, thirst, health, temperature

## License

This project is licensed under the MIT License - see the LICENSE file for details.