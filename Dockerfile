FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Matricula.sln ./
COPY Matricula.Core/Matricula.Core.csproj Matricula.Core/
COPY Matricula.Infrastructure/Matricula.Infrastructure.csproj Matricula.Infrastructure/
COPY Matricula.WebUI/Matricula.WebUI.csproj Matricula.WebUI/
RUN dotnet restore Matricula.WebUI/Matricula.WebUI.csproj
COPY . .
RUN dotnet publish Matricula.WebUI/Matricula.WebUI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Matricula.WebUI.dll"]
