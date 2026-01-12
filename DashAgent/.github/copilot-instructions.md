# DashAgent - Copilot Instructions

## Project Overview

DashAgent is a .NET 10 console application designed to run as a daemon on a Raspberry Pi 5 with an attached Pi Display (backlight path: `/sys/class/backlight/11-0045`).

## Purpose

The application monitors and controls a Raspberry Pi system, broadcasting metrics via MQTT and integrating with Home Assistant through MQTT Discovery.

## Key Features

### 1. Platform Detection
- **Requirement**: Must run exclusively on Raspberry Pi hardware
- **Implementation**: Checks `/proc/device-tree/model` and `/proc/cpuinfo` for Pi identification
- **Behavior**: Exits with code 1 if not running on a Raspberry Pi

### 2. Display Control
- **Backlight Path**: `/sys/class/backlight/11-0045`
- **Capabilities**:
  - Get/Set brightness (0 to max_brightness, typically 31)
  - Turn display on/off
  - Query current brightness level
  - Query display on/off state

### 3. System Metrics Collection
The application monitors and broadcasts the following metrics:
- **Display Brightness**: Current backlight level (0-31)
- **CPU Temperature**: Current CPU temperature in °C
- **CPU Usage**: CPU usage percentage
- **Memory Usage**: Memory usage percentage
- **Display State**: On/off state

### 4. MQTT Integration

#### Topics Structure
- **Discovery**: Publishes Home Assistant MQTT Discovery message
- **Backlight Control**:
  - Command: `raspi/backlight/set`
  - State: `raspi/backlight/state`
  - Brightness Command: `raspi/backlight/brightness/set`
  - Brightness State: `raspi/backlight/brightness/state`
- **Sensors**:
  - CPU Usage: `raspi/cpu/usage`
  - CPU Temperature: `raspi/cpu/temperature`
  - Memory Usage: `raspi/memory/usage`

#### Device Information
- **Device ID**: `raspi_01`
- **Device Name**: Raspberry Pi
- **Manufacturer**: Raspberry Pi Foundation
- **Model**: Raspberry Pi

#### Components
1. **raspi_backlight** (light):
   - Unique ID: `raspi_backlight_01`
   - Schema: JSON
   - Supports brightness control

2. **cpu_usage** (sensor):
   - Unique ID: `raspi_cpu_usage_01`
   - Unit: %
   - Device Class: power_factor
   - State Class: measurement

3. **cpu_temperature** (sensor):
   - Unique ID: `raspi_cpu_temp_01`
   - Unit: °C
   - Device Class: temperature
   - State Class: measurement

4. **memory_usage** (sensor):
   - Unique ID: `raspi_memory_usage_01`
   - Unit: %
   - Device Class: power_factor
   - State Class: measurement

### 5. Background Service
- **Implementation**: Runs as a `BackgroundService`
- **Polling Interval**: 10 seconds
- **Tasks**:
  - Publishes discovery message on startup
  - Polls system metrics every 10 seconds
  - Updates MQTT topics with current state

## Architecture

### Core Classes
- **PiController**: Hardware interaction layer for display control and system metrics
- **PiStateUpdater**: Background service that polls metrics and publishes to MQTT
- **Parsers**: Utilities for parsing `/proc` files (ProcStatParser, MemInfoParser)

## Development Notes

### Target Framework
- .NET 10
- C# 14.0

### Deployment Target
- Raspberry Pi 5
- Linux-based OS
- Requires read/write access to `/sys/class/backlight/11-0045`
- Requires read access to `/proc` filesystem

### Future Enhancements
- Full MQTT client integration (currently console output only)
- Daemon service configuration
- Configuration file support for MQTT broker settings
- Additional sensor support

## Code Style
- Minimal comments (unless explaining complex logic)
- Use existing libraries when possible
- Follow .NET conventions and idioms
- Leverage C# 14.0 features where appropriate
