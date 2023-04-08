#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Server/Onboarding.Server.csproj", "Server/"]
COPY ["Client/Onboarding.Client.csproj", "Client/"]
COPY ["Shared/Onboarding.Shared.csproj", "Shared/"]
RUN dotnet restore "Server/Onboarding.Server.csproj"
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "Onboarding.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Onboarding.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Onboarding.Server.dll"]