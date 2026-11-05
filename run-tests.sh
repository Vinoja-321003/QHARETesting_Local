#!/bin/bash

# QualiFrame C# NUnit Test Runner Script
# This script provides various options for running tests

echo "QualiFrame C# NUnit Test Runner"
echo "================================"

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help              Show this help message"
    echo "  -a, --all               Run all tests"
    echo "  -w, --web               Run Web tests only"
    echo "  -api, --api             Run API tests only"
    echo "  -m, --mobile            Run Mobile tests only"
    echo "  -p, --parallel          Run tests in parallel"
    echo "  -c, --config CONFIG     Use specific configuration (Debug/Release)"
    echo "  -f, --filter FILTER     Run tests matching filter"
    echo "  -r, --report            Generate reports after test run"
    echo "  -v, --verbose           Verbose output"
    echo "  -clean                  Clean build before running tests"
    echo ""
    echo "Examples:"
    echo "  $0 --all                    # Run all tests"
    echo "  $0 --web --parallel         # Run Web tests in parallel"
    echo "  $0 --filter \"Category=API\"  # Run API tests only"
    echo "  $0 --config Release         # Run tests in Release mode"
}

# Default values
RUN_ALL=false
RUN_WEB=false
RUN_API=false
RUN_MOBILE=false
RUN_PARALLEL=false
CONFIG="Debug"
FILTER=""
GENERATE_REPORTS=false
VERBOSE=false
CLEAN=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            usage
            exit 0
            ;;
        -a|--all)
            RUN_ALL=true
            shift
            ;;
        -w|--web)
            RUN_WEB=true
            shift
            ;;
        -api|--api)
            RUN_API=true
            shift
            ;;
        -m|--mobile)
            RUN_MOBILE=true
            shift
            ;;
        -p|--parallel)
            RUN_PARALLEL=true
            shift
            ;;
        -c|--config)
            CONFIG="$2"
            shift 2
            ;;
        -f|--filter)
            FILTER="$2"
            shift 2
            ;;
        -r|--report)
            GENERATE_REPORTS=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -clean)
            CLEAN=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# If no specific test type is selected, run all
if [[ "$RUN_WEB" == false && "$RUN_API" == false && "$RUN_MOBILE" == false ]]; then
    RUN_ALL=true
fi

TEST_PROJECT="src/QualiFrame.Tests/QualiFrame.Tests.csproj"

# Build the dotnet test command (target test project only — avoids MTP/Runner conflicts)
DOTNET_CMD="dotnet test \"$TEST_PROJECT\""

# Add configuration
DOTNET_CMD="$DOTNET_CMD --configuration $CONFIG"

# Add parallel execution
if [[ "$RUN_PARALLEL" == true ]]; then
    DOTNET_CMD="$DOTNET_CMD --parallel"
fi

# Add filter based on test type
if [[ "$RUN_ALL" == true ]]; then
    echo "Running all tests..."
elif [[ "$RUN_WEB" == true ]]; then
    echo "Running Web tests..."
    if [[ -n "$FILTER" ]]; then
        DOTNET_CMD="$DOTNET_CMD --filter \"Category=Web&$FILTER\""
    else
        DOTNET_CMD="$DOTNET_CMD --filter \"Category=Web\""
    fi
elif [[ "$RUN_API" == true ]]; then
    echo "Running API tests..."
    if [[ -n "$FILTER" ]]; then
        DOTNET_CMD="$DOTNET_CMD --filter \"Category=API&$FILTER\""
    else
        DOTNET_CMD="$DOTNET_CMD --filter \"Category=API\""
    fi
elif [[ "$RUN_MOBILE" == true ]]; then
    echo "Running Mobile tests..."
    if [[ -n "$FILTER" ]]; then
        DOTNET_CMD="$DOTNET_CMD --filter \"Category=Mobile&$FILTER\""
    else
        DOTNET_CMD="$DOTNET_CMD --filter \"Category=Mobile\""
    fi
fi

# Add custom filter if provided and no test type filter
if [[ -n "$FILTER" && "$RUN_WEB" == false && "$RUN_API" == false && "$RUN_MOBILE" == false ]]; then
    DOTNET_CMD="$DOTNET_CMD --filter \"$FILTER\""
fi

# Add verbose output
if [[ "$VERBOSE" == true ]]; then
    DOTNET_CMD="$DOTNET_CMD --logger \"console;verbosity=detailed\""
fi

# Clean build if requested
if [[ "$CLEAN" == true ]]; then
    echo "Cleaning build..."
    dotnet clean "$TEST_PROJECT" --configuration "$CONFIG"
    dotnet restore "$TEST_PROJECT"
fi

# Create Results directory if it doesn't exist
mkdir -p Results/Screenshots
mkdir -p Results/Reports
mkdir -p Results/Logs
mkdir -p Results/AllureResults

# Run the tests
echo "Executing: $DOTNET_CMD"
echo ""

# Execute the command
eval $DOTNET_CMD

# Capture exit code
EXIT_CODE=$?

# Generate reports if requested and tests passed
if [[ "$GENERATE_REPORTS" == true && "$EXIT_CODE" == 0 ]]; then
    echo ""
    echo "Generating reports..."
    
    # Generate Allure report if Allure is installed
    if command -v allure &> /dev/null; then
        echo "Generating Allure report..."
        allure generate Results/AllureResults -o Results/AllureReport --clean
        echo "Allure report generated: Results/AllureReport/index.html"
    else
        echo "Allure not found. Install Allure to generate Allure reports."
    fi
    
    echo "Extent report available: Results/Reports/ExtentReport.html"
fi

# Display results
echo ""
echo "Test execution completed with exit code: $EXIT_CODE"

if [[ "$EXIT_CODE" == 0 ]]; then
    echo "✅ All tests passed!"
else
    echo "❌ Some tests failed!"
fi

echo ""
echo "Reports location:"
echo "  - Extent Report: Results/Reports/ExtentReport.html"
echo "  - Screenshots: Results/Screenshots/"
echo "  - Logs: Results/Logs/"

if [[ "$GENERATE_REPORTS" == true && "$EXIT_CODE" == 0 ]]; then
    echo "  - Allure Report: Results/AllureReport/index.html"
fi

exit $EXIT_CODE