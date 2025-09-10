#!/bin/bash

for dir in $(find ./projects/7 -mindepth 1 -maxdepth 2 -type d); do
    dotnet run --project ./VM2Hack $dir
done
