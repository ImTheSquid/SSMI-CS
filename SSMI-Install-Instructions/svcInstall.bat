@echo OFF

:check_Permissions
    echo Administrative permissions required. Detecting permissions...

    net session >nul 2>&1
    if %errorLevel% == 0 (
        echo Success: Administrative permissions confirmed.
		goto INSTALL
    ) else (
        echo Failure: Current permissions inadequate.
		goto END
    )

goto check_Permissions

set _ServiceName=SSMIService

sc query %_ServiceName% | find "does not exist" >nul
if %ERRORLEVEL% EQU 1 goto NOACTION

:INSTALL
cd /d %~dp0
if not exist SSMediaIntegration.exe (
	echo Could not find SSMediaIntegration.exe. Please make sure SSMIMediaIntegration.exe is in the same directory as svcInstall.bat.
) else (
	SSMediaIntegration.exe /i
	echo Service installed.
)
goto END

:NOACTION
echo Service already installed.

:END
echo Done.
pause