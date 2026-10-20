
export async function registrarEstudiante(datos) {
  const respuesta = await fetch('/api/estudiantes', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(datos),
  });

  if (!respuesta.ok) {
    let mensaje = 'No fue posible registrar al estudiante.';

    try {
      const error = await respuesta.json();

      if (typeof error === 'string') {
        mensaje = error;
      } else {
        mensaje =
          error.mensaje ||
          error.message ||
          error.title ||
          mensaje;

        if (error.errors) {
          const detalles = Object.values(error.errors).flat();

          if (detalles.length > 0) {
            mensaje = detalles.join(' ');
          }
        }
      }
    } catch {
      // Conservamos el mensaje predeterminado.
    }

    throw new Error(mensaje);
  }

  return await respuesta.json();
}


async function solicitarPerfil(token, opciones = {}) {
  const respuesta = await fetch('/api/estudiantes/me', {
    ...opciones,
    headers: {
      ...opciones.headers,
      Authorization: `Bearer ${token}`,
    },
  });

  if (respuesta.status === 401) {
    throw new Error('Tu sesión ha expirado. Inicia sesión nuevamente.');
  }

  if (respuesta.status === 403) {
    throw new Error('No tienes permiso para acceder a este perfil.');
  }

  const datos = await respuesta.json();
  if (!respuesta.ok) {
    const detalles = datos.errors
      ? Object.values(datos.errors).flat().join(' ')
      : '';
    throw new Error(
      detalles || datos.mensaje || datos.title || 'No fue posible guardar el perfil.'
    );
  }

  return datos;
}

export function obtenerMiPerfil(token) {
  return solicitarPerfil(token);
}

export function actualizarMiPerfil(token, datos) {
  return solicitarPerfil(token, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(datos),
  });
}

export async function obtenerRegistrosEstudiantes(
  token, pagina = 1, tamanoPagina = 10, signal
) {
  const parametros = new URLSearchParams({ pagina, tamanoPagina });
  const respuesta = await fetch(`/api/registros/estudiantes?${parametros}`, {
    method: 'GET',
    signal,
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (respuesta.status === 401) {
    throw new Error('Tu sesión ha expirado. Inicia sesión nuevamente.');
  }

  if (respuesta.status === 403) {
    throw new Error('No tienes permiso para consultar estos registros.');
  }

  if (!respuesta.ok) {
    throw new Error('No fue posible consultar los registros.');
  }

  return await respuesta.json();
}
