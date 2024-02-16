#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pricing-engine/Pricing-engine.csproj", "Pricing-engine/"]
RUN dotnet restore "Pricing-engine/Pricing-engine.csproj"
COPY . .
WORKDIR "/src/Pricing-engine"
RUN dotnet build "Pricing-engine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pricing-engine.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pricing-engine.dll"]