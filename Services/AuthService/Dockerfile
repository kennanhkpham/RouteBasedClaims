FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY NuGet.Config ./
COPY Services/AuthService/AuthService.csproj Services/AuthService/
RUN dotnet restore Services/AuthService/AuthService.csproj

COPY Services/AuthService/ Services/AuthService/
WORKDIR /src/Services/AuthService
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet publish AuthService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    && ConnectionStrings__AuthDb='Server=localhost;Database=AuthDb' \
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

ENTRYPOINT ["dotnet", "AuthService.dll"]
