FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Matricula.sln ./
COPY Matricula.Core/Matricula.Core.csproj Matricula.Core/
COPY Matricula.Infrastructure/Matricula.Infrastructure.csproj Matricula.Infrastructure/
COPY Matricula.WebUI/Matricula.WebUI.csproj Matricula.WebUI/
RUN dotnet restore Matricula.WebUI/Matricula.WebUI.csproj
COPY . .
RUN apt-get update -qq && apt-get install -y -qq --no-install-recommends curl ca-certificates && rm -rf /var/lib/apt/lists/* \
 && curl -sSL -o /usr/local/bin/tailwindcss https://github.com/tailwindlabs/tailwindcss/releases/download/v4.3.5/tailwindcss-linux-x64 \
 && chmod +x /usr/local/bin/tailwindcss \
 && tailwindcss -i Matricula.WebUI/Styles/app.css -o Matricula.WebUI/wwwroot/css/app.css --minify
RUN dotnet publish Matricula.WebUI/Matricula.WebUI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
RUN mkdir -p /app/keys && chown app:app /app/keys
USER app
ENTRYPOINT ["dotnet", "Matricula.WebUI.dll"]
