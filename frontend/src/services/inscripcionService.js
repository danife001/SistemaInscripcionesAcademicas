
export async function obtenerInscripciones(token) {
  const respuesta = await fetch('/api/inscripciones', {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!respuesta.ok) {
    throw new Error('No fue posible consultar tus inscripciones.');
  }

  return await respuesta.json();
}

export async function inscribirMateria(token, materiaId) {
  const respuesta = await fetch('/api/inscripciones', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({
      materiaId: materiaId,
    }),
  });

  if (!respuesta.ok) {
    let mensaje = 'No fue posible realizar la inscripción.';

    try {
      const error = await respuesta.json();

      mensaje =
        error.mensaje ||
        error.message ||
        error.title ||
        mensaje;
    } catch {
      // Se conserva el mensaje predeterminado.
    }

    if (respuesta.status === 401) {
      mensaje = 'Tu sesión ha expirado. Inicia sesión nuevamente.';
    }

    throw new Error(mensaje);
  }

  return true;
}


export async function cancelarInscripcion(token, inscripcionId) {
  const respuesta = await fetch(
    `/api/inscripciones/${inscripcionId}`,
    {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (respuesta.status === 409) {
    const error = await respuesta.json().catch(() => null);
    throw new Error(
      error?.mensaje || 'Hay otra operación de inscripción en curso. Intenta nuevamente.'
    );
  }

  if (!respuesta.ok) {
    throw new Error('No fue posible cancelar la inscripción.');
  }
}

export async function obtenerCompas(token, materiaId) {
  const respuesta = await fetch(
    `/api/inscripciones/materias/${materiaId}/compas`,
    {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!respuesta.ok) {
    throw new Error('No fue posible consultar los compas.');
  }

  return await respuesta.json();
}
