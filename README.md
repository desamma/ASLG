# Ashen Light (ASL)
### [Website](https://ashenlightrpg.azurewebsites.net/)

A dynamic 2D action RPG built with **Unity** and **C#**, where players journey through dangerous Sacred Grounds to uncover the mysteries of awakening Monoliths and seal away encroaching darkness.

## Overview

**Ashen Light** is an indie action RPG that combines fast-paced combat with deep character progression. The world has been torn by mysterious rifts that bridge the realm of the living with an otherworldly dimension. As the last hope of humanity, you must explore forgotten sanctuaries, face powerful bosses, and restore balance to the world.

### Story
Before kingdoms rose, the world was protected by **Sacred Grounds**—ancient sanctuaries where powerful **Monoliths** held colossal energy in perfect balance. For centuries, the Royal Army guarded these places, keeping corruption at bay.

Then the sky tore open. Rifts now scar the heavens, bleeding darkness and awakening horrors long sealed away. The Monolith of the Sun has gone silent, and the King has issued a desperate decree. You wake in the ruins of a soldier's camp with no memory, marked by something otherworldly—and your story begins now.

## 🎮 Gameplay Features

### Core Combat System
- **Real-time 2D Combat**: Fast-paced, skill-based action gameplay with responsive controls
- **Multiple Classes**: Play as Knight, Archer, Rogue, or Summoner, each with unique abilities and playstyles
- **Attack Types**: Melee and ranged combat options depending on your chosen class
- **Enemy AI**: Intelligent enemy behaviors with varied attack patterns and movement strategies

### Character Progression
- **Class System**: Each class has distinct stats, abilities, and progression paths
- **Stat System**: Track and upgrade Health, Damage, and other attributes
- **Skill Upgrades**: Enhance your abilities through multiple upgrade tiers
- **Experience-based Growth**: Defeat enemies to level up and improve your character

### Inventory & Equipment
- **Dynamic Inventory System**: Collect and manage items with a grid-based inventory
- **Equipment Management**: Equip gear to enhance your stats and abilities
- **Item Database**: Extensive catalog of weapons, armor, and consumables

### Crafting & Upgrades
- **Crafting System**: Combine materials to create new equipment and items
- **Recipe Database**: Unlock recipes to craft rare and powerful equipment
- **Material Collection**: Gather resources from defeated enemies and exploration

### Quest System
- **Quest Tracking**: Follow objectives and complete quests for rewards
- **Quest Objectives**: Multi-step quests with various objectives
- **Quest Rewards**: Earn experience, items, and currency for completion
- **Map Transitions**: Seamlessly travel between different areas via quest triggers

### Economic System
- **Shop System**: Purchase items and equipment from various merchants
- **Currency**: Earn and spend gold through combat and quests
- **NPC Shops**: Interact with NPCs to access unique merchant services

### Boss Encounters
- **Epic Boss Battles**: Face powerful enemies with unique attack patterns
- **Multi-Phase Fights**: Bosses with multiple phases and increasing difficulty
- **Boss-specific Abilities**: Each boss has signature attacks and mechanics
- **Health UI**: Track enemy health during intense battles

## Notable Monsters

The game features a diverse roster of challenging monsters:
- **Argeon Highmayne** (Phases 1 & 2) - A warrior with devastating skills like Decimate, War Surge, and Dual Cast
- **Kaleos Xaan** (Phases 1 & 2) - An arcane spellcaster with blink enhancements and daemonic abilities, accompanied by a dog companion
- **Faie Bloodwing & Kara Winterblade** (Multiple forms) - Twin bosses with coordinated attacks and unique powers
- **Drake Dowager** - A draconic enemy wielding chain lightning
- **Cacophynos** - An agile combatant with complex movement patterns
- **Bringer of Death** - An ominous threat with powerful spells
- **Alter Rexx** - A master of arcane spells
- **Ash Mephyt** - A fiery opponent
- **And many more formidable foes...**

## Technical Features

### Game Architecture
- **State Management**: Robust state machine system for player and enemy states
- **Event System**: Event-driven architecture for responsive game interactions
- **Save/Load System**: Persist player progress with comprehensive save management
- **Session Management**: Track player data across scenes

### UI/UX
- **Dynamic UI System**: Responsive UI with canvas groups and toggles
- **TextMesh Pro Integration**: Professional text rendering and animations
- **Menu System**: Comprehensive main menu with play, continue, and character creation
- **HUD Elements**: Real-time display of health, stats, inventory, and quest progress
- **Lore Intro Screen**: Cinematic introduction with typewriter effects and fade animations

### Audio
- **Sound Effect Manager**: Manage and play sound effects during gameplay
- **Map Audio System**: Context-aware audio that changes based on location
- **Volume Control**: Adjustable audio levels

### Difficulty System
- **AI Difficulty Manager**: Adjust enemy difficulty based on player progression
- **Difficulty Modifiers**: Fine-tune enemy stats and behaviors
- **Growth Profiles**: Define how enemies scale with difficulty

### Utilities
- **Knockback System**: Physical impact effects on characters
- **Animator Parameter Management**: Streamlined animator state control
- **Transform Helpers**: Utility functions for common transform operations
- **Overlap Detection**: Physics-based collision detection

