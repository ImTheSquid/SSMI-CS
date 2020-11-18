@echo OFF

:check_Permissions
    echo Administrative permissions required. Detecting permissions...

    net session >nul 2>&1
    if %errorLevel% == 0 (
        echo Success: Administrative permissions confirmed.
		goto UNINSTALL
    ) else (
        echo Failure: Current permissions inadequate.
		goto END
    )

goto check_Permissions

set _ServiceName=SSMIService

sc query %_ServiceName% | find "does not exist" >nul
if %ERRORLEVEL% EQU 0 goto NOACTION

:UNINSTALL
if not exist SSMediaIntegration.exe (
	echo Could not find SSMediaIntegration.exe. Please access this file via cmd.
) else (
	SSMediaIntegration.exe /u
	echo Service uninstalled.
)
goto END

:NOACTION
echo Service not installed.

:END
echo Done.
pause