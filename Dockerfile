FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["WordCounter.csproj", ""]
RUN dotnet restore "WordCounter.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "WordCounter.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "WordCounter.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "WordCounter.dll"]