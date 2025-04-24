ARG configuration=Release

###########################
#
# Runtime container build
#
###########################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

EXPOSE 80

###########################
#
# Build container
#
###########################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG version=0.0.0-dev

WORKDIR /src

# Project assemblies for layer caching
COPY TezosNotifyBot.sln .
COPY TezosNotifyBot/TezosNotifyBot.csproj ./TezosNotifyBot/
COPY TezosNotifyBot.Dialog/TezosNotifyBot.Dialog.csproj ./TezosNotifyBot.Dialog/
COPY TezosNotifyBot.Shared/TezosNotifyBot.Shared.csproj ./TezosNotifyBot.Shared/
COPY TezosNotifyBot.Domain/TezosNotifyBot.Domain.csproj ./TezosNotifyBot.Domain/
COPY TezosNotifyBot.Storage/TezosNotifyBot.Storage.csproj ./TezosNotifyBot.Storage/
 
# Restore dependencies (with nuget cache)
RUN dotnet restore --packages /nuget

# Copy all sources
COPY . .

WORKDIR /src/TezosNotifyBot

## Publish backend assembly
RUN dotnet publish --output /out --no-restore -v m /property:Version=${version}

###########################
#
# Runtime layer
#
###########################
FROM runtime

COPY --from=build /out /app

ENTRYPOINT ["dotnet", "TezosNotifyBot.dll", "--migrate"]
