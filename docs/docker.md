# Ejecución con Docker Compose

## Componentes

Compose levanta tres servicios Linux:

| Servicio | Imagen / compilación | Función |
|---|---|---|
| `sqlserver` | SQL Server 2022 Developer, imagen oficial | Persistencia relacional |
| `api` | Compilación multietapa con SDK y runtime .NET 10 | API, migraciones, roles y catálogo |
| `web` | Compilación con Node.js 24 y publicación con Nginx | React y proxy de API, Swagger y OpenAPI |

Este entorno es local y de evaluación. No realiza un despliegue en la nube ni configura dominio o TLS público. Los puertos publicados se limitan a `127.0.0.1`. SQL Server Developer se utiliza para desarrollo y pruebas.

## Requisitos

- Docker Desktop iniciado y usando contenedores Linux.
- Docker Compose v2.
- Recursos disponibles para SQL Server y conexión a Internet durante la primera descarga de imágenes y paquetes.
- Plataforma compatible con la imagen de SQL Server `linux/amd64`. La ejecución en ARM mediante emulación no queda validada por este proyecto.

No necesitas instalar Node.js, .NET o SQL Server en el host para esta modalidad.

## Configuración

En esta copia local ya se preparó un `.env` con una contraseña aleatoria. No se imprime en la documentación ni se añade a Git o a las imágenes.

Para una copia nueva del repositorio, desde la raíz:

```powershell
Copy-Item .env.example .env
```

Edita `MSSQL_SA_PASSWORD` antes de arrancar; no dejes el valor de ejemplo. Usa al menos 16 caracteres con mayúsculas, minúsculas, números y símbolos, y evita punto y coma o comillas porque se utiliza en una cadena de conexión. Los otros valores son:

```dotenv
WEB_PORT=8080
API_PORT=5234
SQL_PORT=14333
APP_ENVIRONMENT=Development
```

Si alguno de esos puertos ya está ocupado, cambia su valor. El puerto de la API en Docker se diferencia del 5233 utilizado al ejecutarla con Visual Studio o dotnet run.

## Arrancar

En una terminal con acceso al motor de Docker, desde la raíz del proyecto:

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

La primera ejecución descarga imágenes, restaura dependencias y compila. Compose espera a que SQL Server responda a una consulta antes de iniciar la API; Nginx espera a que la API esté saludable.

Con los valores predeterminados:

