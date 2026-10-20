# Sistema de Inscripciones Académicas

Aplicación web para registrar estudiantes, consultar materias e inscribirse en un programa de créditos. El frontend está desarrollado en React y la API en ASP.NET Core, con persistencia en SQL Server mediante Entity Framework Core.

## Inicio rápido con Docker

**Requisito:** Docker Desktop iniciado con contenedores Linux y Docker Compose v2. No necesitas instalar .NET, Node.js ni SQL Server. Ejecuta estos comandos en PowerShell, desde la raíz del proyecto.

1. Prepara la configuración sin sobrescribir un `.env` existente:

   ```powershell
   if (!(Test-Path .env)) { Copy-Item .env.example .env }
   notepad .env
   ```

2. En `.env`, reemplaza `MSSQL_SA_PASSWORD` por una contraseña propia de al menos 16 caracteres con mayúsculas, minúsculas, números y símbolos; evita `;`, comillas y `$`. Para evaluar el administrador, configura también `ADMIN_CREAR_AL_INICIAR=true`, `ADMIN_CORREO` y una contraseña propia en `ADMIN_PASSWORD`. No compartas este archivo.

3. Construye y arranca los tres servicios:

   ```powershell
   docker compose up -d --build
   docker compose ps
   ```

   Espera a que los servicios estén `healthy`. La primera ejecución descarga las imágenes, crea la base y carga las 10 materias y los 5 profesores automáticamente.

