# Protección de inscripciones simultáneas

Verificación: 20 de octubre de 2026.

## Comportamiento

Las altas y cancelaciones del mismo estudiante se coordinan antes de consultar las reglas de negocio. Una operación espera a que termine la anterior y después vuelve a consultar la base. Por tanto, dos solicitudes concurrentes no pueden reservar el mismo último cupo, inscribir una materia duplicada ni inscribir dos materias del mismo profesor.

La identidad del estudiante sigue obteniéndose del token. Las rutas y cuerpos JSON se conservan.

## Implementación

- `InscripcionService` inicia `IUnidadTrabajo.IniciarTransaccionInscripcionesAsync(estudianteId)` antes de las validaciones, y confirma después de guardar.
- `UnidadTrabajo` abre una transacción SQL Server ReadCommitted y solicita `sys.sp_getapplock` con recurso `Inscripciones:Estudiante:{id}`, modo Exclusive y propietario Transaction.
- El recurso queda limitado a la base de datos y al estudiante. Instancias de la API con conexiones diferentes coordinan el mismo recurso en SQL Server.
- SQL Server libera el bloqueo al confirmar o revertir la transacción. El servicio desecha la transacción también cuando una regla rechaza la solicitud o hay una excepción.
- La cancelación usa el mismo recurso y transacción, de modo que una inscripción posterior comprueba el cupo después de la cancelación confirmada.
- El índice único filtrado de estudiante y materia continúa siendo una protección adicional frente a duplicados activos.
- Los repositorios y servicios conservan sus responsabilidades. La lógica SQL del bloqueo está en la unidad de trabajo; no se añade un semáforo estático o un bloqueo local al proceso.

En SQLite, utilizado en parte de las pruebas, se abre una transacción Serializable no diferida que reserva la escritura. Las pruebas concurrentes de SQLite utilizan un archivo temporal y una conexión diferente por solicitud. No se admite el proveedor InMemory para esta operación porque no proporciona las transacciones necesarias.

Las escrituras académicas deben pasar por estos servicios; una aplicación externa que escriba directamente en las tablas debe coordinar las mismas reglas.

## Respuestas HTTP

Los rechazos por máximo de materias, materia duplicada o profesor repetido conservan HTTP 400 y su mensaje de negocio.

Si no se obtiene el bloqueo SQL Server dentro de 10 segundos, la API devuelve HTTP 409 en POST o DELETE:

```json
{
  "mensaje": "Hay otra operación de inscripción en curso. Intenta nuevamente."
}
```

La operación que no obtiene el bloqueo no guarda cambios. El frontend muestra el mensaje; el usuario puede reintentar cuando termine la otra operación. Swagger documenta el nuevo estado 409.

## Verificación realizada

Se aprobaron **49 casos**, sin omisiones en la ejecución que incluyó SQL Server real:

- 45 casos generales: seis nuevos comprueban altas solapadas para límite, profesor y duplicado, cancelación solapada con alta, y respuestas 409 en POST y DELETE.
- Cuatro casos adicionales ejecutan los escenarios de altas y cancelación contra SQL Server real usando **dos instancias HTTP de la API**, claves y conexiones independientes, y una base temporal compartida.

Las pruebas mantienen la primera solicitud detenida dentro de la transacción mientras la segunda intenta iniciar su operación. Después liberan la primera, comprueban las respuestas y consultan el estado persistido. Para los escenarios de límite, profesor y duplicado, solo la primera alta se acepta y la segunda recibe el rechazo de negocio correspondiente.

Las cuatro pruebas SQL Server se ejecutaron contra el servidor Docker accesible en el puerto SQL del proyecto. Crean bases `InscripcionesConcurrency_<GUID>` y las eliminan al terminar; el fixture rechaza nombres fuera de ese formato. No se modificó `SistemaInscripcionesAcademicas` ni sus registros.

Sin la variable `SQLSERVER_TEST_CONNECTION`, el comando habitual ejecuta 45 casos y omite explícitamente los cuatro que requieren SQL Server:

```powershell
dotnet test SistemaInscripcionesAcademicas.slnx
```

Para repetir el conjunto completo, configura una conexión a un SQL Server de pruebas con permisos de crear y eliminar bases. Ejemplo con los puertos predeterminados:

```powershell
$env:SQLSERVER_TEST_CONNECTION = 'Server=localhost,14333;Database=master;User Id=sa;Password=TU_CLAVE_DE_PRUEBAS;Encrypt=True;TrustServerCertificate=True;'
dotnet test SistemaInscripcionesAcademicas.slnx
Remove-Item Env:SQLSERVER_TEST_CONNECTION
```

Usa tu contraseña local sin añadirla al repositorio. La comparación de migraciones sigue siendo válida y no se necesita una migración nueva para esta protección.

## Activación en Docker

El código fue validado sobre una copia del proyecto para conservar la API local abierta. La imagen en ejecución debe reconstruirse para incorporar los cambios de API y el mensaje de conflicto en React:

```powershell
docker compose up -d --build api web
```

Los volúmenes y los registros se conservan. No ejecutes una eliminación de volúmenes para aplicar este cambio.

Referencia del mecanismo de bloqueo: [sys.sp_getapplock, documentación de Microsoft](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17).

## Coordinación con el catálogo

Las inscripciones y cancelaciones adquieren primero el bloqueo compartido del catálogo. Las escrituras administrativas de materias lo adquieren en modo exclusivo, para impedir cambios incompatibles durante una inscripción. Consulta [gestión de materias](materias.md).