- [Aplicación React](http://localhost:8080)
- [Swagger a través de Nginx](http://localhost:8080/swagger)
- [Swagger directo de la API](http://localhost:5234/swagger)
- [OpenAPI](http://localhost:8080/openapi/v1.json)
- [Salud de la API](http://localhost:5234/health)
- [Salud a través de Nginx](http://localhost:8080/health)

Abre React y crea una cuenta nueva para comenzar. Swagger está disponible porque el entorno predeterminado de esta configuración local es Development. Si defines `APP_ENVIRONMENT=Production`, Swagger y OpenAPI dejan de publicarse.

## Base de datos e inicialización

Esta modalidad utiliza una **base independiente de LocalDB**, dentro del servicio `sqlserver`. No modifica la base del host y no importa automáticamente el `.dacpac` ni sus cuentas de usuario.

Al iniciar la API en Compose:

1. Se aplican las migraciones de EF Core a la base del contenedor.
2. Si no hay profesores ni materias, se crea el catálogo de 5 profesores y 10 materias de 3 créditos, con 2 materias por profesor.
3. Se crean los roles que falten.
4. Se publica `/health`, que comprueba la conexión con la base de datos.

Si existe cualquier profesor o materia, el inicializador conserva el catálogo existente y no intenta rellenarlo o reemplazarlo. La carga es repetible en un catálogo inicializado y no duplica registros. Las banderas de inicialización están habilitadas en Compose; la ejecución local convencional conserva su comportamiento anterior por defecto.

Para consultar esta base con SSMS:

- Servidor: `localhost,14333` (o el valor de `SQL_PORT`).
- Autenticación: SQL Server.
- Usuario: `sa`.
- Contraseña: el valor de tu `.env`.
- Base: `SistemaInscripcionesAcademicas`.
- Confiar en el certificado del servidor para esta conexión local.

La configuración utiliza `sa` para simplificar esta evaluación local. Un entorno publicado debe configurar una cuenta propia para la aplicación y separar la aplicación de migraciones según su despliegue.

## Volúmenes y reinicios

- `sqlserver-data` conserva la base de datos.
- `api-logs` conserva los archivos JSON de diagnóstico en `/app/logs`, con rotación diaria y por tamaño; consulta [logs](logs.md).
- `api-keys` conserva las claves de Data Protection utilizadas por Identity, para que los tokens puedan seguir validándose después de reiniciar la API mientras estén vigentes.

Para detener los servicios conservando datos:

```powershell
docker compose down
```

Para volver a iniciarlos:

```powershell
docker compose up -d
```

Para incorporar cambios del código:

```powershell
docker compose up -d --build
```

No borres los volúmenes para actualizar el código. Eliminar `sqlserver-data` elimina los datos de esa base. Si cambias la contraseña en `.env` después de inicializar SQL Server, debes actualizar también la contraseña del login en esa base; cambiar la variable por sí solo no rota una contraseña ya persistida.

## Diagnóstico y comprobación

```powershell
docker compose ps
docker compose logs --tail=100 sqlserver
docker compose logs --tail=100 api
docker compose logs --tail=100 web
```

- `sqlserver` debe pasar a `healthy`; si falla, revisa la contraseña, los recursos y los logs.
- La API debe aplicar las migraciones y responder `Healthy` en `/health`.
- React debe permitir registrar un estudiante, iniciar sesión y consultar el catálogo.
- Swagger debe cargar por el puerto 8080 y permitir ejecutar una consulta protegida después de Authorize.

Nginx sirve los archivos compilados de React y dirige `/api`, `/swagger`, `/openapi` y `/health` a la API. Conserva el puerto del encabezado Host para que las solicitudes de Swagger funcionen por el mismo origen.

Si Docker informa «Acceso denegado» al canal del motor, utiliza una terminal de tu sesión de Windows con acceso a Docker Desktop. Si indica que no existe el canal, comprueba que Docker Desktop haya terminado de iniciar.

## Estado de validación

Se validó la sintaxis y la resolución de Compose con `docker compose config --quiet`. La última ejecución completa aprobó 74 pruebas del proyecto, incluidas la carga repetible del catálogo, la conservación de datos existentes, el endpoint de salud y la sincronización del modelo con sus migraciones SQL Server.

El usuario ejecutó la construcción y confirmó el funcionamiento en Docker. Desde el entorno de revisión se comprobaron HTTP 200 para React, Swagger y OpenAPI, y Healthy tanto por Nginx como por la API directa. El acceso al canal del motor desde el entorno sigue restringido; los nuevos cambios de concurrencia requieren reconstruir API y web con `docker compose up -d --build api web`. Las pruebas SQL Server del código actualizado ya pasaron sobre bases temporales del servidor en contenedor.

Referencias: [SQL Server en Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17) y [orden de inicio de Compose](https://docs.docker.com/compose/how-tos/startup-order).

En esta copia local se configuró `API_PORT=5235` para resolver un conflicto con 5234. Los enlaces directos de ejemplo usan el valor predeterminado; con esta configuración la API está en `http://localhost:5235`. React y Swagger por Nginx permanecen en el puerto 8080.

## Cuenta administradora

Compose acepta `ADMIN_CREAR_AL_INICIAR`, `ADMIN_CORREO`, `ADMIN_NOMBRE` y `ADMIN_PASSWORD`. Consulta [configuración y uso del administrador](administrador.md). La cuenta es opcional y se crea al iniciar la API actualizada.
