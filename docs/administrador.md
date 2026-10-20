# Administración de estudiantes

El rol `Administrador` permite consultar estudiantes activos con paginación, crear estudiantes, editar su nombre y correo y desactivarlos. La baja es lógica y conserva el historial. El formulario de inicio de sesión es el mismo para ambos roles; React consulta `GET /api/auth/me` y muestra la administración o el portal académico. La autorización se aplica también en la API mediante `[Authorize(Roles = "Administrador")]`.

## Cuenta inicial con Docker

En esta copia se preparó `admin@inscripciones.local` y una contraseña aleatoria guardada únicamente en `ADMIN_PASSWORD` del `.env` local. La cuenta se crea al iniciar la API actualizada. No publiques `.env` ni lo adjuntes a la entrega.

En una copia nueva, agrega estas variables a tu `.env` y escribe tu propia contraseña:

```dotenv
ADMIN_CREAR_AL_INICIAR=true
ADMIN_CORREO=admin@inscripciones.local
ADMIN_NOMBRE=Administrador
ADMIN_PASSWORD=REEMPLAZAR_POR_UNA_CLAVE_PROPIA
```

La contraseña debe cumplir la política de Identity: al menos 6 caracteres, mayúscula, minúscula, número y símbolo. Se recomienda una contraseña larga y única. Evita `$` en este archivo para no provocar interpolación de Compose.

Desde la raíz del proyecto:

```powershell
docker compose up -d --build api web
```

Abre http://localhost:8080 e inicia sesión con el correo configurado y la contraseña de `.env`. Aparecerá **Administración de estudiantes**. Tras confirmar el acceso, puedes poner `ADMIN_CREAR_AL_INICIAR=false`, vaciar `ADMIN_PASSWORD` y ejecutar `docker compose up -d api`; la cuenta ya persistida continúa funcionando.

## Ejecución local sin Docker

Configura la conexión y las tablas como indica el README. En la terminal donde arrancas la API, establece `Administrador__CrearAlIniciar=true`, `Administrador__Correo`, `Administrador__Nombre` y `Administrador__Password` mediante variables de entorno. Otra opción es usar los secretos de usuario de ASP.NET Core en desarrollo. `.env` solo lo consume Docker Compose; `dotnet run` no lo carga automáticamente.

## Comportamiento de la inicialización

- Está deshabilitada por defecto y se habilita explícitamente mediante configuración.
- Usuario y asignación del rol se guardan en una misma transacción; se validan los resultados de Identity.
- Los reinicios no duplican la cuenta ni cambian su contraseña.
- Si el correo ya pertenece a una cuenta sin rol Administrador, el arranque falla con un mensaje explicativo. Utiliza otro correo para la cuenta administrativa; nunca se asciende automáticamente a un estudiante.
- No se crea una ficha de estudiante para el administrador.
- Cambiar `ADMIN_PASSWORD` no cambia una contraseña ya existente.
- El registro público siempre asigna únicamente el rol Estudiante, incluso si el cliente envía un campo de rol adicional.

## Uso y comprobación

1. Entra como administrador y crea un estudiante desde el panel.
2. Consulta la lista paginada, edita su nombre o correo y verifica los cambios.
3. Desactívalo tras confirmar la acción; desaparece de la lista de activos y no puede volver a iniciar sesión.
4. Cierra sesión y entra como un estudiante activo: debe mostrarse el portal académico.
5. Desde Swagger, un estudiante recibe HTTP 403 al intentar listar, editar o desactivar estudiantes mediante los endpoints administrativos.

Las pruebas `AdministradorTests` verifican creación desde la configuración de arranque, login, identidad, listado, actualización, desactivación, rechazo de contraseña incorrecta, permisos, protección frente a elevación de roles, inicialización repetible y reversión ante una contraseña inicial inválida. La interfaz React se verificó mediante compilación y ESLint; la comprobación visual del panel queda para la aplicación reconstruida.

La baja impide nuevos inicios de sesión. Los tokens Bearer emitidos previamente no se revocan de inmediato; conservan su vencimiento. El panel no incluye recuperación de contraseñas, reactivación de estudiantes ni administración de profesores y materias. La administración de materias sí está disponible mediante [endpoints y Swagger](materias.md).
