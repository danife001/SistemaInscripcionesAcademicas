# Verificación de las correcciones

Fecha: 19 de octubre de 2026.

## Registro y configuración

- El registro comprueba el resultado de crear el usuario antes de asignar el rol Estudiante.
- La asignación del rol debe tener éxito antes de crear el estudiante y confirmar la transacción.
- Dos pruebas HTTP verifican que una contraseña inválida no intente asignar roles y que un fallo de rol revierta usuario, estudiante y relaciones.
- La solución incluye los proyectos API y Tests. Se eliminó la referencia al proyecto inexistente.

## Dependencias

- `System.Security.Cryptography.Pkcs`: referencia explícita 9.0.21, en lugar de la transitiva 9.0.11.
- El catálogo de NuGet se descargó por HTTPS y se compararon los intervalos afectados con las versiones resueltas: 50 entradas en la API y 90 en el proyecto de pruebas, sin avisos aplicables.
- `npm audit`: cero vulnerabilidades.
- La compilación de las pruebas no presentó los anteriores avisos NU1902, NU1903 ni MSB9008. Permanece el mensaje informativo NETSDK1057 porque el equipo tiene seleccionado un SDK .NET 11 RC. No se modificó el SDK global del equipo ni el destino net10.0.

Avisos utilizados para revisar la corrección: [CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx) y [CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).

## Pruebas automatizadas

`dotnet test SistemaInscripcionesAcademicas.slnx --no-restore`: **39 casos aprobados**, de los cuales 5 prueban la aplicación completa mediante HTTP con una base SQLite en memoria independiente por aplicación de prueba.

Se comprobó la actualización persistida del nombre y correo, el login con el nuevo correo y el rechazo del antiguo. La paginación HTTP verificó la segunda página de cinco elementos entre doce registros visibles, los totales, la validación de parámetros y el rechazo de accesos sin token o con rol insuficiente.

El frontend compiló con `npm run build` y pasó `npm run lint`.

## Comprobación en navegador

Se ejecutaron el frontend original y la API real mediante un servidor Kestrel de pruebas. Solo en ese servidor se sustituyó SQL Server por SQLite y se prepararon un estudiante autenticable y doce registros adicionales.

Resultados observados:

1. Login con la cuenta desechable de pruebas.
2. Edición del nombre y correo desde Mi perfil; apareció el mensaje de guardado correcto.
3. Cierre de sesión y login con el correo actualizado.
4. Registros: diez elementos inicialmente entre doce estudiantes visibles.
5. Cambio a cinco por página: tres páginas y elementos 1–5.
6. Siguiente: elementos 6–10; siguiente de nuevo: 11–12 y botón Siguiente deshabilitado.
7. Anterior: regreso a elementos 6–10.
8. Cambio a diez por página: regreso a la página 1 de 2.

No se modificaron datos de la base local del usuario. Los datos de SQLite se eliminan al detener el servidor de prueba.

## Límite de esta comprobación

La conexión a LocalDB desde el entorno de revisión falla al resolver la instancia y, mediante su canal local, al generar el contexto SSPI de autenticación de Windows. Por tanto, estos resultados verifican la aplicación y una base relacional SQLite; **no se presenta como completada una prueba contra SQL Server local**.

## Swagger UI

Se añadieron dos pruebas de documentación: interfaz y contrato OpenAPI en Development, y ausencia de interfaz/documento en Production. En navegador se verificó el login interactivo, la aplicación del token Bearer mediante Authorize y GET /api/estudiantes/me con HTTP 200. La prueba utilizó SQLite aislado.

## Repository

Se añadieron repositorios específicos de estudiantes, materias e inscripciones, una unidad de trabajo y su registro scoped. Los servicios y controladores dejaron de depender de EF Core. Se conservaron los contratos HTTP y la transacción del registro. La prueba HTTP nueva verifica catálogo, inscripciones, compañeros, cancelación, privacidad y reinscripción. Los 35 casos pasaron en una copia del código actualizado porque la API estaba abierta y bloqueaba su ejecutable.

## Docker Compose

Se añadieron imágenes multietapa para API y frontend, Nginx, SQL Server 2022, esperas de salud, volúmenes para datos y claves de Identity e inicialización opcional mediante migraciones y catálogo. Compose se validó con config --quiet. Los 39 casos pasan, incluidas cuatro pruebas nuevas relacionadas con el arranque. El motor de Docker rechazó el acceso al canal local, por lo que la construcción y el arranque real quedan pendientes hasta ejecutarse en una sesión con acceso.

## Actualización del 20 de octubre de 2026: concurrencia y SQL Server real

Se aprobaron 49 casos, sin omisiones, configurando SQLSERVER_TEST_CONNECTION. Se añadieron seis casos HTTP con transacciones SQLite y cuatro contra SQL Server real con dos instancias HTTP de la API y conexiones independientes. Se verificaron el límite de tres materias, el profesor repetido, la inscripción duplicada, la coordinación entre cancelación y alta, y las respuestas 409 cuando no se dispone del bloqueo.

