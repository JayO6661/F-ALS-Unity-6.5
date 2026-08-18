# F-ALS — Unity Package Manager installation

F-ALS is distributed as a Unity Package Manager (UPM) package from this repository root.

## Unity 6

Supported baseline: Unity 6000.x.

The package declares `com.unity.animation.rigging` 1.4.0 as a dependency.

## Install from Git URL

In Unity open:

`Window > Package Manager > + > Install package from git URL...`

For a repository that your Git client can authenticate to, enter:

`https://github.com/JayO6661/F-ALS-Unity-6.5.git`

For a reproducible revision append a full commit SHA:

`https://github.com/JayO6661/F-ALS-Unity-6.5.git#<FULL_COMMIT_SHA>`

For private-repository SSH authentication you can alternatively use:

`ssh://git@github.com/JayO6661/F-ALS-Unity-6.5.git`

Git authentication must already work outside Unity. Unity Package Manager cannot display an interactive username/password prompt for a private Git repository.

## Project manifest

The equivalent `Packages/manifest.json` dependency is:

```json
{
  "dependencies": {
    "com.fgp.fals": "https://github.com/JayO6661/F-ALS-Unity-6.5.git"
  }
}
```

Pin a full commit SHA for production projects.

## Package layout

- `Runtime/` — runtime assembly and F-ALS components.
- `Editor/` — editor tooling and player setup utilities.
- `Docs/` — architecture and setup documentation.
- `package.json` — UPM package manifest.

## Production integration rule

Games should drive `FAlsController` through their own input/AI/network orchestration. `FAlsInputDriver` and `FAlsBootstrap` are standalone demo utilities and should not be used as a second production input authority.
