#!/bin/bash
# This script runs all tests, collects code coverage, and generates an HTML report.
set -e

# Define the output directory for the report
REPORT_DIR="coveragereport"

echo "Running tests and collecting code coverage..."

# Run tests and collect coverage data in the Cobertura format.
# The --results-directory is specified to make the output predictable.
dotnet test --collect:"XPlat Code Coverage" --results-directory ./.testresults

# Find the coverage file. It's usually in a directory with a random name.
COVERAGE_FILE=$(find ./.testresults -name "coverage.cobertura.xml" | head -n 1)

if [ -z "$COVERAGE_FILE" ]; then
    echo "Error: Coverage file not found."
    echo "Please ensure that tests ran successfully and that the coverage data was collected."
    exit 1
fi

echo "Found coverage file: $COVERAGE_FILE"

# Clean up the previous report if it exists
if [ -d "$REPORT_DIR" ]; then
    echo "Removing old coverage report directory..."
    rm -rf "$REPORT_DIR"
fi

echo "Generating HTML coverage report..."

# Generate the report using the reportgenerator tool
reportgenerator \
    -reports:"$COVERAGE_FILE" \
    -targetdir:"$REPORT_DIR" \
    -reporttypes:Html

echo "✅ Coverage report generated successfully!"
echo "Open the following file in your browser to view the report:"
echo "file://$(pwd)/$REPORT_DIR/index.html"

