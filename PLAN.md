# Game Plan: Yildiz Krampon

## Risk Tasks

### 1. 5v5 Top-Down Football Match AI
- **Why isolated:** Basic steering AI for 9 NPC players + ball physics + collision in a confined pitch is the core technical unknown. Wrong approaches produce jittery, clumping, or ball-tunneling behavior that infects the rest of the build.
- **Approach:** Separate team state machine (ATTACK/DEFEND/IDLE per player). Attacking team: nearest player chases ball, others spread to open positions. Defending team: nearest player pressures ball carrier, others mark open areas. Ball: RigidBody2D with linear damping. Player movement: CharacterBody2D steering toward target with max speed. No full-blown pathfinding — just target + separation force to prevent overlap.
- **Verify:** Ball can be kicked by player (space bar or auto when in range), ball rolls to a stop, NPC players move toward ball and each other without clumping into same cell. Goals detected by Area2D behind each goal line. Match ends after score or timer. Player can switch between walking and sprinting.

### 2. Multi-Scene Day Sequence
- **Why isolated:** 10-step linear sequence with cutscene-style transitions, dialogue boxes, choice menus, and stat state persisting across scene changes is high-coordination surface area. Wrong global state approach (no GameManager singleton) causes resets or crashes on scene changes.
- **Approach:** GameManager autoload singleton persists all stats (Energy, Morale, Fatigue, ShotPower, Sprint, Technique, Overall). SequenceManager autoload tracks which step of the first-day sequence is active and handles scene transitions. Each scene reads from GameManager and writes back on exit. Transitions: simple fade to black using AnimationPlayer on a CanvasLayer overlay.
- **Verify:** Progressing through bedroom → kitchen → neighborhood → pitch → shop → training → end-of-day preserves stat values across all scenes. Choices in one scene (breakfast) visibly affect stats in later scenes (energy bar on pitch).

## Main Build

Build the full first-day sequence: all 10 steps playable from wake-up to end-of-day summary and next-morning teaser.

**Systems:**
- GameManager singleton (Energy, Morale, Fatigue, ShotPower, Sprint, Technique, Overall)
- SequenceManager singleton (day step, scene routing)
- Day/time display (Morning → Afternoon → Evening)
- DialogueSystem (speaker portrait + text box + choice buttons, advances on click/space)
- Stats HUD (persistent energy/morale/fatigue bars during match and training)
- End-of-day summary screen (stats gained, morale bar, fatigue bar, XP events list)
- Training mini-game (shooting drill: click/space at right moment to hit target)

**Scenes:**
1. Bedroom — wake-up cutscene, player sprite in bed, posters and ball visible
2. Kitchen — breakfast choice (bread +15 Energy, protein bar +20 Energy +5 Technique XP)
3. Neighborhood — walk to park, meet Riza Abi, transition to park
4. Park/Pitch — meet Eren and Baran, trigger 5v5 match
5. 5v5 Match — top-down live match (risk task)
6. Post-match — Coach Kemal Hoca dialogue, option to go to shop
7. Shop — buy protein bar (+training XP bonus) or skip
8. Training — shooting drill mini-game
9. End-of-day — stats summary screen
10. Next morning preview — Academy tryout teaser (static scene, fade to black)

**Assets needed:** Procedural placeholder sprites for characters (colored rectangles with directional indicator), tiled grass for pitch, simple colored backgrounds for indoor scenes. Full pixel art sprites after gameplay is verified.

- **Verify:**
  - All 10 first-day steps are reachable in sequence
  - Stats persist correctly across scene transitions (GameManager never resets mid-day)
  - Breakfast choice visibly changes Energy value on subsequent scenes
  - 5v5 match: player can move, kick ball, NPC players move, goals are counted
  - Training drill: timing mechanic works, XP bonus applies if protein bar bought
  - End-of-day screen shows correct final stats
  - Day/time display advances correctly through sequence
  - Dialogue boxes show correct speaker name and text, choices advance correctly
  - No missing resources, no crash on scene transitions
  - Gameplay flow matches game description
  - No visual glitches, clipping, or broken layouts
  - **Presentation proof bundle:** `screenshots/result/1/` with `video.mp4` (450 frames @ 30fps) and raw frame sequence showing the full first-day loop from bedroom through end-of-day summary.
  - Android debug APK: not requested

## Task Status

| # | Task | Status |
|---|------|--------|
| 1 | Scaffold (project.godot, .csproj, scene stubs) | [ ] pending |
| 2 | Risk: 5v5 match scene — ball + movement + AI | [ ] pending |
| 3 | Risk: Multi-scene sequence + GameManager | [ ] pending |
| 4 | Bedroom scene | [ ] pending |
| 5 | Kitchen scene + breakfast choice | [ ] pending |
| 6 | Neighborhood scene + NPC meetings | [ ] pending |
| 7 | Park/Pitch scene + match trigger | [ ] pending |
| 8 | Post-match + Coach Kemal Hoca scene | [ ] pending |
| 9 | Shop scene + item purchase | [ ] pending |
| 10 | Training drill mini-game | [ ] pending |
| 11 | End-of-day summary screen | [ ] pending |
| 12 | Next morning preview scene | [ ] pending |
| 13 | Full integration pass + presentation bundle | [ ] pending |
