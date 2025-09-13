#!/bin/bash

for dir in $(find ./projects/10 -mindepth 1 -maxdepth 2 -type d); do
    dotnet run --project ./Compiler $dir
done
