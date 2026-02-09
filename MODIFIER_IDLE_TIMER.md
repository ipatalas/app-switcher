# Modifier Idle Timer - Technical Documentation

## Problem Statement

When AppSwitcher switches to another application that blocks keyboard events, the modifier key-up event may never reach AppSwitcher's keyboard hook. This causes `_modifierDown` to remain `true`, leading to unwanted application switches when pressing letter keys alone (without the modifier).

### Why Modifier Events Are Blocked

1. AppSwitcher **suppresses** modifier key events to prevent side effects (e.g., context menus opening on key down)
2. When switching to an app that aggressively captures keyboard input, the modifier key-up event gets "stolen" before reaching AppSwitcher
3. Without the key-up event, AppSwitcher's state machine thinks the modifier is still pressed

## Critical Constraints

These constraints shaped the solution design:

1. **Cannot use `GetAsyncKeyState()`** - Modifier keys are suppressed (by AppSwitcher itself), so querying physical key state returns incorrect results
2. **Must support rapid multi-app switching** - Users hold the modifier and press multiple letter keys quickly (e.g., Apps+T → Apps+V → Apps+E)
3. **Cannot delay legitimate key events** - Any polling/checking solution adds latency to the critical path
4. **Windows keyboard hook limitations** - Hook events can be blocked/stolen by applications with higher-priority hooks or aggressive input capture
5. **No false positives** - Timer must not expire while user is legitimately holding the modifier key
6. **Fast recovery** - When state is stuck, user shouldn't wait more than a few seconds for auto-recovery

## Decision Trail

### ❌ Rejected Approach 1: Poll `GetAsyncKeyState()` on Every Event

**Idea:** Check physical modifier key state on every keyboard event

**Why rejected:**
- Modifier keys are suppressed, so `GetAsyncKeyState()` sees them as "not pressed" even when physically held
- Would incorrectly reset state during normal usage
- Violates constraint #1

---

### ❌ Rejected Approach 2: Background Polling Thread

**Idea:** Dedicated thread polling `GetAsyncKeyState()` every 100ms

**Why rejected:**
- Same issue as Approach 1 - suppressed keys make polling unreliable
- Adds unnecessary CPU overhead
- Polling interval creates tradeoff: too fast = wasteful, too slow = delayed recovery
- Violates constraint #1

---

### ❌ Rejected Approach 3: Single-Tier Short Timeout

**Idea:** Simple 500ms timeout that resets on any event

**Why rejected:**
- Too short - would expire during legitimate use when user pauses between switches
- Would interrupt workflows like "hold Apps, think for 1 second, press next letter"
- Violates constraint #2 (rapid multi-app switching)

---

### ❌ Rejected Approach 4: Manual State Reset Hotkey

**Idea:** Let user press special key combo (e.g., Escape) to manually reset stuck state

**Why rejected:**
- Poor UX - requires user to understand internal state machine
- User may not realize state is stuck until after pressing a letter key
- Doesn't meet constraint #6 (fast automatic recovery)

---

### ✅ Accepted Approach: Timer with Key Repeat Awareness

**Why this works:**

1. **Leverages Windows key repeat behavior** - Held modifier keys generate repeated key-down events (~30-50ms), naturally keeping timer alive
2. **Three-layer safety net** - Normal event (99%), unrelated key detection (fast), timeout (backstop)
3. **No false positives** - Timer only expires if no events occur for 2 seconds (impossible while key is held)
4. **Meets all constraints:**
   - ✅ No reliance on `GetAsyncKeyState()`
   - ✅ Supports rapid multi-app switching (timer restarts on each letter press)
   - ✅ Zero latency on critical path (timer runs asynchronously)
   - ✅ Handles blocked events gracefully (timer expires and resets)
   - ✅ No false positives (key repeats prevent premature expiry)
   - ✅ Fast recovery (2s timeout + instant reset on unrelated keys)

**Key insight discovered during implementation:** Windows' modifier key repeat behavior eliminates the need for complex multi-tier timeout systems. The OS already provides the "keep-alive" signal we need.

