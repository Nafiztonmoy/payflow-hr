FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy project files for caching restore layer
COPY PayFlow.sln ./
COPY src/PayFlow.Domain/PayFlow.Domain.csproj src/PayFlow.Domain/
COPY src/PayFlow.Application/PayFlow.Application.csproj src/PayFlow.Application/
COPY src/PayFlow.Infrastructure/PayFlow.Infrastructure.csproj src/PayFlow.Infrastructure/
COPY src/PayFlow.Api/PayFlow.Api.csproj src/PayFlow.Api/

RUN dotnet restore src/PayFlow.Api/PayFlow.Api.csproj

# Copy complete source code and publish
COPY src/ src/
RUN dotnet publish src/PayFlow.Api/PayFlow.Api.csproj -c Release -o /out

# Stage 2: ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PayFlow.Api.dll"]
