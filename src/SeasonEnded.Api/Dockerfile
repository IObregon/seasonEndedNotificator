FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY ["src/SeasonEnded.Api/SeasonEnded.Api.csproj", "src/SeasonEnded.Api/"]
RUN dotnet restore "src/SeasonEnded.Api/SeasonEnded.Api.csproj"

COPY src/SeasonEnded.Api/ src/SeasonEnded.Api/
RUN dotnet publish "src/SeasonEnded.Api/SeasonEnded.Api.csproj" -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SeasonEnded.Api.dll"]
