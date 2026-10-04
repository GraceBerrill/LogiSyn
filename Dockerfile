# Stage 1: Build using official Microsoft .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for fast caching
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
COPY ["AndersonsBakeryAPI/AndersonsBakeryAPI.csproj", "AndersonsBakeryAPI/"]

# Restore packages
RUN dotnet restore "AndersonsBakeryAPI/AndersonsBakeryAPI.csproj"

# Copy source code and build Release
COPY ["SharedLibrary/", "SharedLibrary/"]
COPY ["AndersonsBakeryAPI/", "AndersonsBakeryAPI/"]

WORKDIR "/src/AndersonsBakeryAPI"
RUN dotnet publish "AndersonsBakeryAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image using lightweight ASP.NET Core 10
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render exposes and routes traffic via port 8080
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AndersonsBakeryAPI.dll"]