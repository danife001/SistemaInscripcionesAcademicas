import { useEffect, useState } from 'react';
import { solicitudAdmin } from '../services/adminService';

const vacio = { nombre: '', correo: '', password: '' };

export default function Administracion({ token }) {
  const [pagina, setPagina] = useState(1);
  const [version, setVersion] = useState(0);
  const [datos, setDatos] = useState(null);
  const [cargando, setCargando] = useState(true);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [edicion, setEdicion] = useState(null);
  const [formulario, setFormulario] = useState(null);

  useEffect(() => {
    const control = new AbortController();
    solicitudAdmin(token, `?pagina=${pagina}&tamanoPagina=10`, { signal: control.signal })
      .then(resultado => {
        // Una baja puede vaciar la última página; vuelve a la última que aún existe.
        if (pagina > Math.max(1, resultado.totalPaginas)) {
          setPagina(Math.max(1, resultado.totalPaginas));
        } else { setDatos(resultado); setCargando(false); }
      }).catch(e => {
        if (e.name !== 'AbortError') { setError(e.message); setCargando(false); }
      });
    return () => control.abort();
  }, [token, pagina, version]);

  function recargar() { setCargando(true); setVersion(v => v + 1); }
  function abrir(estudiante) {
    setEdicion(estudiante?.id ?? null);
    setFormulario(estudiante ? { nombre: estudiante.nombre, correo: estudiante.correo } : { ...vacio });
    setError(''); setMensaje('');
  }
  async function guardar(e) {
    e.preventDefault(); setOcupado(true); setError(''); setMensaje('');
    try {
      await solicitudAdmin(token, edicion ? `/${edicion}` : '', {
        method: edicion ? 'PUT' : 'POST',
        body: JSON.stringify({ ...formulario, nombre: formulario.nombre.trim(), correo: formulario.correo.trim() }),
      });
      setFormulario(null); setMensaje('Estudiante guardado correctamente.'); recargar();
    } catch (e) { setError(e.message); } finally { setOcupado(false); }
  }
  async function desactivar(estudiante) {
    if (!window.confirm(`¿Desactivar a ${estudiante.nombre}? No podrá volver a iniciar sesión.`)) return;
    setOcupado(true); setError(''); setMensaje('');
    try {
      await solicitudAdmin(token, `/${estudiante.id}`, { method: 'DELETE' });
      setMensaje('Estudiante desactivado.'); recargar();
    } catch (e) { setError(e.message); } finally { setOcupado(false); }
  }
  return <section>
    <h2>Estudiantes activos</h2>
    {error && <p className="error" role="alert">{error}</p>}
    {mensaje && <p role="status">{mensaje}</p>}
    <button disabled={ocupado} onClick={() => abrir(null)}>Crear estudiante</button>
    <button disabled={ocupado || cargando} onClick={() => { setError(''); recargar(); }}>Actualizar lista</button>
    {formulario && <form className="login-form" onSubmit={guardar}>
      <h3>{edicion ? 'Editar estudiante' : 'Crear estudiante'}</h3>
      <label htmlFor="admin-nombre">Nombre</label>
      <input id="admin-nombre" required maxLength={200} disabled={ocupado} value={formulario.nombre}
        onChange={e => setFormulario({ ...formulario, nombre: e.target.value })} />
      <label htmlFor="admin-correo">Correo</label>
      <input id="admin-correo" type="email" required maxLength={256} disabled={ocupado} value={formulario.correo}
        onChange={e => setFormulario({ ...formulario, correo: e.target.value })} />
      {!edicion && <><label htmlFor="admin-password">Contraseña inicial</label>
        <input id="admin-password" type="password" autoComplete="new-password" required minLength={6}
          disabled={ocupado} value={formulario.password}
          onChange={e => setFormulario({ ...formulario, password: e.target.value })} />
        <p>Incluye mayúscula, minúscula, número y símbolo.</p></>}
      <button disabled={ocupado} type="submit">{ocupado ? 'Guardando...' : 'Guardar'}</button>
      <button disabled={ocupado} type="button" onClick={() => setFormulario(null)}>Cancelar</button>
    </form>}
    {cargando ? <p role="status">Cargando estudiantes...</p> : datos && <>
      <p>{datos.totalRegistros} estudiantes. Página {datos.pagina} de {Math.max(1, datos.totalPaginas)}.</p>
      {datos.items.length === 0 ? <p>No hay estudiantes activos.</p> :
        <div style={{ overflowX: 'auto' }}><table><thead><tr><th>Nombre</th><th>Correo</th><th>Acciones</th></tr></thead>
          <tbody>{datos.items.map(estudiante => <tr key={estudiante.id}>
            <td>{estudiante.nombre}</td><td>{estudiante.correo}</td><td>
              <button disabled={ocupado} onClick={() => abrir(estudiante)}>Editar</button>
              <button disabled={ocupado} onClick={() => desactivar(estudiante)}>Desactivar</button>
            </td></tr>)}</tbody></table></div>}
      <button disabled={ocupado || !datos.hayAnterior} onClick={() => { setCargando(true); setPagina(p => p - 1); }}>Anterior</button>
      <button disabled={ocupado || !datos.haySiguiente} onClick={() => { setCargando(true); setPagina(p => p + 1); }}>Siguiente</button>
    </>}
  </section>;
}
