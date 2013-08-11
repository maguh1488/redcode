@echo off

for /d %%f in (Cache*) do rmdir %%f /s /q
for /d %%f in (CompiledAssemblies*) do rmdir %%f /s /q

rmdir /s /q "C:\Program Files (x86)\World of Warcraft\Cache"
rmdir /s /q "C:\Program Files (x86)\World of Warcraft\Data\Cache"

rmdir /s /q "C:\Program Files (x86)\World of Warcraft\Errors"
rmdir /s /q "C:\Program Files (x86)\World of Warcraft\Logs"
del "C:\Program Files (x86)\World of Warcraft\WoW.mfil"
del "C:\Program Files (x86)\World of Warcraft\WoW.pfil"
del "C:\Program Files (x86)\World of Warcraft\WoW.tfil"

"C:\Program Files (x86)\World of Warcraft\World of Warcraft Launcher.exe"
