export async function solicitudAdmin(token, ruta = '', opciones = {}) {
  const respuesta = await fetch(`/api/estudiantes${ruta}`, {
    ...opciones,
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
  });
  if (!respuesta.ok) {
    const datos = await respuesta.json().catch(() => ({}));
    throw new Error(datos.mensaje || Object.values(datos.errors || {}).flat().join(' ') ||
      (respuesta.status === 403 ? 'No tienes permisos de administrador.' : 'No fue posible completar la operación.'));
  }
  return respuesta.status === 204 ? null : respuesta.json();
}
