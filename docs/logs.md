# Logs persistentes

La API conserva los eventos de `ILogger` en archivos JSON Lines mediante Serilog. Cada línea es un objeto JSON con fecha, nivel, plantilla/mensaje, categoría, propiedades y excepción cuando corresponda. Se mantienen los proveedores de consola existentes.

## Ubicación y conservación

- Local: `SistemaInscripcionesAcademicas.Api/logs/api-AAAAMMDD.jsonl`, relativo al content root de la API.
- Docker: `/app/logs/api-AAAAMMDD.jsonl`, montado en el volumen `api-logs` de Compose.
- Rotación diaria y al superar aproximadamente 10 MB. Un evento individual puede sobrepasar ese límite antes de que rote el siguiente archivo.
- Se conservan los 14 archivos más recientes, incluidos los segmentos por tamaño; no equivale necesariamente a 14 días.
- La escritura no utiliza buffering del sink; el proveedor se cierra al detener el host.

El volumen conserva los archivos al reiniciar o recrear la API y con `docker compose down` sin eliminación de volúmenes. Borrar el volumen de logs elimina esos archivos. El Dockerfile prepara la carpeta para el usuario no root de la API. Los logs están excluidos de Git y del contexto de construcción.

## Configuración

Valores en `Logging:File` de `appsettings.json`:

```json
{
  "Enabled": true,
  "Directory": "logs",
  "FileSizeLimitBytes": 10485760,
  "RetainedFileCountLimit": 14
}
```

Se pueden sobrescribir mediante `Logging__File__Enabled`, `Logging__File__Directory`, `Logging__File__FileSizeLimitBytes` y `Logging__File__RetainedFileCountLimit`. Compose fija el directorio a `/app/logs`. Tamaño y retención deben ser positivos.

El proveedor de archivo admite Information o superior para la aplicación y Warning o superior para categorías Microsoft. También se aplican los filtros configurados en `Logging:LogLevel`. No se registran automáticamente cuerpos HTTP, cabeceras Authorization, tokens ni contraseñas. Las excepciones técnicas pueden contener detalles internos: los archivos son para diagnóstico del servidor y no se exponen mediante endpoints ni Swagger. Es un registro de diagnóstico, no una auditoría completa de operaciones de negocio.

## Activar y consultar

Desde la raíz:

```powershell
docker compose up -d --build api
docker compose exec api ls -lh /app/logs
```

Consultar las últimas líneas de los archivos:

```powershell
docker compose exec api sh -c 'tail -n 30 /app/logs/api-*.jsonl'
```

Copiar los archivos al equipo para revisarlos:

```powershell
docker compose cp api:/app/logs ./logs-exportados
```

No adjuntes esa copia a la entrega del proyecto. Para consultar el archivo más reciente en ejecución local:

```powershell
$archivoLog = Get-ChildItem ./SistemaInscripcionesAcademicas.Api/logs/api-*.jsonl | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content -LiteralPath $archivoLog.FullName -Tail 30
```

El manejador de excepciones devuelve `traceId`; busca ese mismo valor en el archivo para localizar el error. Los errores inesperados conservan la excepción en el servidor y devuelven un mensaje genérico al cliente.

## Verificación

`LogsPersistentesTests` verifica conservación al reabrir el logger, formato JSON, rotación por tamaño con retención, escritura paralela y persistencia de un fallo HTTP real con el mismo traceId de su respuesta. Las pruebas usan carpetas temporales aisladas. La configuración de Compose se valida aparte; la recreación del contenedor con el volumen nuevo requiere reconstruir la API.

Referencias: [sink de archivos de Serilog](https://github.com/serilog/serilog-sinks-file) e [integración con ILogger](https://github.com/serilog/serilog-extensions-logging).
