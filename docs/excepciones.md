# Manejo centralizado de excepciones

`ManejadorExcepciones` implementa `IExceptionHandler` y se registra con `AddExceptionHandler` y `AddProblemDetails`. `UseExceptionHandler` se ejecuta antes de los demás componentes HTTP, tanto en Development como en Production.

Los servicios lanzan excepciones de negocio explícitas. Los controladores conservan las decisiones normales de autorización y recurso no encontrado, pero ya no repiten bloques try/catch para errores de negocio.

| Excepción | Estado HTTP | Comportamiento |
|---|---|---|
| `ReglaNegocioException` | 400 | Mensaje público de validación de negocio |
| `ReglaNegocioException` con `Conflicto=true` | 409 | Una regla impide modificar el estado actual |
| `ConflictoInscripcionException` | 409 | Bloqueo temporal de inscripciones o catálogo |
| Cualquier otra excepción no controlada | 500 | Mensaje genérico; detalle técnico únicamente en los registros del servidor |

Una `InvalidOperationException` técnica no se considera automáticamente un error del usuario. Las reglas de negocio utilizan su tipo específico para evitar ocultar fallos de programación o publicar mensajes internos.

## Contrato de respuesta

Las excepciones manejadas producen JSON con Content-Type `application/problem+json`:

```json
{
  "title": "Solicitud inválida",
  "status": 400,
  "detail": "El correo ya está registrado.",
  "instance": "/api/Estudiantes",
  "mensaje": "El correo ya está registrado.",
  "traceId": "identificador-de-la-solicitud"
}
```

El campo `mensaje` mantiene la compatibilidad con React. `traceId` permite relacionar la respuesta con los registros del servidor. Los fallos inesperados devuelven «Ocurrió un error inesperado. Intenta nuevamente.», sin el mensaje técnico ni la traza de pila, incluso en desarrollo.

Los errores de validación de DTO siguen utilizando `ValidationProblemDetails` con `errors`. Las respuestas normales 401, 403 y 404 no requieren lanzar excepciones y mantienen su contrato; algunas no tienen cuerpo. El cliente debe comprobar el estado HTTP.

## Registros y alcance

El manejador utiliza `ILogger`: registra fallos inesperados como Error con la excepción y traceId, y rechazos de negocio como Warning con estado y traceId. Usa los proveedores configurados, incluidos los [archivos persistentes con Serilog](logs.md). No registra cuerpos de solicitudes o tokens. Los registros de infraestructura deben tener acceso restringido porque las excepciones técnicas pueden contener información interna.

Los errores durante el arranque (por ejemplo, conexión, migraciones o cuenta inicial inválida) impiden iniciar la aplicación; no son solicitudes HTTP y no los transforma este manejador. Las cancelaciones del cliente y las respuestas ya iniciadas siguen las reglas del middleware de ASP.NET Core. Las transacciones continúan revirtiéndose al salir del servicio sin confirmar.

## Verificación

Las pruebas `ExcepcionesTests` comprueban 400, 409 y 500, contrato ProblemDetails, compatibilidad de `mensaje`, traceId y registros, ocultación de detalles en Development y Production y validación de DTO. Las pruebas existentes siguen verificando transacciones, permisos, registro, materias e inscripciones.

No se requieren migraciones ni cambios en React. Para activar el cambio en Docker:

```powershell
docker compose up -d --build api
```
