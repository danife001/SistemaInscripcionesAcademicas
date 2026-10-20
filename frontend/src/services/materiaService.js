
export async function obtenerMaterias(token) {
  const respuesta = await fetch('/api/materias', {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (respuesta.status === 401) {
    throw new Error('Tu sesión ha expirado. Inicia sesión nuevamente.');
  }

  if (!respuesta.ok) {
    throw new Error('No fue posible obtener las materias.');
  }

  return await respuesta.json();
}
