FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY [".", "./core-example"]
WORKDIR /src/core-example
RUN dotnet publish "core-example.csproj" -v n --configuration Release -o /app 
COPY wait-for-it.sh /app
COPY Ontologies /app/ontologies


FROM base AS final
COPY --from=build /app .
WORKDIR /app
ENTRYPOINT ["dotnet", "core-example.dll"]