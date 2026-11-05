@echo off
REM QualiFrame C# NUnit Test Runner Script for Windows
REM This script provides various options for running tests

echo QualiFrame C# NUnit Test Runner
echo ================================

REM Default values
set RUN_ALL=false
set RUN_WEB=false
set RUN_API=false
set RUN_MOBILE=false
set RUN_PARALLEL=false
set CONFIG=Debug
set FILTER=
set GENERATE_REPORTS=false
set VERBOSE=false
set CLEAN=false

REM Parse command line arguments
:parse_args
if "%~1"=="" goto :run_tests
if "%~1"=="-h" goto :show_help
if "%~1"=="--help" goto :show_help
if "%~1"=="-a" set RUN_ALL=true
if "%~1"=="--all" set RUN_ALL=true
if "%~1"=="-w" set RUN_WEB=true
if "%~1"=="--web" set RUN_WEB=true
if "%~1"=="-api" set RUN_API=true
if "%~1"=="--api" set RUN_API=true
if "%~1"=="-m" set RUN_MOBILE=true
if "%~1"=="--mobile" set RUN_MOBILE=true
if "%~1"=="-p" set RUN_PARALLEL=true
if "%~1"=="--parallel" set RUN_PARALLEL=true
if "%~1"=="-c" (
    set CONFIG=%~2
    shift
)
if "%~1"=="--config" (
    set CONFIG=%~2
    shift
)
if "%~1"=="-f" (
    set FILTER=%~2
    shift
)
if "%~1"=="--filter" (
    set FILTER=%~2
    shift
)
if "%~1"=="-r" set GENERATE_REPORTS=true
if "%~1"=="--report" set GENERATE_REPORTS=true
if "%~1"=="-v" set VERBOSE=true
if "%~1"=="--verbose" set VERBOSE=true
if "%~1"=="-clean" set CLEAN=true
shift
goto :parse_args

:show_help
echo Usage: %0 [OPTIONS]
echo.
echo Options:
echo   -h, --help              Show this help message
echo   -a, --all               Run all tests
echo   -w, --web               Run Web tests only
echo   -api, --api             Run API tests only
echo   -m, --mobile            Run Mobile tests only
echo   -p, --parallel          Run tests in parallel
echo   -c, --config CONFIG     Use specific configuration (Debug/Release)
echo   -f, --filter FILTER     Run tests matching filter
echo   -r, --report            Generate reports after test run
echo   -v, --verbose           Verbose output
echo   -clean                  Clean build before running tests
echo.
echo Examples:
echo   %0 --all                    # Run all tests
echo   %0 --web --parallel         # Run Web tests in parallel
echo   %0 --filter "Category=API"  # Run API tests only
echo   %0 --config Release         # Run tests in Release mode
goto :end

:run_tests
REM If no specific test type is selected, run all
if "%RUN_WEB%"=="false" if "%RUN_API%"=="false" if "%RUN_MOBILE%"=="false" set RUN_ALL=true

REM Build the dotnet test command
set DOTNET_CMD=dotnet test

REM Add configuration
set DOTNET_CMD=%DOTNET_CMD% --configuration %CONFIG%

REM Add parallel execution
if "%RUN_PARALLEL%"=="true" set DOTNET_CMD=%DOTNET_CMD% --parallel

REM Add filter based on test type
if "%RUN_ALL%"=="true" (
    echo Running all tests...
) else if "%RUN_WEB%"=="true" (
    echo Running Web tests...
    if not "%FILTER%"=="" (
        set DOTNET_CMD=%DOTNET_CMD% --filter "Category=Web&%FILTER%"
    ) else (
        set DOTNET_CMD=%DOTNET_CMD% --filter "Category=Web"
    )
) else if "%RUN_API%"=="true" (
    echo Running API tests...
    if not "%FILTER%"=="" (
        set DOTNET_CMD=%DOTNET_CMD% --filter "Category=API&%FILTER%"
    ) else (
        set DOTNET_CMD=%DOTNET_CMD% --filter "Category=API"
    )
) else if "%RUN_MOBILE%"=="true" (
    echo Running Mobile tests...
    if not "%FILTER%"=="" (
        set DOTNET_CMD=%DOTNET_CMD% --filter "Category=Mobile&%FILTER%"
    ) else (
        set DOTNET_CMD=%DOTNET_CMD% --filter "Category=Mobile"
    )
)

REM Add custom filter if provided and no test type filter
if not "%FILTER%"=="" if "%RUN_WEB%"=="false" if "%RUN_API%"=="false" if "%RUN_MOBILE%"=="false" (
    set DOTNET_CMD=%DOTNET_CMD% --filter "%FILTER%"
)

REM Add verbose output
if "%VERBOSE%"=="true" set DOTNET_CMD=%DOTNET_CMD% --logger "console;verbosity=detailed"

REM Clean build if requested
if "%CLEAN%"=="true" (
    echo Cleaning build...
    dotnet clean
    dotnet restore
)

REM Create Results directory if it doesn't exist
if not exist "Results" mkdir Results
if not exist "Results\Screenshots" mkdir Results\Screenshots
if not exist "Results\Reports" mkdir Results\Reports
if not exist "Results\Logs" mkdir Results\Logs
if not exist "Results\AllureResults" mkdir Results\AllureResults

REM Run the tests
echo Executing: %DOTNET_CMD%
echo.

REM Execute the command
%DOTNET_CMD%

REM Capture exit code
set EXIT_CODE=%ERRORLEVEL%

REM Generate reports if requested and tests passed
if "%GENERATE_REPORTS%"=="true" if "%EXIT_CODE%"=="0" (
    echo.
    echo Generating reports...
    
    REM Generate Allure report if Allure is installed
    where allure >nul 2>nul
    if %ERRORLEVEL%==0 (
        echo Generating Allure report...
        allure generate Results\AllureResults -o Results\AllureReport --clean
        echo Allure report generated: Results\AllureReport\index.html
    ) else (
        echo Allure not found. Install Allure to generate Allure reports.
    )
    
    echo Extent report available: Results\Reports\ExtentReport.html
)

REM Display results
echo.
echo Test execution completed with exit code: %EXIT_CODE%

if "%EXIT_CODE%"=="0" (
    echo ✅ All tests passed!
) else (
    echo ❌ Some tests failed!
)

echo.
echo Reports location:
echo   - Extent Report: Results\Reports\ExtentReport.html
echo   - Screenshots: Results\Screenshots\
echo   - Logs: Results\Logs\

if "%GENERATE_REPORTS%"=="true" if "%EXIT_CODE%"=="0" (
    echo   - Allure Report: Results\AllureReport\index.html
)

:end
exit /b %EXIT_CODE%