# Yildiz Krampon

## Dimension: 2D

## Input Actions

| Action | Keys |
|--------|------|
| move_up | W, Up Arrow |
| move_down | S, Down Arrow |
| move_left | A, Left Arrow |
| move_right | D, Right Arrow |
| action | Space, Enter (kick ball / advance dialogue / interact) |
| sprint | Left Shift |

## Autoloads (Singletons)

| Name | File | Role |
|------|------|------|
| GameManager | scripts/GameManager.cs | All player stats, day state, events log |
| SequenceManager | scripts/SequenceManager.cs | Scene transitions with fade, day step tracking |
| DialogueManager | scripts/DialogueManager.cs | Dialogue box + choice menu UI (CanvasLayer layer=50) |

## Scenes

### Bedroom
- **File:** res://scenes/bedroom.tscn
- **Root type:** Node2D
- **Script:** BedroomScene.cs
- **Children:** Background, Bed, Poster1, Poster2, OldBall, PlayerSprite, Title

### Kitchen
- **File:** res://scenes/kitchen.tscn
- **Root type:** Node2D
- **Script:** KitchenScene.cs
- **Children:** Background, Table, Mom, MomLabel, Dad, DadLabel, PlayerSprite, Title

### Neighborhood
- **File:** res://scenes/neighborhood.tscn
- **Root type:** Node2D
- **Script:** NeighborhoodScene.cs
- **Children:** Sky, Ground, RizaShop, ShopSign, RizaAbi, RizaLabel, PlayerSprite, Title

### Match
- **File:** res://scenes/match.tscn
- **Root type:** Node2D (no script on root)
- **Children:**
  - PitchBG (ColorRect)
  - CenterLine, CenterCircle (ColorRect)
  - NetLeft, NetRight (ColorRect)
  - Walls (StaticBody2D, layer 16)
  - GoalOpponent (Area2D, layer 8, %unique, group: goal_red) — ball enters = opponent scored
  - GoalPlayer (Area2D, layer 8, %unique, group: goal_blue) — ball enters = player scored
  - Ball (RigidBody2D, layer 4, script: Football.cs, group: ball)
  - Players (Node2D container):
    - Player (CharacterBody2D, layer 1, script: PlayerController.cs)
    - RedNPC0..3 (CharacterBody2D, layer 2, group: team_red, script: FootballAI.cs)
    - BlueNPC0..4 (CharacterBody2D, layer 2, group: team_blue, script: FootballAI.cs)
  - MatchManager (Node, script: MatchManager.cs)
  - HUD (CanvasLayer, layer 10):
    - HUDControl (Control)
      - ScorePanel (Panel):
        - ScoreLabel (Label, %unique)
        - TimerLabel (Label, %unique)
      - StatsPanel (Panel):
        - EnergyHUD, MoraleHUD, FatigueHUD (Labels)
  - PlayerIndicator (Label)

### PostMatch
- **File:** res://scenes/postmatch.tscn
- **Root type:** Node2D
- **Script:** PostMatchScene.cs

### Shop
- **File:** res://scenes/shop.tscn
- **Root type:** Node2D
- **Script:** ShopScene.cs

### Training
- **File:** res://scenes/training.tscn
- **Root type:** Node2D
- **Script:** TrainingScene.cs
- **Key unique nodes:** %Marker (ColorRect), %TargetZone (ColorRect), %InstructionLabel, %ResultLabel, %ShotsLabel

### EodSummary
- **File:** res://scenes/eod_summary.tscn
- **Root type:** Node2D
- **Script:** EodSummaryScene.cs
- **Key unique nodes:** %EnergyValue, %MoraleValue, %FatigueValue, %ShotValue, %SprintValue, %TechValue, %OverallValue, %EventsLabel, %ContinueLabel

### NextMorning
- **File:** res://scenes/next_morning.tscn
- **Root type:** Node2D
- **Script:** NextMorningScene.cs

## Scripts