Las pruebas SQL Server crearon y eliminaron bases temporales InscripcionesConcurrency_<GUID>. No modificaron la base de la aplicación. El acceso utiliza autenticación SQL al contenedor; el fallo anterior de SSPI se refería a LocalDB con autenticación de Windows.

La versión Docker anterior fue construida y probada por el usuario; también se comprobaron HTTP 200 y Healthy desde el entorno. Para activar esta última protección en Docker, falta reconstruir las imágenes de API y web. El frontend actualizado compila y pasa ESLint.

## Administración — 20 de octubre de 2026

Se implementaron cuenta inicial configurable y transaccional, login administrativo, consulta de sesión y panel React para crear, listar con paginación, actualizar y desactivar estudiantes. No se elevan cuentas existentes ni se cambian sus contraseñas al reiniciar.

Validación actual: **53 pruebas aprobadas, 0 fallidas, 0 omitidas**, incluyendo SQL Server real. La ejecución sin configurar SQL Server aprobó 49 pruebas y el ejecutor reportó 2 entradas omitidas (una teoría representa tres casos SQL). React pasó `npm run build` y `npm run lint`. La comprobación visual del nuevo panel en Docker no se realizó desde este entorno.

La cuenta local se configuró en `.env`, excluido de Git. Se creará al arrancar la imagen actualizada; requiere `docker compose up -d --build api web`. Consulta [administración](administrador.md).

## Gestión de materias — 20 de octubre de 2026

Se añadieron consulta por ID, creación, modificación y desactivación administrativa, con DTO validado, Repository y transacciones coordinadas con las inscripciones. GET de lista conserva la consulta para estudiantes y devuelve el catálogo completo a administradores. No se modificó el esquema ni se añadieron migraciones.

Validación: **61 casos aprobados, 0 fallidos y 0 omitidos**, incluidos seis casos contra SQL Server real. Los ocho casos nuevos cubren permisos, reglas, historial y operaciones concurrentes. Se verificó la coexistencia con las pruebas anteriores de inscripción, administrador, Swagger y paginación. No se reconstruyó el contenedor desde esta tarea; para activar los cambios, ejecutar `docker compose up -d --build api`.

## Manejo centralizado de excepciones — 20 de octubre de 2026

Se añadió IExceptionHandler para traducir excepciones de negocio a 400/409 y fallos inesperados a 500. Conserva `mensaje`, agrega `traceId` y registra el fallo mediante ILogger. Se eliminaron los try/catch repetidos de los controladores y se distinguieron las reglas de negocio de InvalidOperationException técnicas.

Validación: **69 pruebas aprobadas, 0 fallidas y 0 omitidas**, incluidas las seis pruebas SQL Server. Ocho casos nuevos comprueban el contrato, permisos de información (detalles técnicos ocultos), registros y validación de DTO. No se requieren cambios de frontend ni migraciones. Para activar en Docker: `docker compose up -d --build api`.

## Logs persistentes — 20 de octubre de 2026

Se integró Serilog con ILogger para archivos JSON Lines, rotación diaria y por tamaño, retención de 14 archivos y volumen Docker api-logs. Se conservan los proveedores de consola. Se ajustó el traceId del manejador al identificador W3C de la actividad para que coincida con el registrado por Serilog.

Validación final: **73 pruebas aprobadas, 0 fallidas y 0 omitidas**, incluyendo SQL Server real. Las cuatro pruebas nuevas verifican conservación al reabrir el logger, rotación y retención, escritura concurrente y correlación de un error HTTP con su archivo. `docker compose config --quiet` terminó correctamente. El catálogo oficial de vulnerabilidades de NuGet consultado no publicó avisos para Serilog, Serilog.Extensions.Logging y Serilog.Sinks.File. La restauración local se hizo con el feed descargado por las limitaciones previas de acceso de NuGet; no se deshabilitó la auditoría en archivos del proyecto.

La imagen no se reconstruyó durante esta tarea. Para activar el volumen y el proveedor: `docker compose up -d --build api`. No se requieren migraciones de la base de datos.

## Pantalla de materias y limpieza de plantilla — 20 de octubre de 2026

Se añadió la pestaña Materias al panel administrativo con formulario, selector de profesores, filtros, confirmación de baja y mensajes de error. GET /api/profesores devuelve únicamente profesores activos y exige Administrador. Se eliminaron WeatherForecastController.cs y WeatherForecast.cs.

Validación: **74 pruebas aprobadas, 0 fallidas y 0 omitidas**, incluyendo SQL Server. La prueba adicional verifica permisos y contenido del selector; SwaggerTests confirma que /WeatherForecast responde 404 y no aparece en OpenAPI. React pasa build y ESLint. La verificación visual se intentó con una API aislada en SQLite, pero la herramienta de navegador denegó el acceso local por falta de autorización; no se afirma haber completado esa prueba. No se reconstruyeron los contenedores. Activación: `docker compose up -d --build api web`.
