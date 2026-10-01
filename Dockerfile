# Stage 1: Build & Restore Dependencies
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy các file d? án .csproj ?? restore tr??c (t?i ?u cache Docker layer)
COPY ["AudioGuide.Core/AudioGuide.Core.csproj", "AudioGuide.Core/"]
COPY ["AudioGuide.Infrastructure/AudioGuide.Infrastructure.csproj", "AudioGuide.Infrastructure/"]
COPY ["AudioGuide.API/AudioGuide.API.csproj", "AudioGuide.API/"]
COPY ["AudioGuide.Tests/AudioGuide.Tests.csproj", "AudioGuide.Tests/"]

RUN dotnet restore "AudioGuide.API/AudioGuide.API.csproj"

# Copy toàn b? mã ngu?n còn l?i và ti?n hành publish
COPY . .
WORKDIR "/src/AudioGuide.API"
RUN dotnet publish "AudioGuide.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image nh? ph?c v? ch?y ?ng d?ng
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# C?u hình bi?n môi tr??ng m?c ??nh trên Render
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AudioGuide.API.dll"]