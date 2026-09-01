#!/usr/bin/env bash
# Deploys master or any branch to the test environment (https://test.vmi.aeromech.co).
#
# Usage: ./deploy-test.sh [branch]   (defaults to master)
#
# Run from the production checkout on the Lightsail instance. Requires a separate
# test checkout of the repo at TEST_BUILD_CONTEXT (default ../AeroMechUI-test):
#   git clone https://github.com/henrybarker01/AeroMechUI.git ../AeroMechUI-test
set -euo pipefail

BRANCH="${1:-master}"
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

if ! git -C "$TEST_BUILD_CONTEXT" rev-parse --verify --quiet "origin/$BRANCH" >/dev/null; then
    echo "Branch '$BRANCH' not found on origin." >&2
    echo "Available branches:" >&2
    git -C "$TEST_BUILD_CONTEXT" branch -r --format='  %(refname:short)' | sed 's|origin/||' >&2
    exit 1
fi

git -C "$TEST_BUILD_CONTEXT" checkout "$BRANCH"
git -C "$TEST_BUILD_CONTEXT" reset --hard "origin/$BRANCH"

COMPOSE=(docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.test.yml)

echo "Building and starting web-test..."
"${COMPOSE[@]}" up -d --build web-test

echo "Reloading Caddy config..."
# Make sure caddy is up (and created with the test overlay applied)
"${COMPOSE[@]}" up -d caddy
# `git pull` replaces the Caddyfile with a new inode, but a single-file bind
# mount keeps pointing at the old one, so `caddy reload` inside the container
# can silently reload stale config. Restart caddy when its copy is out of date.
if "${COMPOSE[@]}" exec -T caddy cat /etc/caddy/Caddyfile | cmp -s - Caddyfile; then
    "${COMPOSE[@]}" exec caddy caddy reload --config /etc/caddy/Caddyfile
else
    echo "Caddyfile in the container is stale; restarting caddy to remount it..."
    "${COMPOSE[@]}" restart caddy
fi

echo "Done. Test environment is running $BRANCH at https://test.vmi.aeromech.co"
