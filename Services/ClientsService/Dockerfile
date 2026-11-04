FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY NuGet.Config ./
COPY Services/ClientsService/ClientsService.csproj Services/ClientsService/
RUN dotnet restore Services/ClientsService/ClientsService.csproj

COPY Services/ClientsService/ Services/ClientsService/
WORKDIR /src/Services/ClientsService
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet publish ClientsService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    && ConnectionStrings__ClientsDb='Server=localhost;Database=ClientsDb' \
       dotnet ef migrations bundle \
    --configuration Release \
    --output /app/efbundle \
    --no-build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish ./
COPY --from=build /app/efbundle ./efbundle
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClientsService.dll"]
