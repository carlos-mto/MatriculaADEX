### Matricula ADEX

Aplicación web ASP.NET Core MVC (.NET 10, EF Core + PostgreSQL) para matricular estudiantes en cursos por ciclo y turno.

![image](https://raw.githubusercontent.com/ZephiroRB/MatriculaADEX/master/capture.png)

## Ejecución

```bash
cp .env.example .env   # editar ADMIN_PASSWORD (obligatorio) y, si hace falta, ENROLLMENT_PERIOD
docker compose up --build
```

Abrir http://localhost:8080 e iniciar sesión con `ADMIN_USERNAME` / `ADMIN_PASSWORD` del `.env`.
El usuario administrador se crea al primer arranque si aún no existe ninguno.

## Agregar una migración

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 bash -c '
  dotnet tool install --global dotnet-ef --version "10.*" &&
  export PATH=$PATH:/root/.dotnet/tools &&
  dotnet ef migrations add NombreMigracion --project Matricula.Infrastructure --startup-project Matricula.WebUI'
```

## Tests

Requieren Docker (Testcontainers levanta un PostgreSQL temporal). Sin .NET local:

```bash
docker run --rm --network host -v /var/run/docker.sock:/var/run/docker.sock -v $PWD:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test Matricula.Tests/Matricula.Tests.csproj
```

## Despliegue

Por defecto la aplicación escucha solo en `127.0.0.1`. Para exponerla, colocar un proxy inverso HTTPS delante, definir `WEB_BIND=0.0.0.0` solo si es necesario y `TRUST_PROXY=true`.

La migración `EnrollmentPeriod` asigna las matrículas existentes al periodo `2026-II`.
