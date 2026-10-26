# Missions

The main menu now has a MISSIONS button below the title on the right. It opens a modal over the menu. DAILY and MISSIONS switch its contents. X or tapping outside closes it. The menu remains visible and cannot receive button input while the modal is open. Permanent missions scroll vertically.

## Daily missions

Three tasks are selected from the approved nine task types, one from each difficulty group. Shrink tasks and the three-runs-over-60-seconds task are excluded. Selection is saved for the day, avoids yesterday's task in each slot, and excludes two-skin tasks when fewer than two skins are owned and case tasks when no case is available/affordable at selection time.

Reset is midnight UTC (03:00 Turkey). An active run finishes against its original day's tasks before rollover. Completed, unclaimed tasks and their eligible all-three bonus are automatically paid when the game next checks the date. Incomplete tasks expire. Moving the clock backwards does not reopen an old day; this local version does not authenticate time with a server.

Rewards in base coin units: easy 30, medium 60, hard 100, all-three bonus 60. The current project uses CoinScale = 10, so the wallet receives 300/600/1000 and a 600 bonus (2500 total). Targets that refer to coins use the same scaling as the existing case and run economy.

## Permanent missions

Twelve approved series track total/best-run coins, runs, total/best-run survival, total/best-run air balloons, skins owned, cases opened, daily tasks completed, all-three daily sets, and runs lasting at least 120 seconds. Progress persists across sessions and reward claims. Only real run earnings advance coin tasks; shop, mission and double-reward payouts do not.

Stages 1–4 reward 50/100/150/200 base coins. Stage 5 gives **50 gems and no coins**. Later stages use growing targets and coin rewards capped at 300 base coins. Skin and case series finish after five stages at 16 owned skins and 15 cases, matching the finite catalog. Existing ownership and case openings count; run statistics begin tracking when this update is first used.

State is stored in BalloonDog.Missions.v1. Saves happen at run start/end, application suspension/quit, claims, case openings and every five seconds during gameplay. Revives continue the same run. Claim state and wallet reward are written before the same PlayerPrefs save, preventing repeated claims across reloads.

## Validation

Editor tests: BalloonDogMissionsTests and mission integration tests in BalloonDogEconomyTests. Cover reset boundaries, stable selection, unavailable tasks, backward clock movement, rollover during a run, saves, cumulative vs best progress, duplicate claims, fifth-stage gems, case accounting, wallet isolation and modal navigation/input blocking. Existing economy tests preserve and restore the mission save.

Run with Unity Test Runner (EditMode), or Unity batch mode with `-runTests -testPlatform EditMode -testResults missions-tests.xml` and this project path. Unity is required for the runtime/UI tests; syntax parsing alone does not validate rendering or Unity compilation. On-device smoke check: open the popup, switch tabs and scroll, close it, play a run, verify progress, claim a reward, restart and verify it stays claimed.

## Popup layout correction

The list content is explicitly pinned to its viewport's top, and each row's children share its top origin. This removes the half-row gap introduced by mixing centred child anchors with top-pivot rows. The daily page has three fully visible rows without scrolling; its bonus sits outside the viewport with a separate gap. Permanent rows still scroll, retain their offset after claims, and reset to the top when switching tabs.

The popup uses a small shared rounded sprite generated in code, layered cyan/white rims, the existing blue/green button artwork, matching capsule tabs, larger Titan One text and bordered progress tracks. No new imported image dependency is needed. Geometry regression cases cover four portrait sizes in Unity; offline layout calculations cover five sizes, and actual bundled-font fitting covers 168 permanent stage/title combinations. Syntax and offline checks passed here; Unity rendering and EditMode execution still require the Unity Editor.
