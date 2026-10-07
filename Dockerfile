# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/DemoApp/DemoApp.csproj src/DemoApp/
RUN dotnet restore src/DemoApp/DemoApp.csproj
COPY src/ src/
RUN dotnet publish src/DemoApp/DemoApp.csproj -c Release -o /app --no-restore

# --- Run stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DemoApp.dll"]
