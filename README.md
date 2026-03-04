# Foundry Core

Foundry Core is the Unity package that connects your project to the Foundry backend stack:

- **Foundry Database** (accounts, auth, user data, property definitions, roles)
- **Foundry Runtime Networking** (sector/room sessions, entity sync, websocket messaging, voice channel bootstrapping)

Package name: `com.cyberhub.foundry.core`.

---

## What this package gives you

At a high level, this package includes:

- **Application bootstrap and module config**
  - A central app/service container (`FoundryApp`)
  - ScriptableObject-driven module registration (`FoundryAppConfig`, `FoundryModuleConfig`)
- **Database API client**
  - Login/signup/session refresh
  - User/profile/roles/property operations
  - Sector resolution for networking
- **Realtime networking runtime**
  - WebSocket connection and message queues
  - Networked object/entity state sync
  - Voice channel connection bootstrap (UDP)
- **Editor tooling**
  - Config window
  - Setup wizard checks (e.g., App Key required)
  - Database management UI

---

## Unity compatibility

- **Unity**: `2022.3`
- **Version**: currently `0.6.0-preview`

(See `package.json` for the authoritative metadata.)

---

## Installation

Install with Unity Package Manager via Git URL:

1. Open **Window → Package Manager**.
2. Click **+** (top-left) → **Add package from git URL...**
3. Paste your package URL for this repo.

Example format:

```text
https://github.com/<org>/<repo>.git
```

If you need a specific branch/tag/revision:

```text
https://github.com/<org>/<repo>.git#main
https://github.com/<org>/<repo>.git#v0.6.0-preview
```

---

## First-time project setup (recommended order)

### 1) Open the Foundry setup tools

After importing, use:

- **Foundry → Setup Wizard**
- **Foundry → Config**

The setup wizard will flag required tasks (notably the App Key).

### 2) Configure `FoundryCoreConfig`

Set the required fields:

- **App Key** *(required)*
- **Override Database URL** *(recommended default for current deployments unless you run your own Foundry backend)*

Use this database URL by default:

```text
http://85.57.195.227:8080
```

Only change this if you are running a separate Foundry installation elsewhere.

### 3) Ensure app config asset exists

Foundry uses a `Resources/FoundryAppConfig` ScriptableObject for module registration. The editor utilities can auto-create it when needed.

### 4) (Optional) Verify database connectivity

Open:

- **Foundry → Database → Manager**

Use this to test login/account flows and inspect/update user definitions.

---

## Typical runtime flow

A common multiplayer startup path looks like this:

1. Obtain active database session (`DatabaseSession.GetActive()`)
2. Authenticate (login/token/refresh flow)
3. Resolve sector/room from key
4. Connect websocket to runtime server
5. Enter sector and establish local network state
6. Start delta sync and incoming message processing

If `NetworkManager.autoStart` is enabled, this bootstrapping happens automatically in `Start()`.

---

## Core concepts

### Foundry App / modules / services

- `FoundryApp` is the central runtime service registry.
- `FoundryAppConfig` lists module config assets.
- Each module (`FoundryModuleConfig`) can expose service constructors and selectively enable services.

This pattern lets Foundry packages compose capabilities without hard wiring every dependency in one place.

### Database session

`DatabaseSession` is the primary client for Foundry Database APIs. It handles:

- auth/account endpoints
- cached session token + refresh flow
- user/role/prop APIs
- sector lookup for networking

### Network manager

`NetworkManager` is the scene/runtime entry point for multiplayer state. It coordinates:

- room key / sector lifecycle
- spawn/link/despawn behavior for `NetworkObject`s
- entity delta publishing
- incoming event processing

---

## Directory map

```text
Application/     Foundry app bootstrap, module/service registration
Config/          FoundryCoreConfig (App Key + base URL settings)
Database/        HTTP API calls, DTOs, session/auth helpers
Networking/      WebSocket + runtime state/entity sync + native components
Serialization/   Foundry serializer interfaces and built-in serializers
Editor/          Config/setup/database editor windows and utilities
```

---

## New contributor quick start

If you're new to this repo and want to contribute quickly, these are high-value first tasks:

1. **Documentation polish**
   - Add concrete scene setup examples and common troubleshooting.
2. **Runtime safety improvements**
   - Replace/contain `async void` usage where practical.
   - Improve shutdown/disposal guardrails around network session teardown.
3. **Networking correctness pass**
   - Audit message parsing/assertions and queue synchronization semantics.
4. **Tests**
   - Add unit tests for serializer roundtrips.
   - Add lightweight tests for database session behavior with mocked responses.
5. **Editor UX quality**
   - Improve validation messaging and error surfaces in setup/config windows.

---

## Troubleshooting

### "FoundryAppConfig not found"

Make sure the `FoundryAppConfig` asset exists under a Resources path and that module configs are populated.

### "Foundry App Key not set"

Open **Foundry → Config** and set `AppKey` in `FoundryCoreConfig`.

### Can't connect to backend

Check:

- App key validity
- database base URL (`OverrideDatabaseUrl` for local/dev)
- backend service availability
- networking constraints/firewalls for websocket/voice ports

---

## Status

This package is currently marked preview (`0.6.0-preview`), so APIs and workflows may evolve.

