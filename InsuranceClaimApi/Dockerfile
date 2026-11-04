FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY NuGet.Config ./
COPY InsuranceClaimApi/InsuranceClaimApi.csproj InsuranceClaimApi/
RUN dotnet restore InsuranceClaimApi/InsuranceClaimApi.csproj

COPY InsuranceClaimApi/ InsuranceClaimApi/
WORKDIR /src/InsuranceClaimApi

RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet publish InsuranceClaimApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    && ConnectionStrings__DefaultConnection='Server=localhost;Initial Catalog=RouteBasedClaimsDev;User ID=sa;Password=BuildOnly_LocalOnly_123!;Encrypt=True;TrustServerCertificate=True' \
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

ENTRYPOINT ["dotnet", "InsuranceClaimApi.dll"]
