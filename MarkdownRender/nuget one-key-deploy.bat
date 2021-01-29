@ECHO OFF
del /q  *.nupkg && nuget pack && nuget  push  *.nupkg -Apikey oy2avphpqbs27wdggrvqbjn3sj4yi5muzdijgs3etds56a -Source https://api.nuget.org/v3/index.json
@PAUSE