FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /MB

# Initialize the project files
COPY . ./

# Attempt to build MediaBoom
RUN dotnet build "MediaBoom.sln" -p:Configuration=Release

# Run the ASP.NET image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /MB

# Copy the output files and start MediaBoom
COPY --from=build-env /MB/private/MediaBoom.Cli/bin/Release/net8.0 .
ENTRYPOINT ["dotnet", "MediaBoom.Cli.dll"]
