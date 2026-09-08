# Friend Setup Guide for Berry Good Utils

These are the one-time steps to allow Berry Good Utils to run and auto-update on a Windows PC that is not set up for unsigned apps. After this, the app should update itself without showing Windows security prompts.

## What this does

Windows 11 Smart App Control and SmartScreen may block the updater from replacing the `BerryGoodUtils.exe` file and relaunching it. Disabling Smart App Control and allowing the app once solves this.

## Manual steps for your friend

### Step 1: Create a folder for the app

Create a folder such as:

```
C:\Users\<TheirUserName>\BerryGoodUtils
```

Replace `<TheirUserName>` with their Windows user name.

### Step 2: Download the app

Download `BerryGoodUtils.exe` and move it into the folder you just created.

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

$folder = Read-Host "Enter the full path where BerryGoodUtils.exe is saved (for example, C:\Users\John\BerryGoodUtils)"
$exe = Join-Path $folder "BerryGoodUtils.exe"

if (-not (Test-Path $exe)) {
    throw "Could not find BerryGoodUtils.exe at $exe"
}

Unblock-File -Path $exe

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
