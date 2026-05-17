# Memory — Yildiz Krampon

## Toolchain
- Godot: 4.6.2.stable.mono.official.71f334935
- .NET: 9.0.314
- config_version: 5
- SDK: Godot.NET.Sdk/4.6.2
- TargetFramework: net9.0

## Architecture Decisions
- SequenceManager.GoToNextScene() advances through DayScenes array; scenes use index progression not explicit names.
- DialogueManager is always available (autoload CanvasLayer layer=50); scene scripts call it directly.
- GameManager.DayEvents is a List<string> accumulating events for the end-of-day display.
- FootballAI derives team from group membership (team_red/team_blue) set in scene builder before SetScript(); slot from last char of node name.
- Match scene root has NO script; MatchManager is a child Node with the script.
- All NPC collision masks set to 1|2|16 (player|npc|walls); ball mask 1|2|16; goals mask 4 (ball only).
- GravityScale = 0 on all CharacterBody2D and RigidBody2D (2D top-down, no gravity).

## Known Issues / Workarounds
- (none yet — will record during task execution)
