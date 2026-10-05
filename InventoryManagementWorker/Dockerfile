FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["InventoryManagementWorker/InventoryManagementWorker.csproj", "InventoryManagementWorker/"]

RUN dotnet restore "InventoryManagementWorker/InventoryManagementWorker.csproj"

COPY . .

WORKDIR "/src/InventoryManagementWorker"

RUN dotnet build "InventoryManagementWorker.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "InventoryManagementWorker.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "InventoryManagementWorker.dll"]