# Install F-ALS 1.0.2

## Unity Package Manager

Open:

`Window > Package Manager > + > Install package from git URL...`

Enter:

`https://github.com/JayO6661/F-ALS-Unity-6.5.git`

For production, pin a full commit SHA:

`https://github.com/JayO6661/F-ALS-Unity-6.5.git#<FULL_COMMIT_SHA>`

The package declares Animation Rigging 1.4.0 and Unity Package Manager resolves it automatically.

## First use

1. Select the player root GameObject.
2. Run `Tools > F-ALS > Setup Selected Player`.
3. Click `Apply Core Setup`.
4. Run `Tools > F-ALS > Validate Selected Player`.

The setup tool does not add game input, stamina, ball or networking logic.

## Updating a Git-installed package

If Unity is pinned to an old commit, remove the package and install the new pinned URL, or update the Git revision in `Packages/manifest.json` and reopen the project.

## Production manifest example

```json
{
  "dependencies": {
    "com.fgp.fals": "https://github.com/JayO6661/F-ALS-Unity-6.5.git#<FULL_COMMIT_SHA>"
  }
}
```
