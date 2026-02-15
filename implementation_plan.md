# Packet_P1.3.2_WeaponCategorySourceOfTruth_001 Implementation Plan

## Goal
Establish `WeaponDef` as the Source of Truth for `WeaponCategory`, ensuring robust detection over name guessing, and standardize StepBack mechanics with TimeCore ticks and loop prevention.

## Proposed Changes

### 1. `Assets/Scripts/Combat/TimeCore/WeaponDef.cs`
- **Overwrite File** (due to read corruption):
  - Define `public class WeaponDef : ScriptableObject`
  - Add `public float BasePower;` (Restoring known field)
  - Add `public enum WeaponCategory { Knife, Sword, Greatsword, Spear, Bow, Gun, Unknown }`
  - Add `public WeaponCategory category = WeaponCategory.Unknown;`
  - Add `public string ToDisplayName() => $"{name} ({category})";` (Helper)

### 2. `Assets/Scripts/Combat/TimeCore/BattleTimeManager.cs`
- Remove internal `enum WeaponCategory`.
- Update `GetWeaponCategory` to:
  - Check `equippedWeapon.category`.
  - If `Unknown`, fallback to logic.
- Add `const int STEPBACK_TICKS = 1;`
- **Refactor `EnqueueNormalAttack`**:
  - Add optional `retryCount` parameter.
  - Implement Infinite Loop Prevention (Max 2 retries).
  - Use `STEPBACK_TICKS` for the delay.

## Verification Plan
### Manual Verification
- **Test 1: Check Data**
  - Verify `WeaponCategory` enum is accessible in Inspector (if possible) or Code.
- **Test 2: Infinite Loop Prevention**
  - Verify "Impossible" condition aborts smoothly after limit.
- **Test 3: StepBack Timing**
  - Verify StepBack takes 1 tick.
