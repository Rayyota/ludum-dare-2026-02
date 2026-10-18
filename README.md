# ludum-dare-2026-02

## Editable prototype interface

Open `Assets/Scenes/TimingPrototype.unity` in Unity 6000.6.6f1. The interface is
ordinary Canvas/uGUI content saved in the scene and in
`Assets/Prefabs/Timing Prototype UI.prefab`. It is visible and editable before Play;
no UI objects are generated at runtime.

Expand `Timing Prototype > Canvas` in the Hierarchy:

- `Home/Pet`: move and resize the pet with RectTransform. Assign your sprite to
  the Image on Pet for preview. Healthy and broken sprites are assigned on
  TimingMinigame and switch automatically when a care button breaks or is repaired.
- `Home/Lives`: three editable heart graphics above the needs panel at the top right.
- `Home/Pet/CareActions`: circular hit areas over the device buttons. Yellow
  plays, red feeds, and cyan washes. These have no separate visible labels or
  lower button row. They follow the pet position and aspect-fitted sprite size.
  Each restores 8 points to its matching need, capped at 100. Adjust the amounts
  on PetCareActions. Care is unavailable during repairs and after game over.
- `Home/Stats`: Joy, Satiety, Cleanliness, and Breakdown below the hearts on the right.
  Each row has an icon and a read-only Slider with an editable Fill Image; the
  text labels are retained but hidden to match the supplied art.
- `RepairPanel/Puzzle`: the timing minigame. RepairPanel is initially inactive;
  temporarily enable it in the Inspector to edit its contents, then disable it.
- `StartScreen/StartButton`: supplied start menu, visible initially.
- `GameOverScreen/RestartButton`: supplied death screen and retry button.

Select UI objects in Scene view with the Rect tool (T); use 2D view and press F
to frame the selected object. Canvas Scaler uses a 912 x 512 reference resolution. Use a 16:9 Game view for the
supplied screens. Temporarily disable StartScreen in the editor to edit Home.
Changes made during Play are temporary, so edit layout outside Play mode.

Tiny5 is assigned to all text. The static font file and SIL Open Font License are
in `Assets/Fonts`; its source is https://github.com/google/fonts/tree/main/ofl/tiny5.
Buttons use size 24 and meter labels size 16.

## Gameplay

- The game opens the supplied start menu. Press Start to begin with empty needs.
  There is no automatic repair timer. Retry returns straight to gameplay.
- Feed, Wash, and Play each restore 8 points to their corresponding need, capped
  at 100. Satiety, Cleanliness, and Joy decrease by 3, 2, and 2.5 points per second,
  including during repairs. They stop decreasing at zero and pause on game over.
- Each completed care action increases Breakdown by 3 points (maximum 100).
  Breakdown is overall device wear and is not reset by repairing one button.
- Each care button counts its own presses. At 0% Breakdown it breaks on the sixth
  press; at 100% it breaks on the third. Intermediate limits are interpolated and
  checked using the current Breakdown on every press. Increasing wear can
  shorten the remaining life of a button that is already in use.
- A breaking press opens the timing puzzle instead of immediately granting care.
  Three hits repair the button, reset its counter, and complete the interrupted
  action once. A miss or three-second stage timeout costs one heart, grants no
  care, and leaves that button broken/red. Clicking it retries the repair.
  Other buttons remain usable when home. Both outcomes return straight home.
- The repair modal contains only three progress dots, a track, a small time bar,
  and the **Контакт** button. Click the track or button on mouse down. The target
  narrows to 46%, 28%, and 14% across the three stages.
- Losing all three hearts ends the game. Restart resets lives, needs, wear, and
  all button counters. Care is blocked during a repair and after game over.
- Tune care amounts, per-action wear, and press limits on PetCareActions; need
  decay rates and starting values on PetStatus; timing difficulty on TimingMinigame.
  Empty needs do not currently remove hearts. Progress is session-only.

TimingMinigame exposes RequestRepair, StartRepair, and Inspector result events
for future care systems. Keep its Scene UI References assigned when editing the
hierarchy. The prefab can be placed in another scene; use one EventSystem only.

## Supplied sprites

The 20 original PNGs from the provided Downloads folder are copied into
`Assets/Art/Tamagotchi`. Source files in Downloads are untouched. Textures use
Point filtering, no mipmaps, no compression, and Sprite/Texture2D import settings.
The two parts of UI/frames.png are sliced in Unity; the PNG itself is unchanged.

Background, intact/broken device, meter icons/frames, full/empty hearts, menu
screens and normal menu buttons are wired into the editable prefab.
The full gameplay images are retained as references; gameplay uses separate
objects and actual live meters. **Tools > Tamagotchi > Apply Supplied Sprites**
reapplies this skin and its layout to the prefab.

Run the Tamagotchi.Tests EditMode suite through **Window > General > Test Runner**.
The editor utility **Tools > Tamagotchi > Rebuild Prototype UI Scene** recreates the
initial scene and prefab; it overwrites their layout, so use it only deliberately.
