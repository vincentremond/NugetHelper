@ECHO OFF

dotnet tool restore
dotnet build -- %*

AddToPath .\NugetHelper\bin\Debug\
