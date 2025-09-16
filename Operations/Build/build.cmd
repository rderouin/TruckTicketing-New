@REM echo  _______              _      _______ _      _        _   _             
@REM echo |__   __|            | |    |__   __(_)    | |      | | (_)            
@REM echo    | |_ __ _   _  ___| | __    | |   _  ___| | _____| |_ _ _ __   __ _ 
@REM echo    | | '__| | | |/ __| |/ /    | |  | |/ __| |/ / _ \ __| | '_ \ / _` |
@REM echo    | | |  | |_| | (__|   <     | |  | | (__|   <  __/ |_| | | | | (_| |
@REM echo    |_|_|   \__,_|\___|_|\_\    |_|  |_|\___|_|\_\___|\__|_|_| |_|\__, |
@REM echo                                                                   __/ |
@REM echo      

@echo off
timeout 1 > nul
:menu
echo.
echo                    TRUCK TICKETING LOCAL BUILD UTILITY
echo.
echo PROJECTS:
echo 1 : Billing Service
echo 2 : Integration Service
echo 3 : SE.TruckTicketing
echo 4 : Token Service
echo 5 : All Services
echo 6 : CosmosDB
echo.
echo ENVIRONMENT:
echo devl: Dev-local
echo devi: Dev-int
echo.
echo OTHER
echo q: Quit

SET /P APPSELECTION=Select Application(s):
@rem if /i {%ANSWER%}=={q} {goto:quit}
@rem cls
@echo off
echo.
REM Selection
if /i {%APPSELECTION%}=={1} (goto:billingService)
if /i {%APPSELECTION%}=={2} (goto:integrationService)
if /i {%APPSELECTION%}=={3} (goto:truckTicketingApp)
if /i {%APPSELECTION%}=={4} (goto:tokenService)
if /i {%APPSELECTION%}=={5} (goto:allApplication)
if /i {%APPSELECTION%}=={6} (goto:cosmosDbWrapper)
if /i {%APPSELECTION%}=={cs} (goto:createDBwithSeed)

REM ************ CosmosDB Wrapper ****************
:cosmosDbWrapper
echo COSMOS DB:
echo.
echo OPERATIONS:
echo crdb: Create Database 
echo dldb: Delete Database
echo dlc: Delete Container
echo r: Rebuild Operations
echo.
SET /P ANSWER=Enter a Choice:
echo.
echo You Chose: %ANSWER%
if /i {%ANSWER%}=={crdb} (goto:createDatabase)
if /i {%ANSWER%}=={cdc} (goto:createContainer)
if /i {%ANSWER%}=={sc} (goto:scale)
if /i {%ANSWER%}=={dldb} (goto:deleteDatabase)
if /i {%ANSWER%}=={dlc} (goto:deleteContainer)
if /i {%ANSWER%}=={r} (goto:rebuildOps)

:rebuildOps
echo.
SET /P REBUILD=Rebuild All (Y/N)?
if /i {%REBUILD%}=={Y} (
	CALL :cosmosUserSelection "rebuild-all" "" "" ""
)
if /i {%REBUILD%}=={N} (
	goto:rebuildSelection
)

:rebuildSelection
echo.
echo SAMPLE COMMANDS:
echo.
SET /P CONTAINERNAME=Enter Conatiner Name:
SET /P ISSOFT=Is Soft Rebuild (Y/N)?:
SET /P ISUPDATE=Overwrite when Rebuild (Y/N)?:
CALL :cosmosUserSelection "rebuild" "TT" "" %CONTAINERNAME% %ISSOFT% %ISUPDATE% 

:createDatabase
echo.
SET /P DATABASENAME=Enter Database Name:
echo Entered Database: %DATABASENAME%
CALL :cosmosUserSelection "create_db" "TT" %DATABASENAME% ""

:createContainer
echo.
SET /P DATABASENAME=Enter Database Name:
SET /P CONTAINERNAME=Enter Conatiner Name:
CALL :cosmosUserSelection "delete" "TT" %DATABASENAME% %CONTAINERNAME%

:deleteDatabase
echo.
SET /P DATABASENAME=Enter Database Name:
CALL :cosmosUserSelection "delete" "TT" %DATABASENAME% ""

:deleteContainer
echo.
SET /P DATABASENAME=Enter Database Name:
SET /P CONTAINERNAME=Enter Conatiner Name:
CALL :cosmosUserSelection "delete" "TT" %DATABASENAME% %CONTAINERNAME%

:cosmosUserSelection
CALL powershell -file .\Cosmos-db-cli.ps1 %1 %2 %3 %4 %5 %6
GOTO :menu

REM ************ Billing Service ****************
:billingService
CALL :displayCommands
SET /P ANSWER=Enter a Choice:
echo.
echo You Chose: %ANSWER% 
CALL :userSelection "BillingService" "%ANSWER%"
GOTO :menu

REM ************ Integration Service ****************
:integrationService
CALL :displayCommands
SET /P ANSWER=Enter a Choice:
echo.
echo You Chose: %ANSWER%
CALL :userSelection "Integrations" "%ANSWER%"
GOTO :menu

REM ************ TruckTicketing Application ****************
:truckTicketingApp
CALL :displayCommands
SET /P ANSWER=Enter a Choice:
echo.
echo You Chose: %ANSWER%
CALL :userSelection "TruckTicketing" "%ANSWER%"
GOTO :menu

REM ************ Token Service ****************
:tokenService
CALL :displayCommands
SET /P ANSWER=Enter a Choice:
echo.
echo You Chose: %ANSWER%
CALL :userSelection "TokenService" "%ANSWER%"
GOTO :menu

REM ************ Select All ****************
:allApplication
CALL :displayCommands
SET /P ANSWER=Enter a Choice:
echo.
echo You Chose: %ANSWER%
CALL :userSelection "BillingService" "%ANSWER%"
CALL :userSelection "Integrations" "%ANSWER%"
CALL :userSelection "TruckTicketing" "%ANSWER%"
CALL :userSelection "TokenService" "%ANSWER%"
GOTO :menu

:userSelection
CALL powershell -file .\Backend-build.ps1 %1 %2

:displayCommands
echo.
echo OPERATIONS:
echo b: Build
echo r: Restore
echo c: Clean
echo t: Test
echo cc: Code Coverage
echo all: All of the Above

REM :userSelection
REM for %%a in (%ANSWER%) do(
	REM set /p operation = %%a
	REM echo operation
	REM if /i {%operation%}=={b} (goto:performBuild)
	REM if /i {%operation%}=={c} (goto:performClean)
	REM if /i {%operation%}=={r} (goto:performRestore)
	REM if /i {%operation%}=={t} (goto:performTest)
	REM if /i {%operation%}=={cc} (goto:performCoverage)
REM )


REM if /i {%ANSWER%}=={bb} (goto:backendBuild)
REM if /i {%ANSWER%}=={bp} (goto:backendPartial)
REM if /i {%ANSWER%}=={bt} (goto:backendTest)
REM if /i {%ANSWER%}=={bc} (goto:backendClean)