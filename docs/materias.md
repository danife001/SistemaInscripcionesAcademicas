# Gestión administrativa de materias

La API expone estas operaciones mediante `IMateriaService`, `IMateriaRepository` y la unidad de trabajo:

| Método | Ruta | Permiso | Resultado |
|---|---|---|---|
| GET | `/api/materias` | Administrador o Estudiante | Administrador: catálogo completo con inactivas. Estudiante: solo materias y profesores activos. |
| GET | `/api/materias/{id}` | Administrador | Detalle, incluidas materias inactivas. |
| POST | `/api/materias` | Administrador | Crea una materia activa; 201 y encabezado Location. |
| PUT | `/api/materias/{id}` | Administrador | Actualiza una materia activa sin alterar historial; 200. |
| DELETE | `/api/materias/{id}` | Administrador | Baja lógica si no hay inscripciones activas; 204. |

La consulta de la lista conserva acceso para estudiantes porque el portal la utiliza para inscribirse. Las otras operaciones están restringidas al administrador. No se añadió una pantalla React de mantenimiento de materias; las operaciones se pueden usar desde Swagger o un cliente HTTP.

## Datos y validaciones

POST y PUT reciben el mismo cuerpo. Sustituye `profesorId` por un profesor activo que exista:

```json
{
  "nombre": "Álgebra",
  "creditos": 3,
  "profesorId": 1
}
```

La respuesta incluye `id`, `nombre`, `creditos`, `profesorId`, `profesor` y `activo`.

- Nombre obligatorio, sin espacios vacíos y con un máximo de 200 caracteres.
- Exactamente 3 créditos por materia.
- Profesor existente y activo, con un máximo de 2 materias activas. Las inactivas no ocupan una plaza del catálogo.
- El catálogo inicial mantiene 10 materias y 5 profesores con 2 materias cada uno. Una baja puede reducir temporalmente esas cantidades; permite crear un reemplazo sin superar el límite por profesor. No se obliga a conservar exactamente dos durante cada operación de mantenimiento.
- Las materias con cualquier historial de inscripciones, incluso canceladas, no pueden cambiar su nombre, profesor ni créditos. El historial referencia la misma entidad y esos cambios alterarían los registros previos.
- Se rechaza la baja si quedan inscripciones activas. Al desactivar se conserva la fila, el profesor y todas las inscripciones previas.
- Las materias desactivadas no admiten modificación, nueva inscripción ni reactivación mediante este contrato.

## Respuestas de error

- 400: entrada inválida, por ejemplo créditos distintos de 3, nombre vacío o identificador de profesor no positivo.
- 401: sesión ausente o inválida.
- 403: usuario sin rol autorizado.
- 404: materia inexistente; una segunda baja de una materia ya inactiva también devuelve 404.
- 409: regla de negocio que impide la operación (profesor inexistente/inactivo, dos materias activas, historial protegido o inscripciones activas) o bloqueo temporal. El cuerpo incluye `mensaje`.

## Operaciones simultáneas

SQL Server coordina las escrituras del catálogo mediante un bloqueo exclusivo transaccional `Inscripciones:Catalogo`. Las altas y cancelaciones de inscripciones adquieren primero ese mismo bloqueo en modo compartido y después el bloqueo exclusivo por estudiante. De este modo, los estudiantes pueden operar en paralelo y las modificaciones administrativas esperan hasta que se confirme el estado que necesitan validar.

El orden de adquisición es el mismo en todas las operaciones. El bloqueo se libera al confirmar o revertir; una espera de bloqueo fallida devuelve 409. SQLite utiliza una transacción de escritura para las pruebas. No se requieren migraciones.

## Comprobación

En Swagger, inicia sesión con el administrador, pulsa Authorize y prueba POST, GET por ID, PUT y DELETE. Un profesor del catálogo inicial ya tiene dos materias: para crear un reemplazo, desactiva primero una materia que no tenga inscripciones activas.

Las pruebas `MateriasAdministracionTests` cubren CRUD, créditos, cupo por profesor, profesores inexistentes/inactivos, permisos, historial, bajas repetidas y catálogo visible por rol. Las pruebas SQL Server adicionales comprueban altas concurrentes para un mismo profesor y una baja que coincide con una inscripción.

Para activar el backend actualizado en Docker:

```powershell
docker compose up -d --build api
```
