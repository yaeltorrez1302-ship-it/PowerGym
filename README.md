# POWER GYM
## Sistema Web de Gestión de Socios y Membresías

Proyecto académico desarrollado para la Actividad 5: Prototipado Ágil.

### Tecnologías utilizadas
- C#
- ASP.NET Core MVC (.NET 9)
- PostgreSQL 18
- Npgsql
- HTML, CSS y Bootstrap
- Git y GitHub

### Funcionalidades
- Registro de socios.
- Búsqueda por nombre o documento.
- Edición de información.
- Desactivación lógica de socios.
- Administración de planes.
- Registro y consulta de membresías.
- Control de fechas de vigencia.

### Base de datos
Motor: PostgreSQL.

Base de datos: power_gym.

El archivo database/power_gym_schema.sql contiene la estructura de la base de datos, sin datos personales.

### Ejecución
1. Instalar .NET 9 y PostgreSQL.
2. Crear la base de datos power_gym.
3. Configurar la cadena de conexión mediante User Secrets.
4. Restaurar dependencias:

   dotnet restore

5. Ejecutar:

   dotnet run --urls http://localhost:5080

### Acceso
- Socios: /Socios
- Membresías: /Membresias

### Repositorio
https://github.com/yaeltorrez1302-ship-it/PowerGym

### Autor
Rodrigo Alonso Sanchez De Los Rios

Universidad Privada Domingo Savio
Ingeniería de Sistemas
Tarija - Bolivia
Noviembre 2026
