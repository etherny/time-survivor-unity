#!/bin/bash

# Quick test runner script
# Usage: ./run-tests.sh [EditMode|PlayMode]

PROJECT_PATH="/Users/etherny/Documents/work/games/TimeSurvivorGame"
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.2.12f1/Unity.app/Contents/MacOS/Unity"
TEST_PLATFORM="${1:-EditMode}"  # Default to EditMode if no argument
TEST_RESULTS="TestResults.xml"

echo "========================================="
echo "  Unity Test Runner ($TEST_PLATFORM)"
echo "========================================="
echo ""

$UNITY_PATH \
  -runTests \
  -batchmode \
  -projectPath "$PROJECT_PATH" \
  -testResults "$TEST_RESULTS" \
  -testPlatform "$TEST_PLATFORM" \
  -logFile test.log

EXIT_CODE=$?

echo ""
echo "========================================="

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Tests completed successfully"
else
    echo "❌ Tests failed (Exit code: $EXIT_CODE)"
fi

echo ""
echo "Results saved to:"
echo "  - Test Results: $TEST_RESULTS"
echo "  - Logs: test.log"
echo "========================================="

# Display test summary if xmllint is available
if command -v xmllint &> /dev/null && [ -f "$TEST_RESULTS" ]; then
    echo ""
    echo "Test Summary:"
    TOTAL=$(xmllint --xpath "//test-run/@total" "$TEST_RESULTS" 2>/dev/null | grep -o '[0-9]*' || echo "?")
    PASSED=$(xmllint --xpath "//test-run/@passed" "$TEST_RESULTS" 2>/dev/null | grep -o '[0-9]*' || echo "?")
    FAILED=$(xmllint --xpath "//test-run/@failed" "$TEST_RESULTS" 2>/dev/null | grep -o '[0-9]*' || echo "?")

    echo "  Total: $TOTAL"
    echo "  Passed: $PASSED"
    echo "  Failed: $FAILED"
    echo ""
fi

exit $EXIT_CODE