### GameManager
- **File:** res://scripts/GameManager.cs
- **Extends:** Node
- **Autoload name:** GameManager
- **State:** Energy, Morale, Fatigue, ShotPower, Sprint, Technique, Overall, DayStep, BoughtProteinBar, WatchedByCoach, MatchGoalsScored, DayEvents list

### SequenceManager
- **File:** res://scripts/SequenceManager.cs
- **Extends:** Node
- **Autoload name:** SequenceManager
- **DayScenes array:** bedroom → kitchen → neighborhood → match → postmatch → shop → training → eod_summary → next_morning
- **Fade overlay:** CanvasLayer layer=100 with black ColorRect

### DialogueManager
- **File:** res://scripts/DialogueManager.cs
- **Extends:** Node
- **Autoload name:** DialogueManager
- **CanvasLayer layer=50:** Panel (bottom-wide, OffsetTop=-180) with speaker label, text label, choices VBox

### PlayerController
- **File:** res://scripts/PlayerController.cs
- **Extends:** CharacterBody2D
- **Attaches to:** Match:Players:Player
- **Reads:** GameManager.Energy for speed modifier
- **Kick:** Space within KickRadius=40px of ball → applies force to ball RigidBody2D

### FootballAI
- **File:** res://scripts/FootballAI.cs
- **Extends:** CharacterBody2D
- **Attaches to:** All NPC CharacterBody2Ds in match scene
- **Team derived from:** IsInGroup("team_red") / IsInGroup("team_blue")
- **Slot derived from:** last char of node name (RedNPC0 → slot 0)

### Football
- **File:** res://scripts/Football.cs
- **Extends:** RigidBody2D
- **Attaches to:** Match:Ball

### MatchManager
- **File:** res://scripts/MatchManager.cs
- **Extends:** Node
- **Attaches to:** Match:MatchManager
- **Signals:** GoalScored(team), MatchEnded(playerTeamScore, opponentScore)
- **Finds via unique name:** %ScoreLabel, %TimerLabel, %GoalPlayer, %GoalOpponent

## Signal Map

- Match:GoalPlayer.BodyEntered → MatchManager._OnGoal(0) [player scored]
- Match:GoalOpponent.BodyEntered → MatchManager._OnGoal(1) [opponent scored]

## Collision Layers

| Layer | Bitmask | Name | Notes |
|-------|---------|------|-------|
| 1 | 1 | player | Player character |
| 2 | 2 | npc | AI players |
| 3 | 4 | ball | Football |
| 4 | 8 | goals | Goal Area2D triggers |
| 5 | 16 | walls | Static pitch boundary |

## Build Order

1. dotnet build
2. scenes/BuildBedroom.cs → scenes/bedroom.tscn
3. scenes/BuildKitchen.cs → scenes/kitchen.tscn
4. scenes/BuildNeighborhood.cs → scenes/neighborhood.tscn
5. scenes/BuildPostMatch.cs → scenes/postmatch.tscn
6. scenes/BuildShop.cs → scenes/shop.tscn
7. scenes/BuildEodSummary.cs → scenes/eod_summary.tscn
8. scenes/BuildNextMorning.cs → scenes/next_morning.tscn
9. scenes/BuildTraining.cs → scenes/training.tscn
10. scenes/BuildMatch.cs → scenes/match.tscn

## Asset Hints

- Player sprite sheet (top-down, 4 directions, 4 walk frames each) ~192x128px
- NPC sprite sheets for red team and blue team, same layout
- Mom, Dad, Riza Abi, Coach Kemal portrait sprites ~48x80px
- Bedroom background (posters, bed, ball) 1280x720
- Kitchen background 1280x720
- Neighborhood street background (Turkish coastal town) 1280x720
- Match pitch top-down view (grass, chalk lines) 1280x720
- Shop interior background 1280x720
- Training pitch (dusk) background 1280x720
- Football sprite (top-down) 20x20px
