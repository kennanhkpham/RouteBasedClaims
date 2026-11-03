FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY NuGet.Config ./
COPY Services/ClaimsService/ClaimsService.csproj Services/ClaimsService/
RUN dotnet restore Services/ClaimsService/ClaimsService.csproj

COPY Services/ClaimsService/ Services/ClaimsService/
WORKDIR /src/Services/ClaimsService
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet publish ClaimsService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    && ConnectionStrings__ClaimsDb='Server=localhost;Database=ClaimsDb' \
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

ENTRYPOINT ["dotnet", "ClaimsService.dll"]
