# SpectralShift (Multiplayer Prototype)
**A networked 1v1 FPS arena shooter featuring procedurally generated mazes and server-authoritative gameplay.**

> **Note:** This project is currently in active development. It serves as a technical demonstration of Multiplayer Networking, Procedural Generation, and Latency Management.

##  Project Overview
**SpectralShift** is a multiplayer First-Person Shooter where two players (Host and Client) spawn into a procedurally generated maze. The core loop involves navigating a unique arena that changes every match and engaging in hitscan combat.

The primary goal of this project was to master **Unity Netcode for GameObjects (NGO)** and solve common challenges in networked physics and bandwidth optimization.

##  Technical Highlights

### 1. Bandwidth-Optimized Procedural Generation
*Most procedural games suffer from load-time lag when syncing map data. I implemented a seed-based approach to solve this.*

* **The Problem:** Spawning 400+ wall objects on the Server and attempting to sync them individually to the Client caused massive bandwidth spikes and connection timeouts.
* **The Solution:** Implemented **Seed Synchronization**. The Host generates a random integer seed which is synced via a `NetworkVariable`. The Client receives this seed and runs the exact same maze generation algorithm locally.
* **The Result:** Zero network lag during map generation and minimal bandwidth usage, as only a single integer is transmitted.

### 2. Networking & Architecture
* **Framework:** Built using **Unity Netcode for GameObjects (NGO)**.
* **Connection:** Integrated **Unity Relay** to allow players to connect across different networks without the need for port forwarding.
* **Architecture:** Strictly **Server-Authoritative**. Movement validation and game state are controlled by the server to prevent cheating and desynchronization.

### 3. Latency Handling & "Game Feel"
* **Client-Side Prediction:** Weapon visuals (e.g., bullet trails) trigger instantly on the local client to ensure responsive gameplay, while the actual hit logic is validated by the Server via RPCs.
* **Position Sync:** Uses `NetworkTransform` with interpolation to ensure enemy movement appears smooth even with minor latency.


##  How it Works (Under the Hood)
* **Maze Algorithm:** A **Recursive Backtracker** algorithm generates a grid-based maze with guaranteed pathing between start and end points, ensuring no player is ever walled off.
* **Smart Spawning:** The logic automatically identifies opposite corners of the generated grid (Bottom-Left vs Top-Right) to ensure fair spawn points regardless of the randomized maze size.
* **Relay Manager:** A custom UI flow handles authenticating with Unity Services, requesting an allocation, and generating a Join Code for easy connectivity.

##  How to Run

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/Kirit-Jain/Spectral-Shift.git
    ```
2.  **Open in Unity:** Version **2022.3** or later is recommended.
3.  **Build the Project:**
    * Go to `File > Build Settings`.
    * Select **Windows/Mac/Linux** and click **Build**.
4.  **Run Multiplayer:**
    * **Host:** Run the Unity Editor (or one instance of the build) and click **"Start Host"**. Copy the Join Code printed in the Console/UI.
    * **Client:** Run the standalone executable, paste the Join Code into the input field, and click **"Join"**.

##  Future Roadmap
- [ ] Implementation of Player Health and Death states.
- [ ] Spectator Camera functionality.
- [ ] Simple Lobby UI for easier connection.
- [ ] Scoreboard system.

##  Author
**Kirit Jain**

* [**GitHub**](https://github.com/Kirit-Jain/)
* [**LinkedIn**](https://www.linkedin.com/in/kirit-jain-019a60288/)
