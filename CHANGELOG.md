# Changelog

## 1.0.2

- Rebuilt repository as a clean Unity UPM package.
- Removed bundled ALS-Refactored C++ reference source, historical research files and test scene from the distributed package.
- Removed unfinished active-ragdoll and legacy standalone execution utilities from the production core.
- Stabilized Unity `.meta` files and package GUIDs.
- Fixed malformed folder GUIDs and previous duplicate GUID collisions.
- Added production-only player setup and validation editor tools.
- Hardened `FAlsController` initialization.
- Made `FAlsAnimatorBridge` tolerant of partial Animator controllers.
- Retained locomotion motor, movement capacity, recovery state, procedural signals, football body-action readiness, Foot IK and debug signals.

## 1.0.1

- Fixed initial UPM metadata omissions and GUID collisions.

## 1.0.0

- First Git-installable Unity Package Manager release.
