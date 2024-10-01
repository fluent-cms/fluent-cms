FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-amd64 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM base AS final
WORKDIR /app
COPY bin/Release/net8.0/publish .
ENTRYPOINT ["dotnet", "FluentCMS.dll"]

#docker buildx build --platform linux/amd64 -f Amd64.Dockerfile -t jaike/fluent-cms-amd64:0.1 --push .