## 🛠️ Development Stack

- **Engine**: Unity
- **Language**: C# (.NET Framework 4.7.1)
- **Version Control**: Git
- 
## 🚀 Getting Started

### Prerequisites
- Unity 2020.3 LTS or later
- .NET Framework 4.7.1+
- Git (for cloning)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/desamma/ASLG.git
cd ASLG
```

2. Open the project in Unity:
   - Launch Unity Hub
   - Click "Open Project"
   - Select the `ASLG` folder
   - Wait for assets to import

3. Open the Main Menu scene:
   - Navigate to `Assets/Scenes/`
   - Double-click the Login scene to load

### Running the Game

1. Press the **Play** button in the Unity Editor
2. Create a new character or continue from a save
3. Begin your adventure!

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Player/              # Player controller, movement, and combat
│   ├── Enemies/             # Boss and enemy AI implementations
│   ├── Inventory/           # Item management and equipment systems
│   ├── Crafting/            # Crafting mechanics and recipes
│   ├── Shop/                # Merchant and economic systems
│   ├── Quest/               # Quest tracking and objectives
│   ├── UI/                  # User interface components
│   ├── Session/             # Game state and session management
│   ├── StatusEffect/        # Status conditions and effects
│   ├── DifficultySystem/    # Difficulty and AI scaling
│   ├── StateMachine/        # State management utilities
│   ├── Utilities/           # Helper functions and extensions
│   ├── AI NPC/              # Non-player character AI and companions
│   └── MainMenu/            # Main menu logic
├── Scenes/                  # Game scenes
├── Prefabs/                 # Reusable game objects
├── Sprites/                 # Graphics and UI assets
└── TextMesh Pro/            # TextMesh Pro resources
```

## 🎮 Controls

### Player Movement & Combat
- **Arrow Keys** or **WASD**: Move character
- **Mouse**: Aim (for ranged classes)
- **Left Click**: Attack
- **F**: Use special ability
- **Spacebar**: Dash/Dodge
- **E**: Interact with NPCs/Objects

### UI Navigation
- **B**: Open Inventory
- **M**: Open Map
- **J**: Open Quest Log
- **ESC**: Pause Game
- **C**: Open Crafting Menu
- **K**: Open Shop (when available)

*Controls may vary based on class selection and current game state*

## Key Systems Explained

### Class System
Each class offers a unique playstyle:

- **Knight**: High health and defense, melee-focused with heavy hits and area attacks
- **Archer**: Ranged attacks with precision, mobility, and projectile-based combat
- **Rogue**: Fast attacks with evasion, stealth mechanics, and critical strikes
- **Summoner**: Support abilities, companion summons, and utility spells

### Combat Mechanics
- **Stat-based Damage**: Your damage scales with character stats and equipment
- **Health Management**: Maintain health through healing items and avoiding damage
- **Status Effects**: Enemies and players can inflict conditions (poison, burn, stun, etc.)
- **Knockback**: Enemies push you when they attack, affecting positioning and strategy
- **Skill Cooldowns**: Manage ability usage with strategic cooldown management

### Progression System
- **Leveling**: Gain experience from defeating enemies to increase your level
- **Skill Trees**: Unlock and upgrade unique abilities specific to your class
- **Equipment Enhancement**: Craft better gear to improve stats and unlock special effects
- **Stat Growth**: Increase Health, Damage, and other attributes through leveling
- **Monolith Powers**: Unlock special abilities from defeated Monolith encounters

### Crafting System
- **Material Gathering**: Collect resources from defeated enemies and exploration
- **Recipe Discovery**: Find and unlock new recipes throughout your journey
- **Item Creation**: Combine materials to craft weapons, armor, and consumables
- **Equipment Modification**: Enhance gear with special properties

## Game Statistics

- **Multiple Classes**: 4 unique playable classes
- **Boss Encounters**: 15+ epic boss battles with unique mechanics
- **Quests**: Diverse quest types and multi-step objectives
- **Enemy Types**: Varied enemy types with unique AI behaviors
- **Items**: Extensive inventory of weapons, armor, and consumables
- **Crafting Recipes**: Dozens of craftable items and equipment pieces

## Known Issues & Limitations

- Single-player experience only
- Online features (NPC companions, cloud saves) require API configuration
- Some difficulty balancing may vary based on class selection
- Performance depends on system specifications

## Authentication & Cloud Features

The game includes optional authentication and cloud save features:
- **Token Manager**: Secure session management
- **API Integration**: Connection to backend services for advanced features
- **Offline Mode**: Full gameplay offline with local saves
- **Companion System**: AI NPC companions that can aid you in battle (optional)

## 🎯 Roadmap

- [X] Basic playable
- [ ] Multiplayer support
- [ ] Additional boss encounters
- [ ] Expanded skill trees and abilities
- [ ] New playable areas and zones
- [ ] Cosmetic customization options
- [ ] Achievement system

## Credits

- **Development**: Desamma, Mazl, NerroHoang, JT-24PPAN, Trangiahuy1477
- **Assets**: Mixed sources
---

**ASLG** is an ongoing project in active development.

For more information, visit: [Website](https://ashenlightrpg.azurewebsites.net/)
