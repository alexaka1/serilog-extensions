dotnet pack src/Serilog.Extensions.Formatting -c Relase --version-suffix beta -o ./artifacts
dotnet nuget push ./artifacts/*.nupkg -s https://api.nuget.org/v3/index.json -k $NUGET_API_KEY
