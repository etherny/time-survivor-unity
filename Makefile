# Unity Project Build Automation
# Usage: make [target]

# Get the directory where this Makefile is located
MAKEFILE_DIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))

.PHONY: help test build build-with-tests clean

# Default target
help:
	@echo "Available targets:"
	@echo "  make test              - Run EditMode tests only"
	@echo "  make test-play         - Run PlayMode tests"
	@echo "  make build             - Compile project (no tests)"
	@echo "  make build-with-tests  - Run tests + build (recommended)"
	@echo "  make clean             - Clean test results and logs"

# Run EditMode tests
test:
	@cd "$(MAKEFILE_DIR)" && ./run-tests.sh EditMode

# Run PlayMode tests
test-play:
	@cd "$(MAKEFILE_DIR)" && ./run-tests.sh PlayMode

# Build without tests (fast)
build:
	@echo "Building project (skipping tests)..."
	@cd "$(MAKEFILE_DIR)" && /Applications/Unity/Hub/Editor/6000.2.12f1/Unity.app/Contents/MacOS/Unity \
		-quit -batchmode -nographics \
		-projectPath "$(MAKEFILE_DIR)" \
		-logFile compile.log
	@echo "✅ Build complete. Logs: compile.log"

# Build with tests (recommended for CI/CD)
build-with-tests:
	@cd "$(MAKEFILE_DIR)" && ./build-with-tests.sh

# Clean generated files
clean:
	@echo "Cleaning test results and logs..."
	@cd "$(MAKEFILE_DIR)" && rm -f TestResults.xml test.log compile.log
	@echo "✅ Clean complete"
