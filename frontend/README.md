# Frontend del Sistema de Inscripciones Académicas

Portal de estudiantes desarrollado en React y Vite. Consulta la [documentación principal](../README.md) para configurar la base de datos, ejecutar la API y revisar los endpoints.

## Ejecutar en desarrollo

Desde esta carpeta, con Node.js 24 y la API iniciada en `http://localhost:5233`:

```powershell
npm ci
npm run dev
```

Abre la dirección indicada por Vite, habitualmente `http://localhost:5173`. Las solicitudes a `/api` se envían al backend mediante el proxy de `vite.config.js`.

## Comprobar y compilar

```powershell
npm run lint
npm run build
```

La compilación se guarda en `dist/`. El servidor utilizado al publicar estos archivos debe configurar el acceso a `/api`; el proxy de Vite solo corresponde al desarrollo.

## Pantallas

- Registro e inicio de sesión.
- Materias disponibles e inscripción.
- Inscripciones propias, cancelación y compañeros.
- Registros de otros estudiantes con paginación.
- Perfil propio y actualización de nombre y correo.

## Docker

El Dockerfile compila React con Node.js 24 y sirve el resultado con Nginx. Desde la raíz del repositorio, `docker compose up -d --build` levanta el frontend junto a la API y SQL Server. Consulta [la guía de Docker](../docs/docker.md).
