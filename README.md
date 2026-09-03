# Death Race

Death Race is a multiplayer vehicular combat racing game built in Unity, using Netcode for GameObjects, Unity Lobby/Relay, and HDRP for real-time networked play. Up to 8 players race across two laps in tiered vehicles, with weapons (miniguns, rockets) and a deflection shield unlocked via pressure plates that only arm after the second lap, keeping the race fair before combat opens up. The name, setting, and pickup design draw creative inspiration from the 2008 film of the same name. The project was built as an MSc dissertation.

This repository contains the Unity project source code only. No assets and models are included. 

## Instructions

Supporting document: how to run the build and the controls.

**Link to the Build:** https://drive.google.com/file/d/1TWnarak7UsvsTBatEnU7NdnQaHA-NyJ3/view?usp=sharing

## 1. Running the Build

1. Extract the supplied .zip file to any folder on your PC.
2. Open the extracted folder and run the `DeathRace.exe` file to launch the game.

## 2. How To Play The Game
1. Login with your name.
2. Select a car from the car showroom or race with the default car.
3. Read instructions by clicking the white console icon in the top right corner.
4. Click on Start to go to the lobby scene.
5. Click the + icon to make a new lobby.
6. Or join a lobby by pressing the refresh icon just beside the + icon -after clicking it, any existing lobbies will pop up if someone has already made one.
7. From the lobby, connect with other players (up to 8) then begin the race.
8. You will be spawned in a default waiting area; after 30 seconds you will spawn on the racetrack, and after the green lights go off you can start racing.
9. You have to close the game manually yourself through Alt+F4 if you want to quit the game.

## 3. Controls

| Action | Primary Binding | Alternate Binding |
|---|---|---|
| Drive (accelerate / steer / reverse) | WASD | Arrow keys |
| Aim | Mouse movement | - |
| Shoot (main gun) | Left Ctrl | - |
| Fire rockets | Right-click | Left Alt |
| Homing rockets | Right Alt | - |
| Deflection shield | Automatic on pickup (no key needed) | - |
| Toggle free-look camera | T | - |

Note: rockets can be fired with either right-click or Left Alt - both trigger the same action. Right Alt fires homing rockets. The shield activates automatically once picked up from its pressure plate and needs no separate button press.

