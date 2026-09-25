FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["backend/JamineERP.Backend.csproj", "backend/"]
RUN dotnet restore "backend/JamineERP.Backend.csproj"

# Copy everything else and build
COPY backend/ backend/
WORKDIR /src/backend
RUN dotnet publish "JamineERP.Backend.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 (Render default for web services)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "JamineERP.Backend.dll"]
