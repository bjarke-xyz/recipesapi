#!/usr/bin/env bash

docker run --rm -v "$PWD/output:/app/output" -v "$PWD/conf:/root/.config/rclone" frida