# Week 4 Part D — remote Windows steps

This replacement delivery adds API security and MAUI to baseline week4-partD at 1f84abf. It was reconstructed after the earlier temporary ZIP became unavailable. Its verification logs describe this replacement only. It changes no database model/migration or Week 3 business rules. Do not merge before native validation.

## 1. Apply

Close Visual Studio. Extract the delivery ZIP outside ApexPost. In Developer PowerShell at `C:\DotNetTraining\Day_1\ApexPost`, run the extracted Apply-Week4D.ps1 with `powershell -NoProfile -ExecutionPolicy Bypass -File "FULL-PATH-TO-Apply-Week4D.ps1"`. It requires clean week4-partD at the exact baseline and checks before applying. It never commits, pushes, migrates or merges.

## 2. Configure and verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\partD\Setup-PartD.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\partD\Verify-PartD.ps1 -WithLocalDb -BuildWindows
```

Setup preserves the existing connection and keys. If no API connection exists, rerun with your existing database, for example:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\partD\Setup-PartD.ps1 -ConnectionString 'Server=(localdb)\MSSQLLocalDB;Database=ApexPostDb;Trusted_Connection=True;TrustServerCertificate=True'
```

Tests create/drop disposable test databases using master; they do not reset the app database. Without SQL Server, 23 database-dependent tests skip (22 Week 3 and one API delivery test). Skipped tests do not verify persistence. The separate native solution keeps the server build independent of MAUI workloads. Your remote machine already listed maui-windows; if needed install .NET Multi-platform App UI development in Visual Studio Installer.

## 3. API and token

Leave this running in the first terminal:

```powershell
dotnet run --project ApexPost.Api --launch-profile https
```

In a second terminal:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\partD\Copy-DevelopmentToken.ps1
```

Open https://localhost:7268/swagger, choose v2, Authorize → Bearer, paste the clipboard token without the word Bearer. Create fictional shipment APX-12345678 using the existing POST and fictional recipient/address/phone, weight 1.25. If it already exists, use it or another APX-eight-digit identifier. The token expires in 15 minutes; obtain another when needed. Mask credentials in screenshots/video and clear the clipboard after pasting.

## 4. MAUI Windows

Open **ApexPost.Mobile.slnx** in Visual Studio. Set ApexPost.Courier as Startup Project, choose Windows Machine, F5. Keep the API running. Login: paste the Courier token. New Scan: the created tracking number, Pickup, fictional location. Save locally; inspect Pending Scans and Sync Status. Later scans must obey existing InTransit/Delivered transitions.

Alternatively:

```powershell
dotnet build ApexPost.Courier -t:Run -f net10.0-windows10.0.19041.0
```

The Windows Work offline switch safely simulates disconnection. Save scans then disable it: the worker wakes and uploads batches of 20 or fewer per shipment. **Never disconnect the RDP host's network.** Literal airplane-mode evidence requires an Android emulator/device. See SECURITY.md for the extra Android workload/certificate setup.

Stop the API while at least one scan is Pending, leave online mode enabled, Sync now: the circuit opens after repeated failures and the offline banner appears. Restart the API; after its 10-second break and periodic wake, a successful probe closes it and sync resumes.

## 5. Finish evidence

Follow docs/week4-partD/VERIFICATION.md for auth cases, native UI, LocalDB and the required 3–5 minute video. No video or native runtime evidence is supplied. Review changes before committing/pushing week4-partD and opening the PR. No automatic push/merge occurs.