4. Abre [la aplicación](http://localhost:8080) o [Swagger](http://localhost:8080/swagger). Crea una cuenta para probar el portal del estudiante; para administrar, inicia sesión con `ADMIN_CORREO` y `ADMIN_PASSWORD`. Estas URLs usan los puertos predeterminados y Swagger requiere `APP_ENVIRONMENT=Development`, como en el archivo de ejemplo.

Si un puerto está ocupado, cambia `WEB_PORT`, `API_PORT` o `SQL_PORT` en `.env` y repite el arranque. Para detener conservando los datos y logs: `docker compose down`. Consulta [Docker y diagnóstico](docs/docker.md) o [acceso administrativo](docs/administrador.md) si necesitas más detalles.

## Funcionalidades y reglas de negocio

- Registro de estudiantes con correo único e inicio de sesión.
- Consulta y actualización del perfil propio: nombre y correo electrónico.
- Catálogo de 10 materias de 3 créditos, distribuidas entre 5 profesores con 2 materias cada uno.
- Máximo de 3 inscripciones activas por estudiante, equivalentes a 9 créditos en el catálogo incluido.
- Rechazo de inscripciones duplicadas y de dos materias del mismo profesor.
- Cancelación lógica de inscripciones, conservando el historial y liberando una plaza dentro del límite de materias del estudiante.
- Consulta de registros de otros estudiantes con paginación.
- Consulta de compañeros de una materia: únicamente sus nombres, y solo cuando el solicitante está inscrito en esa materia.
- Endpoints administrativos para consultar, actualizar y desactivar estudiantes.
- Gestión administrativa de materias con tres créditos, máximo de dos activas por profesor y baja lógica que protege las inscripciones existentes; consulta [materias](docs/materias.md).

## Tecnologías y requisitos

| Componente | Tecnología / requisito |
|---|---|
| API | SDK .NET 10 y ASP.NET Core |
| Persistencia | Entity Framework Core 10.0.13 y SQL Server |
| Autenticación | ASP.NET Core Identity, roles y tokens Bearer |
| Frontend | React 19, Vite 8 y JavaScript |
| Node.js | Node.js 24 con npm; compatible con las dependencias declaradas |
| Pruebas | xUnit, InMemory, SQLite y pruebas HTTP con SQL Server real |
| Base local | SQL Server LocalDB en Windows; se puede configurar otra instancia de SQL Server |
| Publicación del paquete de base de datos | SqlPackage, si se utiliza el `.dacpac` local opcional |

Para comprobar las herramientas instaladas:

```powershell
dotnet --version
node --version
npm --version
```

Los comandos de este documento utilizan PowerShell. Ejecútalos desde la raíz del proyecto, salvo que se indique otra carpeta.

## Estructura del proyecto

```text
SistemaInscripcionesAcademicas/
├── SistemaInscripcionesAcademicas.slnx
├── README.md
├── base de datos.dacpac        # Opcional, solo local; excluido de Git
├── SistemaInscripcionesAcademicas.Api/
│   ├── Controllers/            # Endpoints y control de acceso
│   ├── Data/                   # DbContext e inicialización de roles
│   ├── DTOs/                   # Contratos de entrada, salida y paginación
│   ├── Extensions/             # Paginación de consultas
│   ├── Migrations/             # Esquema de la base de datos
│   ├── Models/                 # Entidades e integración con Identity
│   ├── Repositories/           # Consultas, persistencia, interfaces y unidad de trabajo
│   ├── Services/               # Lógica de negocio e interfaces
│   └── Program.cs              # Configuración e inyección de dependencias
├── SistemaInscripcionesAcademicas.Tests/
├── frontend/
│   └── src/
│       ├── pages/              # Pantallas del portal
│       └── services/           # Cliente HTTP de la API
└── docs/
    ├── logs.md
    ├── excepciones.md
    ├── materias.md
    ├── administrador.md
    ├── concurrencia.md
    ├── docker.md
    ├── repository.md
    ├── swagger.md
    ├── verificacion.md
    └── paginacion.md
```

## Configuración de la base de datos

### Opción A: utilizar la base local existente

La API lee `ConnectionStrings:DefaultConnection` de su configuración. La conexión local configurada en el proyecto utiliza autenticación de Windows:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SistemaInscripcionesAcademicas;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Si ya tienes esta base con las tablas y los datos, no necesitas volver a publicarla. Comprueba que LocalDB esté instalado y que la instancia sea accesible para tu usuario de Windows.

Para usar otra instancia, cambia el servidor y la base de datos en la conexión. También puedes sobrescribir la configuración sin editar archivos mediante una variable de entorno, en la misma terminal donde ejecutarás la API:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=SistemaInscripcionesAcademicas;Trusted_Connection=True;TrustServerCertificate=True;'
```

El ejemplo de certificado corresponde al entorno de desarrollo local. Una instalación publicada debe utilizar una conexión y certificados adecuados para su servidor.

### Opción B: recrear la base desde una exportación local (opcional)

El archivo local `base de datos.dacpac` contiene el esquema y datos de la base exportada, incluidos los profesores y las materias. **No se incluye en GitHub**, porque también contiene datos de cuentas de Identity. Esta opción solo aplica si dispones de una copia privada autorizada; para evaluar desde GitHub, utiliza el inicio rápido con Docker, que crea la base y el catálogo sin ese archivo. En la copia revisada se verificaron 10 materias activas de 3 créditos y 5 profesores activos con 2 materias cada uno.

Instala SqlPackage si aún no lo tienes:

```powershell
dotnet tool install --global Microsoft.SqlPackage
```

Publica el paquete en una **base nueva de evaluación**, para conservar tu base de trabajo:

```powershell
SqlPackage /Action:Publish /SourceFile:"base de datos.dacpac" /TargetConnectionString:"Server=(localdb)\MSSQLLocalDB;Database=SistemaInscripcionesAcademicas_Evaluacion;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
```

Configura la API para utilizar esa base, en la terminal donde vayas a arrancarla:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=SistemaInscripcionesAcademicas_Evaluacion;Trusted_Connection=True;TrustServerCertificate=True;'
```

`Publish` consume el `.dacpac`; la operación `Import` corresponde a archivos `.bacpac`. Un `.dacpac` con datos puede publicar tanto el esquema como esos datos. Consulta la [documentación de SqlPackage Publish](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-publish?view=sql-server-ver17).

El paquete exportado fue inspeccionado, pero su publicación no se ejecutó automáticamente durante la revisión del proyecto.

### Opción C: crear únicamente el esquema mediante migraciones

Si necesitas una base vacía, configura primero su conexión y ejecuta:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet restore SistemaInscripcionesAcademicas.Api/SistemaInscripcionesAcademicas.Api.csproj
dotnet ef database update --project SistemaInscripcionesAcademicas.Api --startup-project SistemaInscripcionesAcademicas.Api
```

Si ya tienes `dotnet-ef`, comprueba que su versión principal sea compatible con EF Core 10. Las migraciones crean el esquema; no cargan el catálogo de materias y profesores. Para disponer de esos datos, utiliza el paquete local opcional o cárgalos por separado.

En la ejecución local convencional, la API crea los roles `Estudiante` y `Administrador` al iniciar, pero requiere que la base y las tablas ya existan. En Docker Compose se habilitan explícitamente la aplicación de migraciones y la carga del catálogo vacío.

### Catálogo incluido en la exportación

Todas estas materias tienen 3 créditos:

| Profesor | Materias |
|---|---|
| Carlos Rodríguez | Matemáticas, Física |
| Ana Martínez | Programación, Bases de Datos |
| Pedro Gómez | Inglés, Comunicación |
| Laura Fernández | Estadística, Cálculo |
| Andrés López | Redes, Sistemas Operativos |

## Ejecutar con Docker

El proyecto incluye Dockerfiles y `compose.yaml` para React con Nginx, la API .NET y SQL Server 2022. En esta copia se preparó un `.env` local con contraseña aleatoria y excluido de Git. Para una copia nueva, copia `.env.example` a `.env` y define tu contraseña.

Desde la raíz, con Docker Desktop iniciado en modo Linux:

```powershell
docker compose up -d --build
docker compose ps
```

- [React](http://localhost:8080)
- [Swagger](http://localhost:8080/swagger)
- [API directa](http://localhost:5234)

La base del contenedor se crea con migraciones y recibe un catálogo inicial de 10 materias y 5 profesores. Los datos y las claves de Identity se conservan en volúmenes. No se modifica ni se importa automáticamente la base de LocalDB. Consulta [la guía completa de Docker](docs/docker.md), incluidos el estado de validación y los comandos de diagnóstico.

## Acceso del administrador

La misma pantalla de login admite estudiantes y administradores. El administrador ve un panel con las pestañas **Estudiantes** y **Materias** para listar, crear, editar y desactivar registros. La pestaña Materias incluye selector de profesores, créditos fijos, filtro de estado y confirmación de baja lógica. La cuenta inicial se configura mediante variables de entorno y nunca mediante el registro público.

En esta copia, `.env` incluye `ADMIN_CREAR_AL_INICIAR=true`, el correo `admin@inscripciones.local` y una contraseña aleatoria en `ADMIN_PASSWORD`. Se crea al reconstruir y arrancar la API con `docker compose up -d --build api web`. Para una nueva instalación, configura estos valores siguiendo [la guía de administración](docs/administrador.md). No distribuyas `.env`.

## Instalación y ejecución

### 1. Arrancar la API

Prepara la base con una de las opciones anteriores. Desde la raíz, en una primera terminal:

```powershell
dotnet restore SistemaInscripcionesAcademicas.Api/SistemaInscripcionesAcademicas.Api.csproj
dotnet run --project SistemaInscripcionesAcademicas.Api --launch-profile http
```

La API se inicia en [http://localhost:5233](http://localhost:5233). La ruta raíz no tiene una página de inicio; utiliza un endpoint documentado.

En el perfil de desarrollo HTTP, el documento OpenAPI está disponible en [http://localhost:5233/openapi/v1.json](http://localhost:5233/openapi/v1.json). La interfaz interactiva Swagger UI está en [http://localhost:5233/swagger](http://localhost:5233/swagger). Ambas rutas están disponibles solo en Development.

Existe además un perfil `https` para `https://localhost:7007` y `http://localhost:5233`. Para el arranque descrito y el proxy actual de Vite, utiliza el perfil `http`.

### 2. Arrancar React

En una segunda terminal, desde la raíz:

```powershell
cd frontend
npm ci
npm run dev
```

Abre la dirección que indique Vite, habitualmente [http://localhost:5173](http://localhost:5173). Si el puerto está ocupado, Vite puede seleccionar otro.

Las solicitudes `/api` del frontend pasan por el proxy de Vite hacia `http://localhost:5233`. Si cambias el puerto de la API, actualiza `server.proxy` en `frontend/vite.config.js`.

### 3. Compilar y revisar el frontend

Desde `frontend`:

```powershell
npm run build
npm run lint
```

La compilación genera `frontend/dist`. El proxy descrito es de desarrollo: al publicar los archivos estáticos, el servidor debe dirigir `/api` hacia el backend. La modalidad Docker incluye Nginx con estas rutas configuradas; consulta [Docker Compose](docs/docker.md).

## Flujo de uso

1. Abre el portal y selecciona **Crear una cuenta**.
2. Introduce nombre, correo y contraseña, y confirma la contraseña. Identity aplica su política predeterminada: al menos 6 caracteres, mayúscula, minúscula, número y carácter no alfanumérico. Usa una contraseña propia.
3. Después del registro, inicia sesión con tu correo y contraseña.
4. En **Materias disponibles**, consulta el profesor y los créditos e inscríbete. El backend valida el límite de materias, los duplicados y los profesores repetidos.
5. En **Mis inscripciones**, revisa tus materias y créditos, cancela una inscripción o selecciona **Ver compañeros**.
6. En **Registros de estudiantes**, consulta los nombres y materias de otros estudiantes. Usa **Anterior**, **Siguiente** y el selector de cantidad para paginar.
7. En **Mi perfil**, actualiza nombre o correo y guarda los cambios. Para futuros inicios de sesión utiliza el correo actualizado; la contraseña se conserva.
8. Usa **Cerrar sesión** para retirar el token del almacenamiento de la pestaña y volver al inicio de sesión.

El token se conserva en `sessionStorage`. Cerrar sesión lo elimina del navegador; no hay un endpoint de revocación. Aunque Identity devuelve un `refreshToken`, la aplicación no tiene un endpoint de renovación habilitado y el frontend no lo utiliza. Si el acceso expira, inicia sesión de nuevo.

## Arquitectura y modelo de datos

El recorrido habitual es **pantalla React → servicio HTTP → controlador → servicio de negocio → Repository → DbContext → SQL Server**. Los servicios y controladores dependen de interfaces y no consultan directamente Entity Framework Core.

- `EstudianteService` gestiona registro, consultas, actualización y desactivación.
- `InscripcionService` concentra las validaciones, consultas y cancelaciones de inscripciones.
- `MateriaService` gestiona el acceso al catálogo a través de `IMateriaRepository`.
- `IEstudianteRepository`, `IMateriaRepository` e `IInscripcionRepository` encapsulan consultas y operaciones de persistencia.
- `IUnidadTrabajo` coordina guardados y transacciones; el registro conserva una transacción compartida con Identity.
- Los servicios y repositorios se registran con alcance scoped mediante inyección de dependencias en `Program.cs` y `AddPersistenciaAcademica`.
- Los DTO evitan devolver entidades completas de Identity y definen las validaciones de entrada.
- `ApplicationDbContext` configura relaciones e índices. Un índice único filtrado impide duplicar una inscripción activa del mismo estudiante en la misma materia.
- La consulta paginada utiliza `CountAsync`, `Skip` y `Take` antes de materializar los registros.

| Entidad | Relación / propósito |
|---|---|
| Usuario | Cuenta de Identity; contiene nombre, correo y credenciales administradas por Identity |
| Estudiante | Relación uno a uno con Usuario; contiene su estado activo |
| Profesor | Tiene varias materias |
| Materia | Pertenece a un profesor y tiene créditos |
| Inscripción | Relaciona estudiante y materia; conserva fecha y estado activo |

El nombre utilizado por las consultas y por la edición del perfil es `Usuario.Nombre`. Las bajas de estudiantes e inscripciones son lógicas. El patrón Repository está implementado con interfaces específicas y una unidad de trabajo. Identity conserva su propio acceso a usuarios y roles mediante `UserManager` y sus stores. Consulta [la documentación de Repository](docs/repository.md).

## API: autenticación y permisos

Los ejemplos JSON son ilustrativos. Envía `Content-Type: application/json` para las solicitudes con cuerpo y la siguiente cabecera en los endpoints protegidos:

```http
Authorization: Bearer <accessToken>
```

La identidad del estudiante se obtiene del token; el cliente no envía un ID de estudiante para operar con sus propias inscripciones o perfil. Los tokens Bearer de Identity no deben asumirse como JWT.

El registro público asigna únicamente el rol `Estudiante`. El login acepta estudiantes activos y administradores. La cuenta administrativa inicial se aprovisiona por configuración; los endpoints administrativos no son accesibles con el token de un estudiante. Consulta [la guía de administración](docs/administrador.md).

### Endpoints

| Método | Ruta | Acceso | Operación / respuesta correcta |
|---|---|---|---|
| POST | `/api/estudiantes` | Público | Registrar estudiante; `201` con el estudiante creado |
| POST | `/api/auth/login` | Público | Iniciar sesión como estudiante o administrador; `200` con tokens de Identity |
| GET | `/api/estudiantes/me` | Estudiante activo | Obtener perfil; `200` |
| PUT | `/api/estudiantes/me` | Estudiante activo | Actualizar nombre y correo propios; `200` |
| GET | `/health` | Público | Comprobar conexión a base de datos; `200` saludable o `503` no disponible |
| GET | `/api/auth/me` | Autenticado | Consultar nombre, correo y roles de la sesión; `200` |
| GET | `/api/materias` | Estudiante o Administrador | Disponibles para estudiantes; catálogo completo para administradores; `200` |
| GET | `/api/profesores` | Administrador | Profesores activos para el selector de materias; `200` |
| GET | `/api/materias/{id}` | Administrador | Detalle de una materia; `200` |
| POST | `/api/materias` | Administrador | Crear una materia; `201` |
| PUT | `/api/materias/{id}` | Administrador | Modificar una materia sin alterar historial; `200` |
| DELETE | `/api/materias/{id}` | Administrador | Desactivar sin inscripciones activas; `204` |
| GET | `/api/inscripciones` | Estudiante activo | Listar sus inscripciones activas; `200` |
| POST | `/api/inscripciones` | Estudiante activo | Inscribir una materia; `201` |
| DELETE | `/api/inscripciones/{id}` | Estudiante activo | Cancelar una inscripción propia; `204` |
| GET | `/api/inscripciones/materias/{materiaId}/compas` | Estudiante activo inscrito en la materia | Listar nombres de compañeros; `200` |
| GET | `/api/registros/estudiantes` | Estudiante activo | Consultar registros de otros estudiantes, paginados; `200` |
| GET | `/api/estudiantes` | Administrador | Consultar estudiantes activos, paginados; `200` |
| GET | `/api/estudiantes/{id}` | Administrador | Consultar un estudiante activo; `200` |
| PUT | `/api/estudiantes/{id}` | Administrador | Actualizar nombre y correo; `200` |
| DELETE | `/api/estudiantes/{id}` | Administrador | Desactivar un estudiante; `204` |

`/openapi/v1.json` y `/swagger` están habilitados en Development. El panel administrativo dispone de las pestañas Estudiantes y Materias. `GET /api/profesores` proporciona los profesores activos para el selector de materias y requiere el rol Administrador. Los archivos y el endpoint de plantilla WeatherForecast fueron eliminados.

### Registro

`POST /api/estudiantes`

```json
{
  "nombre": "Estudiante de prueba",
  "correo": "estudiante@example.com",
  "password": "Ejemplo123!"
}
```

Respuesta `201`:

```json
{
  "id": 3,
  "nombre": "Estudiante de prueba",
  "correo": "estudiante@example.com",
  "activo": true
}
```

### Inicio de sesión

`POST /api/auth/login`

```json
{
  "correo": "estudiante@example.com",
  "password": "Ejemplo123!"
}
```

La respuesta contiene `tokenType`, `accessToken`, `expiresIn` y `refreshToken`. Conserva `accessToken` para las siguientes solicitudes. Un correo o contraseña incorrectos, un estudiante inactivo o la ausencia del rol Estudiante producen `401`.

### Actualización del perfil

`PUT /api/estudiantes/me`

```json
{
  "nombre": "Nombre actualizado",
  "correo": "nuevo@example.com"
}
```

Devuelve el estudiante actualizado con el mismo formato del registro. El nombre es obligatorio y admite hasta 200 caracteres; el correo debe ser válido, único y de hasta 256 caracteres. No permite cambiar contraseña, rol o estado activo. Al cambiar el correo se actualizan también el nombre de usuario de Identity y los valores normalizados.

### Materias e inscripciones

`GET /api/materias` devuelve una lista:

```json
[
  { "id": 1, "nombre": "Matemáticas", "creditos": 3, "profesor": "Carlos Rodríguez" }
]
```

`POST /api/inscripciones` recibe:

```json
{ "materiaId": 1 }
```

Devuelve `201` con la inscripción. `GET /api/inscripciones` devuelve una lista de objetos de este mismo tipo:

```json
{
  "id": 6,
  "materiaId": 1,
  "materia": "Matemáticas",
  "profesor": "Carlos Rodríguez",
  "creditos": 3,
  "fechaInscripcion": "2026-10-19T15:00:00Z",
  "activa": true
}
```

`DELETE /api/inscripciones/6` cancela únicamente la inscripción propia indicada. Devuelve `404` si no existe, está inactiva o no pertenece al solicitante.

`GET /api/inscripciones/materias/1/compas` devuelve:

```json
[
  { "nombre": "Otro estudiante" }
]
```

No incluye al solicitante ni estudiantes inactivos; devuelve `[]` si no hay compañeros y `403` si el solicitante no está inscrito.

### Paginación

Tanto `GET /api/registros/estudiantes` como `GET /api/estudiantes` aceptan:

| Parámetro | Predeterminado | Valores válidos |
|---|---:|---|
| `pagina` | 1 | Entero mayor o igual a 1 |
| `tamanoPagina` | 10 | Entero entre 1 y 100 |

Ejemplo: `GET /api/registros/estudiantes?pagina=2&tamanoPagina=5`.

```json
{
  "items": [
    { "nombre": "Otro estudiante", "materias": ["Matemáticas"] }
  ],
  "pagina": 2,
  "tamanoPagina": 5,
  "totalRegistros": 6,
  "totalPaginas": 2,
  "hayAnterior": true,
  "haySiguiente": false
}
```

Los totales se calculan después de aplicar los filtros de acceso y actividad. El orden es por nombre y después por ID. Una página fuera del rango devuelve `items: []` conservando los totales; si no hay resultados, `totalPaginas` es 0. La consulta administrativa utiliza el mismo contenedor, con elementos que contienen `id`, `nombre`, `correo` y `activo`.

Consulta los detalles en [Paginación de estudiantes](docs/paginacion.md).

### Respuestas de error

Consulta [el manejo centralizado de excepciones](docs/excepciones.md) para los códigos HTTP, el formato completo y la consulta de registros. Los fallos inesperados devuelven 500 con un mensaje genérico; los detalles técnicos permanecen en el servidor.

| Código | Casos habituales |
|---|---|
| `400` | Datos inválidos, correo duplicado o incumplimiento de una regla de inscripción |
| `401` | Credenciales inválidas o token ausente, inválido o expirado |
| `403` | Rol insuficiente, estudiante inactivo en operaciones que lo comprueban o acceso a compañeros de una materia ajena |
| `404` | Estudiante o inscripción no encontrados en la operación solicitada |
| `409` | No se pudo obtener a tiempo el bloqueo para una inscripción o cancelación; reintentar |

Las excepciones de negocio se convierten centralmente en ProblemDetails (`application/problem+json`), conservando el campo `mensaje` que utiliza React. Incluyen `title`, `status`, `detail`, `instance` y `traceId`. Ejemplo del mensaje compatible:

```json
{ "mensaje": "No puede inscribir más de tres materias." }
```

Las validaciones automáticas de `[ApiController]` devuelven `ValidationProblemDetails`, con campos como `title`, `status` y `errors`. No todas las respuestas de error incluyen un cuerpo JSON: el cliente debe comprobar también el estado HTTP.

## Probar la API con Swagger UI

1. Inicia la API con `dotnet run --project SistemaInscripcionesAcademicas.Api --launch-profile http`.
2. Abre [Swagger UI](http://localhost:5233/swagger).
3. Si no tienes cuenta, ejecuta `POST /api/Estudiantes` con nombre, correo y contraseña.
4. Abre `POST /api/auth/login`, completa el JSON con tus credenciales y pulsa **Execute**.
5. Copia únicamente `accessToken` de la respuesta HTTP 200.
6. Pulsa **Authorize**, pega el token sin el prefijo `Bearer` y aplica las credenciales. Swagger añadirá `Authorization: Bearer ...` a las operaciones protegidas.
7. Prueba `GET /api/estudiantes/me`, la consulta de materias o la paginación. Los candados identifican las operaciones protegidas y las descripciones indican los roles.

La interfaz utiliza el documento existente `/openapi/v1.json`, generado por ASP.NET Core. El paquete `Swashbuckle.AspNetCore.SwaggerUI` añade la interfaz sin reemplazar el generador. Los tokens no se configuran como JWT ni se guardan de forma persistente en Swagger.

Consulta [la guía de Swagger](docs/swagger.md) para más detalles. Se verificaron desde el navegador el login, Authorize y una consulta protegida del perfil con respuesta HTTP 200, utilizando la API real con SQLite aislado.

## Pruebas y verificación

Desde la raíz:

```powershell
dotnet test SistemaInscripcionesAcademicas.slnx
```

La solución incluye la API y el proyecto de pruebas, con una única referencia válida entre ambos. También puedes ejecutar directamente `dotnet test SistemaInscripcionesAcademicas.Tests/SistemaInscripcionesAcademicas.Tests.csproj`.

En la última ejecución de revisión se aprobaron **74 casos**, incluidos seis contra SQL Server real; cuatro de ellos verifican la coordinación entre dos instancias de la API. La cobertura incluye límite de materias, duplicados, profesores repetidos, cancelación, registros inactivos, actualización, correo duplicado, identidad del perfil, paginación, orden estable y validación de parámetros. También permanece una prueba de ejemplo de suma.

Las pruebas unitarias utilizan EF Core InMemory o SQLite según las operaciones que verifican; las de inscripción utilizan transacciones reales de SQLite. Las cinco pruebas de integración HTTP del flujo académico arrancan la aplicación real con `WebApplicationFactory` y SQLite en memoria: verifican el rechazo de contraseñas inválidas sin asignar roles, la reversión de la transacción si falla el rol, la actualización persistida con login mediante el nuevo correo, la paginación con validación y permisos HTTP y el flujo de inscripción, compañeros, cancelación y reinscripción a través de los repositorios. Otras dos pruebas verifican la interfaz Swagger, los permisos del documento OpenAPI, los esquemas de respuesta y que la documentación no esté publicada en Production. Cuatro pruebas adicionales verifican el catálogo inicial repetible, la conservación de datos existentes, el endpoint de salud y que las migraciones SQL Server estén sincronizadas con el modelo. Cada aplicación de prueba tiene su propia base y claves efímeras de Data Protection. No se modifica la configuración de SQL Server ni la protección de datos de la aplicación normal.

También se verificaron en navegador la edición del perfil, el login con el correo actualizado, el avance y retroceso entre páginas, el límite de la última página y el reinicio a la primera al cambiar la cantidad. La API real se ejecutó con una base SQLite aislada y datos de prueba. La conexión original a LocalDB fallaba por SSPI, pero posteriormente se pudo acceder al SQL Server del contenedor mediante autenticación SQL. Allí se ejecutaron las cuatro pruebas concurrentes con bases temporales independientes de la base de la aplicación. Consulta [el registro de verificación](docs/verificacion.md).

Para validar manualmente el conjunto:

1. Registrar dos estudiantes y comprobar el rechazo de un correo duplicado.
2. Iniciar sesión y actualizar el perfil; volver a entrar con el nuevo correo.
3. Inscribir tres materias de profesores diferentes y verificar 9 créditos.
4. Comprobar que se rechazan una cuarta materia, una materia duplicada y dos materias del mismo profesor.
5. Cancelar una inscripción y comprobar que se permite otra.
6. Inscribir a ambos estudiantes en una misma materia y consultar los nombres de compañeros.
7. Comprobar los registros paginados; para probar varias páginas, utiliza más estudiantes visibles que el tamaño seleccionado.
8. Consultar un endpoint protegido sin token y comprobar que se rechaza el acceso.
9. Verificar en SQL Server que los cambios se conservaron.

## Inscripciones simultáneas

Las altas y cancelaciones del mismo estudiante utilizan una transacción y un bloqueo exclusivo gestionado por SQL Server mediante `sp_getapplock`. Las reglas se consultan después de obtenerlo; la siguiente solicitud valida el estado ya confirmado. Se protegen el máximo de tres materias, los duplicados y la repetición de profesor, también con varias instancias de la API.

Si la espera supera 10 segundos, se devuelve HTTP 409 con un mensaje para reintentar. No se necesitan migraciones. Consulta [la protección de concurrencia y sus pruebas](docs/concurrencia.md).

La última ejecución completa aprobó 74 casos, incluidos cuatro de logs persistentes, ocho de manejo de excepciones, cuatro de administración y ocho de gestión de materias. Por defecto se ejecutan 68 casos sin SQL Server y se omiten las pruebas que requieren `SQLSERVER_TEST_CONNECTION`; la guía explica cómo habilitarlos con un SQL Server de pruebas. Se crean y eliminan bases temporales, sin modificar la base de la aplicación.

Para incorporar estos cambios a los contenedores existentes, ejecuta desde la raíz:

```powershell
docker compose up -d --build api web
```

## Problemas habituales

| Síntoma | Comprobación |
|---|---|
| La API falla al iniciar al consultar roles | Revisar la conexión, acceso del usuario de Windows y existencia de las tablas de Identity |
| No aparecen materias | Una base creada solo con migraciones no contiene el catálogo; publicar el paquete o cargarlo |
| React no puede consultar `/api` | Comprobar que la API esté en el puerto 5233 y que coincida con el proxy de Vite |
| El token deja de funcionar | Cerrar sesión e iniciar sesión de nuevo |
| No se puede usar `/swagger` | Iniciar la API con el perfil `http` o `https` en Development; en Production la documentación está deshabilitada |
| Aparece `NETSDK1057` al compilar | El equipo usa un SDK preliminar; utilizar un SDK estable de .NET 10 para evitar este aviso informativo |
| Restauración de NuGet o `npm ci` falla | Comprobar acceso a los registros de paquetes y las versiones de las herramientas |

## Estado y limitaciones actuales

- El proyecto incluye Repository, Swagger y Docker Compose. El usuario confirmó el funcionamiento en Docker y se comprobaron HTTP 200 y Healthy en React, Swagger y la API. Los cambios posteriores requieren reconstruir las imágenes. Las pruebas automatizadas incluyen SQLite y SQL Server real.
- El administrador dispone de login y panel de gestión de estudiantes. La cuenta inicial es opcional, configurable y se crea al arrancar la API; consulta [administración](docs/administrador.md).
- Existe manejo centralizado de excepciones con ProblemDetails, mensajes compatibles con React y registros mediante ILogger. Los logs se guardan en archivos JSON con rotación y volumen persistente en Docker; consulta [logs persistentes](docs/logs.md). No hay renovación de tokens habilitada.
- El registro comprueba primero `CreateAsync` y después el resultado de asignar el rol. Si cualquiera falla, la transacción no se confirma; las pruebas verifican que no queden usuarios o estudiantes parciales.
- Las inscripciones y cancelaciones se coordinan por estudiante con un bloqueo de SQL Server ligado a su transacción. Las validaciones y el guardado se ejecutan dentro del bloqueo; esta protección también opera entre instancias de la API.
- Se actualizó `System.Security.Cryptography.Pkcs` de la versión transitiva 9.0.11 a una referencia explícita 9.0.21. El catálogo de vulnerabilidades de NuGet consultado no mostró avisos aplicables a las versiones resueltas de la API y las pruebas; `npm audit` reportó cero vulnerabilidades. El SDK instalado de .NET 11 RC sigue emitiendo `NETSDK1057`, un aviso informativo por usar una versión preliminar. El proyecto mantiene el destino `net10.0`.
