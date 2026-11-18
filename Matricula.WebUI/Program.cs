using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Matricula.Infrastructure.Services;
if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}
var builder = WebApplication.CreateBuilder(args);

builder.RegisterDbContext();
builder.RegisterAuthentication();
builder.RegisterAppServices();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var trustProxy = Environment.GetEnvironmentVariable("TRUST_PROXY") == "true";
if (trustProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

var app = builder.Build();
if (trustProxy)
{
    app.UseForwardedHeaders();
}
app.ExecuteMigrations();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");




using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MatriculaContext>();
    var seedPeriod = await scope.ServiceProvider.GetRequiredService<PeriodService>().GetActivePeriodAsync();

    if (!context.AdminUsers.Any())
    {
        var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        if (string.IsNullOrEmpty(adminUsername)) adminUsername = "admin";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrEmpty(adminPassword))
        {
            app.Logger.LogWarning("No se creó el usuario administrador: defina ADMIN_PASSWORD");
        }
        else
        {
            var salt = SecurityHelper.GenerateSalt();
            context.AdminUsers.Add(new AdminUser
            {
                Username = adminUsername,
                Salt = salt,
                PasswordHash = SecurityHelper.HashPassword(adminPassword, salt),
                SecurityStamp = Guid.NewGuid().ToString("N")
            });
            context.SaveChanges();
        }
    }

    var count = context.Students.Count();
    if (count == 0)
    {

        //ALUMNOS C1
        Student std11 = new Student { FullName = "Juan Perez", Cycle = 1 };
        Student std12 = new Student { FullName = "Maria Lopez", Cycle = 1 };
        Student std13 = new Student { FullName = "Pedro Martinez", Cycle = 1 };

        //ALUMNOS C2
        Student std21 = new Student { FullName = "Carlos Torres", Cycle = 2 };
        Student std22 = new Student { FullName = "Raquel Mendoza", Cycle = 2 };

        //ALUMNOS C3
        Student std31 = new Student { FullName = "Mario Chavez", Cycle = 3 };
        Student std32 = new Student { FullName = "Carolina Huaman", Cycle = 3 };


        context.Students.Add(std11);
        context.Students.Add(std12);
        context.Students.Add(std13);

        context.Students.Add(std21);
        context.Students.Add(std22);

        context.Students.Add(std31);
        context.Students.Add(std32);

       
        //TURNOS
        Shift sfm = new Shift { Name = "Mañana" };
        Shift sft = new Shift { Name = "Tarde" };
        Shift sfn = new Shift { Name = "Noche" };

        context.Shifts.Add(sfm);
        context.Shifts.Add(sft);
        context.Shifts.Add(sfn);

        context.SaveChanges();
        
        //CURSOS CICLO 1
        Course courseCycleA1 = new Course { Name = "Introducción a la Algoritmia" };
        Course courseCycleB1 = new Course { Name = "Matematica I" };
        Course courseCycleC1 = new Course { Name = "Arquitectura Web" };
        Course courseCycleD1 = new Course { Name = "Fundamentos de Gestion Empresarial" };
        //CURSOS CICLO 2
        Course courseCycleA2 = new Course { Name = "Algoritmos y Estructura de Datos" };
        Course courseCycleB2 = new Course { Name = "Desarrollo Web" };
        Course courseCycleC2 = new Course { Name = "Base de Datos" };
        Course courseCycleD2 = new Course { Name = "Matematica II" };
        //CURSOS CICLO 3
        Course courseCycleA3 = new Course { Name = "Lenguaje de Programación I" };
        Course courseCycleB3 = new Course { Name = "Programación Orientada a Objetos I" };
        Course courseCycleC3 = new Course { Name = "Base de Datos Avanzado I" };
        Course courseCycleD3 = new Course { Name = "Analisis y Diseño de Sistemas II" };

        context.Courses.Add(courseCycleA1);
        context.Courses.Add(courseCycleB1);
        context.Courses.Add(courseCycleC1);
        context.Courses.Add(courseCycleD1);

        context.Courses.Add(courseCycleA2);
        context.Courses.Add(courseCycleB2);
        context.Courses.Add(courseCycleC2);
        context.Courses.Add(courseCycleD2);

        context.Courses.Add(courseCycleA3);
        context.Courses.Add(courseCycleB3);
        context.Courses.Add(courseCycleC3);
        context.Courses.Add(courseCycleD3);

        context.SaveChanges();

        ClassRoom classRoom = new ClassRoom { Name = "A1" };
        ClassRoom classRoom2 = new ClassRoom { Name = "A2" };
        ClassRoom classRoom3 = new ClassRoom { Name = "A3" };
        
        context.ClassRooms.Add(classRoom);
        context.ClassRooms.Add(classRoom2);
        context.ClassRooms.Add(classRoom3);

        context.SaveChanges();

        // PROFESORES
        Teacher tAna = new Teacher { FullName = "Ana Gutiérrez", Email = "ana.gutierrez@adex.edu.pe", Specialty = "Matemáticas" };
        Teacher tLuis = new Teacher { FullName = "Luis Ramírez", Email = "luis.ramirez@adex.edu.pe", Specialty = "Programación" };
        Teacher tRosa = new Teacher { FullName = "Rosa Medina", Email = "rosa.medina@adex.edu.pe", Specialty = "Gestión" };

        context.Teachers.AddRange(tAna, tLuis, tRosa);
        context.SaveChanges();

        // AULA CURSO 1
        ClassRoomCourse crc1 = new ClassRoomCourse { Teacher = tLuis, ClassRoom = classRoom2, Course = courseCycleA1, Shift = sfm, Cycle = 1, StartTime = TimeSpan.Parse("8:30:0"), EndTime = TimeSpan.Parse("9:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse crc2 = new ClassRoomCourse { Teacher = tAna, ClassRoom = classRoom2, Course = courseCycleB1, Shift = sfm, Cycle = 1, StartTime = TimeSpan.Parse("9:30:0"), EndTime = TimeSpan.Parse("10:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse crc3 = new ClassRoomCourse { ClassRoom = classRoom2, Course = courseCycleC1, Shift = sfm, Cycle = 1, StartTime = TimeSpan.Parse("10:30:0"), EndTime = TimeSpan.Parse("11:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse crc4 = new ClassRoomCourse { Teacher = tRosa, ClassRoom = classRoom2, Course = courseCycleD1, Shift = sfm, Cycle = 1, StartTime = TimeSpan.Parse("11:30:0"), EndTime = TimeSpan.Parse("12:30:0"), Capacity = 2, Period = seedPeriod };

        context.ClassRoomCourses.Add(crc1);
        context.ClassRoomCourses.Add(crc2);
        context.ClassRoomCourses.Add(crc3);
        context.ClassRoomCourses.Add(crc4);
        context.SaveChanges();

        // AULA CURSO 2
        ClassRoomCourse cra1 = new ClassRoomCourse { ClassRoom = classRoom, Course = courseCycleA2, Shift = sft, Cycle = 2, StartTime = TimeSpan.Parse("8:30:0"), EndTime = TimeSpan.Parse("9:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse cra2 = new ClassRoomCourse { Teacher = tLuis, ClassRoom = classRoom, Course = courseCycleB2, Shift = sft, Cycle = 2, StartTime = TimeSpan.Parse("10:30:0"), EndTime = TimeSpan.Parse("12:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse cra3 = new ClassRoomCourse { Teacher = tRosa, ClassRoom = classRoom, Course = courseCycleC2, Shift = sft, Cycle = 2, StartTime = TimeSpan.Parse("14:30:0"), EndTime = TimeSpan.Parse("15:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse cra4 = new ClassRoomCourse { Teacher = tAna, ClassRoom = classRoom, Course = courseCycleD2, Shift = sft, Cycle = 2, StartTime = TimeSpan.Parse("18:30:0"), EndTime = TimeSpan.Parse("19:30:0"), Capacity = 2, Period = seedPeriod };

        context.ClassRoomCourses.Add(cra1);
        context.ClassRoomCourses.Add(cra2);
        context.ClassRoomCourses.Add(cra3);
        context.ClassRoomCourses.Add(cra4);
        context.SaveChanges();

        // AULA CURSO 3
        ClassRoomCourse crx1 = new ClassRoomCourse { ClassRoom = classRoom3, Course = courseCycleA3, Shift = sfn, Cycle = 3, StartTime = TimeSpan.Parse("8:30:0"), EndTime = TimeSpan.Parse("9:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse crx2 = new ClassRoomCourse { Teacher = tLuis, ClassRoom = classRoom3, Course = courseCycleB3, Shift = sfn, Cycle = 3, StartTime = TimeSpan.Parse("18:30:0"), EndTime = TimeSpan.Parse("19:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse crx3 = new ClassRoomCourse { Teacher = tRosa, ClassRoom = classRoom3, Course = courseCycleC3, Shift = sft, Cycle = 3, StartTime = TimeSpan.Parse("10:30:0"), EndTime = TimeSpan.Parse("11:30:0"), Capacity = 2, Period = seedPeriod };

        ClassRoomCourse crx4 = new ClassRoomCourse { ClassRoom = classRoom3, Course = courseCycleD3, Shift = sfn, Cycle = 3, StartTime = TimeSpan.Parse("12:30:0"), EndTime = TimeSpan.Parse("13:30:0"), Capacity = 2, Period = seedPeriod };

        context.ClassRoomCourses.Add(crx1);
        context.ClassRoomCourses.Add(crx2);
        context.ClassRoomCourses.Add(crx3);
        context.ClassRoomCourses.Add(crx4);
        context.SaveChanges();
        

    }
}


app.Run();
