FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FluentCMS/FluentCMS.csproj", "FluentCMS/"]
RUN dotnet restore "FluentCMS/FluentCMS.csproj"

COPY ["Utils/Utils.csproj", "Utils/"]
RUN dotnet restore "Utils/Utils.csproj"

COPY . .
WORKDIR "/src/FluentCMS"
RUN dotnet build "FluentCMS.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FluentCMS.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FluentCMS.dll"]
