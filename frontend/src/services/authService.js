
export async function iniciarSesion(correo, password) {
  const respuesta = await fetch('/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      correo,
      password,
    }),
  });

  if (!respuesta.ok) {
    if (respuesta.status === 401) {
      throw new Error('Correo o contraseña incorrectos.');
    }

    throw new Error('No fue posible iniciar sesión.');
  }

  const datos = await respuesta.json();

  if (!datos.accessToken) {
    throw new Error('La API no devolvió un token de acceso.');
  }

  return datos;
}

export async function obtenerSesion(token) {
  const respuesta = await fetch('/api/auth/me', {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!respuesta.ok) throw new Error('No fue posible verificar la sesión. Vuelve a iniciar sesión.');
  return respuesta.json();
}
