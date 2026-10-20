# Swagger UI

## Acceso

Con SQL Server configurado y la API iniciada mediante el perfil de desarrollo:

```powershell
dotnet run --project SistemaInscripcionesAcademicas.Api --launch-profile http
```

- [Interfaz Swagger UI](http://localhost:5233/swagger)
- [Documento OpenAPI](http://localhost:5233/openapi/v1.json)

En el perfil `https`, las mismas rutas están disponibles bajo `https://localhost:7007`. Las dos están habilitadas solo en Development.

## Autenticación Bearer

1. Registra una cuenta desde el frontend o con `POST /api/Estudiantes`.
2. Despliega `POST /api/auth/login` y completa el cuerpo JSON:

```json
{
  "correo": "estudiante@example.com",
  "password": "TuPassword123!"
}
```

3. Pulsa **Execute** y copia el valor `accessToken` de la respuesta correcta.
4. Pulsa **Authorize** en la parte superior. Pega únicamente el token, sin comillas ni el prefijo Bearer, y pulsa **Apply credentials**; cierra el diálogo.
5. Despliega `GET /api/estudiantes/me` y pulsa **Execute**. Una cuenta activa y un token vigente producen HTTP 200 con el perfil.
6. Para retirar el token, vuelve a Authorize y pulsa **Remove authorization**. Si expira, vuelve a ejecutar el login.

El token es de ASP.NET Core Identity y no se presupone que sea JWT. Swagger configura el esquema HTTP Bearer y añade la cabecera. Las credenciales no se guardan de forma persistente en la interfaz.

## Operaciones y permisos

- Registro y login son públicos.
- Los candados identifican las operaciones que requieren token. La descripción indica sus roles.
- El login permite estudiantes activos y administradores. Usa la cuenta configurada según [la guía de administración](administrador.md) para probar las rutas administrativas; un token de estudiante recibe 403 en ellas.
- Los esquemas muestran los cuerpos de entrada y salida. Se documentan los estados de creación, cancelación y errores principales.
- Las rutas de estudiantes admiten `pagina` desde 1 y `tamanoPagina` entre 1 y 100; si se omiten, utilizan 1 y 10.

## Verificación

Se verificaron la carga de la interfaz y los recursos, el documento OpenAPI, el esquema Bearer, la ausencia de requisitos de token en registro y login, los contratos de respuesta y la ausencia de documentación en Production. En navegador se ejecutaron login, Authorize y consulta protegida del perfil con HTTP 200.

Estas comprobaciones utilizaron la API real con una base SQLite aislada. No sustituyen la validación de conectividad a SQL Server local.

Referencias: [uso de OpenAPI con Swagger UI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0) y [personalización de OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0).