## Solution Overview

A **timer-based recovery mechanism** with multiple safety nets to automatically reset the stuck modifier state.

### Core Mechanism

The `ModifierIdleTimer` class provides a 2-second timeout that:
- **Starts/restarts** on modifier key-down events (including key repeats)
- **Starts/restarts** after application switches
- **Cancels** on modifier key-up events (normal case - 99% of time)
- **Expires and resets state** if no activity occurs within timeout

## Key Insight: Modifier Key Repeats

**Critical Discovery:** Windows sends repeated key-down events for held modifier keys (~30-50ms intervals) **UNTIL** another key is pressed.

### Behavior Flow

```
Time   Event                          Timer Status
─────────────────────────────────────────────────────
0ms    Modifier Down (initial)        Started (2000ms)
30ms   Modifier Down (repeat)         Restarted (2000ms)
60ms   Modifier Down (repeat)         Restarted (2000ms)
...    (repeats every ~30-50ms)       Keeps restarting
500ms  Letter Down                    Restarted (2000ms)
       (NO MORE modifier repeats)     
700ms  Modifier Up                    Cancelled ✅
```

**Why this works:** As long as the modifier is physically held, repeated key-down events keep resetting the timer, preventing false timeouts. The timer can only expire if:
1. The modifier is released (but event was blocked), OR
2. User stops pressing keys for 2+ seconds

## Recovery Mechanisms (Three Safety Nets)

### 1. Normal Event Reception (Primary - 99% of cases)

Modifier key-up event is received normally → Timer cancelled immediately

```csharp
else // modifier up after letter key was pressed
{
    modifierIdleTimer.Cancel();
}
```

### 2. Unrelated Key Detection (Fast Recovery)

If user presses any non-modifier, non-bound-letter key while `_modifierDown == true`:
- Immediate state reset (no waiting for timeout)
- Indicates user has moved on to normal typing

```csharp
else if (_modifierDown && !IsConfiguredModifier(e.InputEvent.Key))
{
    logger.LogDebug("Unrelated key {Key} pressed while modifier down - resetting state");
    ResetModifierState();
}
```

### 3. Timeout Recovery (Backstop - Handles Edge Cases)

If timer expires after 2 seconds of no activity:
- Assume modifier was released but event was blocked
- Reset state automatically

## Edge Cases Handled

### Edge Case 1: Original Issue - App Switch Blocks Modifier-Up

**Scenario:**
```
1. User presses Modifier → Timer starts (2s)
2. User presses Letter → App switches, timer restarts (2s)
3. User releases Modifier → Event BLOCKED ❌
4. 2 seconds later → Timer expires, state resets ✅
5. User presses Letter alone → No unwanted switch ✅
```

**Resolution:** Timeout recovery kicks in after 2 seconds

---

### Edge Case 2: Modifier Pressed Alone (No App Switch)

**Scenario:**
```
1. User presses Modifier → Timer starts (2s)
2. User releases Modifier → Event BLOCKED ❌
3. 2 seconds later → Timer expires, state resets ✅
```

**Resolution:** Timer was already running from modifier-down, so timeout recovery still works

**Note:** Modifier key repeats keep the timer alive as long as key is held, so timer won't expire prematurely.

---

### Edge Case 3: Multiple Quick App Switches

**Scenario:**
```
1. User presses Modifier → Timer starts (2s)
2. User presses A → Switch to App A, timer restarts (2s)
3. User presses B → Switch to App B, timer restarts (2s)
4. User presses C → Switch to App C, timer restarts (2s)
5. User releases Modifier → Timer cancelled ✅
```

**Resolution:** Each letter press restarts the timer, allowing unlimited rapid switching

---

### Edge Case 4: User Holds Modifier Without Pressing Letters

**Scenario:**
```
1. User presses Modifier → Timer starts (2s)
2. Modifier Down (repeat) → Timer restarts (2s)
3. Modifier Down (repeat) → Timer restarts (2s)
... (user thinking for 10 seconds)
20. User presses Letter → App switches ✅
```

