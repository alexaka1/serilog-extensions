dotnet pack src/Serilog.Extensions.Formatting -c Release -o ./artifacts
dotnet nuget push ./artifacts/*.nupkg -s https://api.nuget.org/v3/index.json -k $NUGET_API_KEY
