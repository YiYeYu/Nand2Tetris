
for file in $(find ./projects/6 -name "*.asm"); do
    dotnet run --project ./Assembler $file
done
