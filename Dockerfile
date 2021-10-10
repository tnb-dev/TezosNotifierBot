ARG configuration=Release

###########################
#
# Build container
#
###########################
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

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
# Runtime container build
#
###########################
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS runtime

COPY --from=build /out /app

WORKDIR /app

EXPOSE 80

ENTRYPOINT ["dotnet", "TezosNotifyBot.dll", "--migrate"]
