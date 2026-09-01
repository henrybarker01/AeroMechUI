#!/usr/bin/env bash
# Deploys main or any branch to the test environment (https://test.vmi.aeromech.co).
#
# Usage: ./deploy-test.sh [branch]   (defaults to main)
#
# Run from the production checkout on the Lightsail instance. Requires a separate
# test checkout of the repo at TEST_BUILD_CONTEXT (default ../AeroMechUI-test):
#   git clone https://github.com/henrybarker01/AeroMechUI.git ../AeroMechUI-test
set -euo pipefail

BRANCH="${1:-main}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Read TEST_BUILD_CONTEXT from .env if set, otherwise use the default
TEST_BUILD_CONTEXT="$(grep -E '^TEST_BUILD_CONTEXT=' .env 2>/dev/null | cut -d= -f2- || true)"
TEST_BUILD_CONTEXT="${TEST_BUILD_CONTEXT:-../AeroMechUI-test}"

if [ ! -d "$TEST_BUILD_CONTEXT/.git" ]; then
    echo "Test checkout not found at $TEST_BUILD_CONTEXT" >&2
    echo "Create it with: git clone <repo-url> $TEST_BUILD_CONTEXT" >&2
    exit 1
fi

echo "Updating test checkout ($TEST_BUILD_CONTEXT) to $BRANCH..."
git -C "$TEST_BUILD_CONTEXT" fetch origin --prune
git -C "$TEST_BUILD_CONTEXT" checkout "$BRANCH"
git -C "$TEST_BUILD_CONTEXT" reset --hard "origin/$BRANCH"

echo "Building and starting web-test..."
docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.test.yml up -d --build web-test

echo "Reloading Caddy config..."
docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.test.yml exec caddy caddy reload --config /etc/caddy/Caddyfile

echo "Done. Test environment is running $BRANCH at https://test.vmi.aeromech.co"