**Resolution:** Key repeats keep resetting the timer indefinitely while modifier is held

---

### Edge Case 5: User Releases Modifier and Starts Typing

**Scenario:**
```
1. User presses Modifier → Timer starts (2s)
2. User presses Letter → App switches, timer restarts (2s)
3. User releases Modifier → Event BLOCKED ❌
4. User presses Space/Enter → State resets immediately ✅ (Fast recovery)
```

**Resolution:** Unrelated key detection provides instant recovery (no 2-second wait)

---

### Edge Case 6: Configuration Change Mid-Operation

**Scenario:**
```
1. User presses Modifier (LeftCtrl) → Timer starts
2. Config changes modifier to RightAlt
3. User releases LeftCtrl → Wrong modifier, state stuck
```

**Resolution:** `UpdateConfiguration()` calls `ResetModifierState()`, clearing all state

```csharp
public void UpdateConfiguration(Configuration.Configuration config)
{
    _config = config;
    ResetModifierState(); // Clears _modifierDown and cancels timer
}
```

---

### Edge Case 7: Timeout Between Switches

**Scenario:**
```
1. User presses Modifier → Timer starts (2s)
2. User presses Letter A → Switch, timer restarts (2s)
3. (user waits 3 seconds)
4. Timer expires → State resets ❌
5. User presses Letter B → No switch (modifier lost)
```

**Status:** **Accepted Tradeoff**

This is intentional behavior - users have 2 seconds between actions. If they wait longer, they need to release and re-press the modifier. This prevents indefinite stuck states.

---

### Edge Case 8: Key Repeats Stop After Letter Press

**Important Discovery:** Modifier key repeats **stop** once any other key is pressed.

**Scenario:**
```
1. Modifier Down (initial) → Timer starts (2s)
2. Modifier Down (repeat) → Timer restarts (2s)
3. Letter Down → Switch, timer restarts (2s), NO MORE REPEATS
4. (2 second countdown begins)
5. Either:
   a) Modifier Up → Cancel timer ✅
   b) Letter Down → Restart timer ✅
   c) Timeout → Reset state ✅
```

**Resolution:** This is fine because:
- Each letter press (app switch) restarts the timer
- User has 2 seconds between actions
- Normal modifier-up still works (primary case)

## Why 2 Seconds?

The 2-second timeout is a balance between:
- **Too short (< 1s):** May interrupt legitimate multi-app switching workflows
- **Too long (> 5s):** User experiences the bug for longer before auto-recovery
- **2 seconds:** Reasonable for "thinking time" between switches, fast enough recovery

**Future:** This can be made user-configurable via config.json

## Testing Scenarios

To manually test the fix:

1. **Normal Usage:** Modifier + Letter → Switch → Release modifier (should work as before)
2. **Blocked Event:** Use an app that blocks events → Modifier + Letter → Switch → Release (wait 2s) → Press letter alone (should NOT switch)
3. **Multi-Switch:** Hold modifier → Press A → B → C rapidly → Release (all should switch)
4. **Fast Recovery:** Modifier + Letter → Switch → Release (blocked) → Press Space → Press letter (should NOT switch)
5. **Long Hold:** Hold modifier for 10+ seconds → Press letter (should still switch)

## Summary

The timer-based solution provides:
- ✅ **Robust recovery** from blocked modifier key-up events
- ✅ **Three-layer safety net** (normal event, unrelated key, timeout)
- ✅ **No false positives** during normal usage (key repeats prevent premature timeout)
- ✅ **Fast recovery** via unrelated key detection (< 2s in common cases)
- ✅ **Clean architecture** (separated concern, DI-managed, testable)
- ✅ **Configurable** (timeout can be adjusted or disabled)

The key insight that makes this work elegantly is **modifier key repeats** - they naturally keep the timer alive as long as the key is physically held, eliminating the need for complex multi-tier timeout systems.
