#!/usr/bin/env bash
set -euo pipefail

APP_NAME=${1:-ecs-multiplayer-prototype}
IMAGE_TAG=${2:-latest}
TARGET_PLATFORM="linux/amd64"

# Automatically detect native platform for the build stage
NATIVE_ARCH=$(uname -m)
if [[ "$NATIVE_ARCH" == "x86_64" ]]; then
  NATIVE_PLATFORM="linux/amd64"
else
  NATIVE_PLATFORM="linux/arm64"
fi

heroku container:login

DOCKER_IMAGE=registry.heroku.com/${APP_NAME}/web
REPO_ROOT_DIR=$(cd "$(dirname "$0")/.." && pwd)

echo "Building and pushing Docker image for target ${TARGET_PLATFORM}..."

docker buildx build \
  --builder heroku-builder \
  --platform "${TARGET_PLATFORM}" \
  --build-arg BUILDPLATFORM=${NATIVE_PLATFORM} \
  --build-arg TARGETPLATFORM=${TARGET_PLATFORM} \
  -f "$REPO_ROOT_DIR/deploy/Dockerfile" \
  -t "${DOCKER_IMAGE}:${IMAGE_TAG}" \
  --push \
  "$REPO_ROOT_DIR"

echo "Releasing app ${APP_NAME}..."
heroku container:release web --app "${APP_NAME}"

echo "Deployment complete."