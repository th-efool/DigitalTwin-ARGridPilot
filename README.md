# Modular Piezo Grid Interface for Digital Twin Control

![Unity](https://img.shields.io/badge/Engine-Unity-black?logo=unity)
![Arduino](https://img.shields.io/badge/Hardware-Arduino-00979D?logo=arduino)
![AR Foundation](https://img.shields.io/badge/AR-AR%20Foundation-blueviolet)
![Digital Twin](https://img.shields.io/badge/Architecture-Digital%20Twin-blue)
![Robotics](https://img.shields.io/badge/Domain-Robotics%20Control-darkgreen)
![Sensors](https://img.shields.io/badge/Input-Piezo%20Sensors-orange)
![Serial](https://img.shields.io/badge/Comm-Serial%20USB-lightgrey)
![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20PC-informational)
![Status](https://img.shields.io/badge/Status-Prototype%20%2F%20Research-yellow)

## Overview

This project implements a **modular piezo-sensor tap interface** that maps a physical **n×n soft-input grid** to a **Unity-based Digital Twin**.
Each tap on the physical piezo array is streamed into Unity and interpreted as a **discrete, intensity-aware control signal**, enabling structured interaction with a virtual robot.

The system is designed for **robotic control, signal encoding, and human–machine interaction research**, including **Morse-style patterned inputs** and spatial command sequences.


<p align="center">
  <img 
    src="https://github.com/th-efool/DigitalTwin-PizeoSensorARGridPilot/blob/main/Assets/p/WhatsApp%20Image%202025-12-30%20at%2009.56.45.jpeg?raw=true" 
    width="100%" 
  />
</p>

<table>
  <tr>
    <td align="center">
      <img 
        src="https://github.com/th-efool/DigitalTwin-PizeoSensorARGridPilot/blob/main/Assets/p/screenshot20251230095840.png?raw=true" 
        width="100%"
      />
    </td>
    <td align="center">
      <img 
        src="https://github.com/th-efool/DigitalTwin-PizeoSensorARGridPilot/blob/main/Assets/p/screenshot20251230095919.png?raw=true" 
        width="100%"
      />
    </td>
    <td align="center">
      <img 
        src="https://github.com/th-efool/DigitalTwin-PizeoSensorARGridPilot/blob/main/Assets/p/screenshot20251230095927.png?raw=true" 
        width="100%"
      />
    </td>
  </tr>
</table>

---

## Core Idea

Instead of treating input as a single binary trigger, this system treats **space + sequence + timing** as the input language.

* Each grid cell corresponds to a **logical command node**
* Tap location selects intent
* Tap sequences encode meaning
* The Digital Twin visualizes and executes the command in real time

This makes the interface scalable, expressive, and suitable for robotics experimentation.

---

## System Architecture

```text
Physical Piezo Grid (n×n)
        │
        ▼
Microcontroller (Arduino)
  - Piezo hit detection
  - Sensor index encoding
        │
        ▼
Serial Communication (USB)
        │
        ▼
Unity Runtime
  - PiezoSerialReceiver
  - GridMaster (logic + visualization)
  - Digital Twin Robot
```

---
Arduino Integration (Piezo Grid → Unity)

The physical input layer is implemented using an Arduino-based piezo sensor array, where each piezo element corresponds to a cell in the logical n×n grid used by the Digital Twin.

Hardware Role

Each piezo sensor is wired to a dedicated Arduino input channel

A tap generates an analog spike whose magnitude reflects impact intensity

The Arduino performs thresholding and debouncing to detect valid taps

Each detected tap is mapped to a grid index in the range 0 … (n×n − 1)

The Arduino does not attempt to interpret motion or behavior.
It acts as a deterministic signal encoder, keeping the hardware simple and predictable.

## Key Components

### 1. GridMaster (Core Logic)

Responsible for:

* Spawning an **n×n grid** dynamically
* Computing grid positions with padding and scaling
* Validating movement rules (adjacent / diagonal)
* Driving robot movement and animation
* Highlighting valid tiles and active targets

Key features:

* Adjustable grid size (`Length`)
* Configurable spacing and padding
* Smooth robot interpolation with rotation alignment
* Visual feedback through material switching and pulse animation
* Singleton-based access for hardware input routing

---

### 2. GridTile (Per-Cell Interaction)

Each grid cell:

* Knows its index in the grid
* Reports interaction back to `GridMaster`
* Supports both **mouse/touch input** and **hardware-driven activation**

This allows:

* Software-only testing
* Hardware-in-the-loop control
* Seamless fallback between input sources

---

### 3. PiezoSerialReceiver (Hardware Bridge)

Acts as the **physical → digital boundary**.

Responsibilities:

* Connects to Arduino via Serial
* Parses incoming sensor indices
* Supports multiple serial formats (`"2"`, `"SENSOR,2"`)
* Validates bounds against grid size
* Triggers Digital Twin movement in real time

Design goals:

* Non-blocking reads
* Robust against malformed input
* Hardware-agnostic beyond index mapping

---

### 4. AR Grid Placement (Optional)

For AR-enabled builds:

* Places the grid onto detected planes using AR Foundation
* Supports scale and rotation calibration
* Locks placement once confirmed
* Disables plane rendering after calibration

This allows the **physical grid, virtual grid, and real world** to align spatially.

---

### 5. UI Calibration Layer

Provides:

* Rotation control
* Scale adjustment
* Placement confirmation
* Clean lock-in state after calibration

Keeps calibration separate from interaction logic.

---

## Interaction Model

* A tap on a piezo sensor → mapped to a grid index
* The grid index selects a logical position
* Movement rules enforce local adjacency
* The robot animates smoothly to the new tile
* Visual feedback confirms valid and invalid moves

This structure allows extension into:

* Gesture sequences
* Tap-timing patterns
* Morse-like symbolic encoding
* Multi-step robotic commands

---

## Why This Matters

This is **not just a UI grid**.

It is:

* A **soft tactile input language**
* A bridge between **physical sensing** and **virtual embodiment**
* A foundation for **robot control abstractions**
* A testbed for **Digital Twin interaction design**

The grid scales naturally, the encoding grows richer with time, and the system remains interpretable.

---

## Extension Ideas

* Tap intensity → velocity / priority
* Sequence buffers for symbolic commands
* Multi-robot routing
* Inverse kinematics triggers per tile
* Learning-based gesture recognition
* Networked Digital Twin synchronization

---

## Intended Use Cases

* Robotics research & prototyping
* Human–machine interaction experiments
* Digital Twin control systems
* Educational robotics interfaces
* AR-assisted physical control panels

---

If you want, I can also:

* add **a minimal wiring + Arduino sketch section**
* write a **Morse / pattern encoding spec**
* convert this into a **paper-style system description**
* or refactor the code into **clean subsystems**

Just say the direction.
