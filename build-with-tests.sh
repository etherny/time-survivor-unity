#!/bin/bash

# Build script with automated tests
# Usage: ./build-with-tests.sh

PROJECT_PATH="/Users/etherny/Documents/work/games/TimeSurvivorGame"
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.2.12f1/Unity.app/Contents/MacOS/Unity"
TEST_RESULTS="TestResults.xml"

echo "========================================="
echo "  Unity Build with Tests"
echo "========================================="
echo ""

# Step 1: Run EditMode tests
echo "[1/3] Running EditMode tests..."
$UNITY_PATH \
  -runTests \
  -batchmode \
  -projectPath "$PROJECT_PATH" \
  -testResults "$TEST_RESULTS" \
  -testPlatform EditMode \
  -logFile test.log

TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo ""
    echo "❌ TESTS FAILED (Exit code: $TEST_EXIT_CODE)"
    echo ""
    echo "Test results saved to: $TEST_RESULTS"
    echo "Test logs saved to: test.log"
    echo ""
    echo "Please fix failing tests before building."
    exit 1
fi

echo "✅ All tests passed!"
echo ""

# Step 2: Check test results XML for failed tests
if command -v xmllint &> /dev/null; then
    FAILED_TESTS=$(xmllint --xpath "//test-run/@failed" "$TEST_RESULTS" 2>/dev/null | grep -o '[0-9]*')
    if [ "$FAILED_TESTS" != "0" ]; then
        echo "❌ $FAILED_TESTS test(s) failed"
        echo "See $TEST_RESULTS for details"
        exit 1
    fi
fi

# Step 3: Build the project
echo "[2/3] Compiling project..."
$UNITY_PATH \
  -quit -batchmode -nographics \
  -projectPath "$PROJECT_PATH" \
  -logFile compile.log

BUILD_EXIT_CODE=$?

if [ $BUILD_EXIT_CODE -ne 0 ]; then
    echo ""
    echo "❌ BUILD FAILED (Exit code: $BUILD_EXIT_CODE)"
    echo ""
    echo "Build logs saved to: compile.log"
    exit 1
fi

echo "✅ Build completed successfully!"
echo ""

# Step 4: Summary
echo "[3/3] Build Summary"
echo "========================================="
echo "✅ Tests passed"
echo "✅ Compilation succeeded"
echo ""
echo "Logs:"
echo "  - Tests: test.log"
echo "  - Build: compile.log"
echo "  - Test Results: $TEST_RESULTS"
echo "========================================="

exit 0
