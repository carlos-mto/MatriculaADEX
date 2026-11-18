### Matricula ADEX

Aplicación web ASP.NET Core MVC (.NET 10, EF Core + PostgreSQL) para matricular estudiantes en cursos por ciclo y turno.

![image](https://raw.githubusercontent.com/ZephiroRB/MatriculaADEX/master/capture.png)

## Ejecución

```bash
cp .env.example .env   # opcional, hay valores por defecto
docker compose up --build
```

Abrir http://localhost:8080

## Agregar una migración

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 bash -c '
  dotnet tool install --global dotnet-ef --version "10.*" &&
  export PATH=$PATH:/root/.dotnet/tools &&
  dotnet ef migrations add NombreMigracion --project Matricula.Infrastructure --startup-project Matricula.WebUI'
```
