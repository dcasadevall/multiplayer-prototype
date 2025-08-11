# Use the native build platform ($BUILDPLATFORM) to avoid slow emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy all .csproj files first.
# This creates a layer that is only invalidated when your project dependencies change.
COPY Shared/Shared.csproj Shared/
COPY Server/Server.csproj Server/
# (If you had a solution file .sln, you would copy that here too)

# 2. Restore dependencies for the entire solution.
# Now that all project files are present, this will be a complete restore.
RUN dotnet restore Server/Server.csproj

# 3. Copy the rest of the application's source code.
COPY . .

# 4. Publish the application. The --no-restore flag works now because the
# restore was complete.
RUN dotnet publish "Server/Server.csproj" -c Release -o /app/publish --no-restore


# ---- Final Stage ----
# (Your final stage should remain the same)
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]