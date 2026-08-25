FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files for caching restore layer
COPY TestApp.sln ./
COPY src/TestApp.Api/TestApp.Api.csproj src/TestApp.Api/
COPY src/TestApp.Client/TestApp.Client.csproj src/TestApp.Client/
COPY tests/TestApp.Api.Tests/TestApp.Api.Tests.csproj tests/TestApp.Api.Tests/

RUN dotnet restore src/TestApp.Api/TestApp.Api.csproj

# Copy full source and publish API
COPY . ./
RUN dotnet publish src/TestApp.Api/TestApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TestApp.Api.dll"]
