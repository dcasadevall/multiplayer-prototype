# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy project files and restore dependencies.
COPY Shared/Shared.csproj Shared/
COPY Server/Server.csproj Server/
RUN dotnet restore Server/Server.csproj

# 2. Copy the rest of the source code.
COPY . .

# 3. Publish the application.
RUN dotnet publish "Server/Server.csproj" -c Release -o /app/publish --no-restore


# ---- Final Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]