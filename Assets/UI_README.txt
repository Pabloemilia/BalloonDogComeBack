BALLOON DOG MODERN UI
=====================

The new interface is generated at runtime by:

  Assets/Scripts/BalloonDogModernUI.cs

It automatically replaces the legacy prototype menu panels when the Game
scene starts. No installer menu or manual scene hookup is required.

Implemented screens and flows:

- Main menu and Play flow
- Run result / game-over screen
- Market with local virtual coins
- Owned-skin selection and equip flow
- Sound, master volume and vibration settings
- Privacy notice
- Pause, resume, restart and return-to-menu flow
- Device safe-area handling

Skin and economy data are stored locally through PlayerPrefs. The first launch
starts with 500 test coins and the Classic skin. Run rewards, purchases,
ownership, selected skin, high score and settings persist between sessions.

The original Figma PNG exports are kept in Assets/UI/FigmaReference as design
source material. Some exports contain Figma component-set bounds, so the live
buttons and controls were recreated as editable Unity UI instead of being used
as flattened screenshots.
