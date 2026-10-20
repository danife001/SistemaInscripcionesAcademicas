# Paginación de estudiantes

Las dos consultas de estudiantes se paginan en la base de datos:

- `GET /api/registros/estudiantes`: requiere un estudiante autenticado y activo. Excluye su propio registro y los estudiantes inactivos.
- `GET /api/estudiantes`: requiere el rol Administrador y devuelve estudiantes activos.

Ambas aceptan `pagina` (entero desde 1, predeterminado 1) y `tamanoPagina` (entero entre 1 y 100, predeterminado 10). Los parámetros inválidos producen HTTP 400. Una página posterior a la última devuelve HTTP 200 con `items` vacío y conserva los totales.

Ejemplo: `GET /api/registros/estudiantes?pagina=2&tamanoPagina=5`, con la cabecera `Authorization: Bearer <token>`.

```json
{
  "items": [
    { "nombre": "Ana Pérez", "materias": ["Matemáticas"] }
  ],
  "pagina": 2,
  "tamanoPagina": 5,
  "totalRegistros": 6,
  "totalPaginas": 2,
  "hayAnterior": true,
  "haySiguiente": false
}
```

La consulta administrativa usa el mismo formato y sus elementos contienen `id`, `nombre`, `correo` y `activo`. Estas respuestas reemplazan las listas JSON sin metadatos de la versión anterior; los clientes deben leer `items`.

Los resultados se ordenan por nombre y después por ID para resolver los nombres repetidos. Una consulta cuenta los registros y otra recupera únicamente la página solicitada. Si los datos cambian entre solicitudes, pueden cambiar los totales y las posiciones.

En React, abre **Registros de estudiantes**. Usa **Anterior**, **Siguiente** y el selector de cantidad. Cambiar la cantidad vuelve a la primera página. Si la página deja de existir porque se eliminaron o desactivaron registros, la pantalla vuelve a la última disponible. Los errores permiten reintentar y las solicitudes anteriores se cancelan al navegar.

No se requieren migraciones ni cambios en los datos locales.

Para verificar:

```powershell
dotnet test SistemaInscripcionesAcademicas.Tests/SistemaInscripcionesAcademicas.Tests.csproj
cd frontend
npm run build
npm run lint
```
