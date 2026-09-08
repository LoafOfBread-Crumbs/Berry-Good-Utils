# Friend Setup Guide for Berry Good Utils

These are the one-time steps to allow Berry Good Utils to run and auto-update on a Windows PC that is not set up for unsigned apps. After this, the app should update itself without showing Windows security prompts.

## What this does

Windows 11 Smart App Control and SmartScreen may block the updater from replacing the `BerryGoodUtils.exe` file and relaunching it. Disabling Smart App Control and allowing the app once solves this.

## Manual steps for your friend

### Step 1: Create a folder for the app

Use this per-user application folder:

```
%LOCALAPPDATA%\BerryGoodUtils
```

To open it, press **Win + R**, paste `%LOCALAPPDATA%`, and press Enter. Create a folder named `BerryGoodUtils` there.

Do **not** put the app under `C:\Program Files`. Windows normally requires administrator permission to update files there, so the automatic updater may fail or display a UAC prompt. `%LOCALAPPDATA%` is writable by the current user and is the recommended location for this updater.

### Step 2: Download and move the app

Download `BerryGoodUtils.exe`, then move it from Downloads into:

```
%LOCALAPPDATA%\BerryGoodUtils\BerryGoodUtils.exe
```

Keeping it outside Downloads prevents it from being accidentally deleted and gives the updater a stable location.

### Step 2b: Create shortcuts

1. Right-click `BerryGoodUtils.exe` in `%LOCALAPPDATA%\BerryGoodUtils`.
2. Select **Show more options** → **Send to** → **Desktop (create shortcut)**.
3. Rename the shortcut to **Berry Good Utils** if desired.
4. To pin it, right-click the shortcut and select **Pin to Start** or **Pin to taskbar**.

### Step 3: Unblock the file

1. Right-click `BerryGoodUtils.exe` and choose **Properties**.
2. On the **General** tab, if you see this message at the bottom:
   ```
   This file came from another computer and might be blocked to help protect this computer.
   ```
   check the **Unblock** box, then click **OK**.

### Step 4: Run the app the first time

1. Double-click `BerryGoodUtils.exe`.
2. If Windows shows **"Windows protected your PC"** or a Microsoft Defender SmartScreen message, click **More info**, then **Run anyway**.

This adds the app to the allowed list on this computer.

### Step 5: Turn off Smart App Control (Windows 11 only)

Smart App Control is what usually blocks the auto-updater when it tries to rewrite the EXE file.

1. Open **Settings**.
2. Go to **Privacy & security**.
3. Select **Windows Security**, then click **Open Windows Security**.
4. Click **App & browser control**.
5. Under **Smart App Control**, click **Off** and confirm.

If the page says **Smart App Control has already been evaluated for your device**, click it and set it to **Off**.

### Step 5b: If you do not see Smart App Control

Some Windows versions use the older SmartScreen settings instead:

1. Open **Windows Security**.
2. Go to **App & browser control**.
3. Click **Reputation-based protection settings**.
4. Turn off:
   - **Check apps and files**
   - **Potentially unwanted app blocking**

## PowerShell helper (for you to run if you have access to their PC)

If you can run PowerShell as an administrator on their machine, this will unblock the downloaded EXE, disable SmartScreen for apps, and launch it once.

Save the following as `setup-friend.ps1` and run it in an administrator PowerShell window:

```powershell
#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

$source = Join-Path $env:USERPROFILE "Downloads\BerryGoodUtils.exe"
$folder = Join-Path $env:LOCALAPPDATA "BerryGoodUtils"
$exe = Join-Path $folder "BerryGoodUtils.exe"

if (-not (Test-Path $source)) {
    throw "Could not find BerryGoodUtils.exe in the Downloads folder."
}

New-Item -ItemType Directory -Path $folder -Force | Out-Null
Copy-Item -Path $source -Destination $exe -Force
Unblock-File -Path $exe

$shell = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcut = $shell.CreateShortcut((Join-Path $desktop "Berry Good Utils.lnk"))
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $folder
$shortcut.Save()

# Disable SmartScreen for executables
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" -Name "SmartScreenEnabled" -Value "Off" -Force -ErrorAction SilentlyContinue

# Disable Windows SmartScreen for apps and files
Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System" -Name "EnableSmartScreen" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue

Write-Host "Launching BerryGoodUtils.exe..."
Start-Process -FilePath $exe
```

Note: if Smart App Control is already enabled, the UI method in Step 5 is the most reliable way to turn it off.

## After setup

Once the above is complete, future updates should be fully automatic:

1. The app checks GitHub for a new version.
2. If one is found, it downloads the new `BerryGoodUtils.exe`.
3. It closes, replaces itself, and restarts.

Your friend should only need to click **Download & Install Update** inside the app. Windows should not block the relaunch again.
