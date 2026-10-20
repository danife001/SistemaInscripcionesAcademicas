import { useEffect, useRef, useState } from 'react';
import { solicitudAdministrativa } from '../services/adminService';

export default function AdministracionMaterias({ token }) {
  const [catalogo, setCatalogo] = useState(null);
  const [profesores, setProfesores] = useState([]);
  const [version, setVersion] = useState(0);
  const [cargando, setCargando] = useState(true);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [formulario, setFormulario] = useState(null);
  const [confirmacion, setConfirmacion] = useState(null);
  const [filtro, setFiltro] = useState('todas');
  const nombreRef = useRef(null);

  useEffect(() => {
    const control = new AbortController();
    Promise.all([
      solicitudAdministrativa(token, '/api/materias', { signal: control.signal }),
      solicitudAdministrativa(token, '/api/profesores', { signal: control.signal }),
    ]).then(([materias, docentes]) => {
      if (!control.signal.aborted) { setCatalogo(materias); setProfesores(docentes); setCargando(false); }
    }).catch(e => {
      if (e.name !== 'AbortError' && !control.signal.aborted) { setError(e.message); setCargando(false); }
    });
    return () => control.abort();
  }, [token, version]);

  const formularioId = formulario?.id;
  useEffect(() => { if (formularioId !== undefined) nombreRef.current?.focus(); }, [formularioId]);

  function recargar() { setError(''); setCargando(true); setVersion(v => v + 1); }
  function abrir(materia) {
    setError(''); setMensaje(''); setConfirmacion(null);
    setFormulario(materia ? { id: materia.id, nombre: materia.nombre, profesorId: String(materia.profesorId) }
      : { id: 0, nombre: '', profesorId: '' });
  }
  function cantidadProfesor(id) {
    return (catalogo || []).filter(m => m.activo && m.profesorId === id && m.id !== formulario?.id).length;
  }
  async function guardar(evento) {
    evento.preventDefault(); setError(''); setMensaje('');
    if (!formulario.nombre.trim()) { setError('El nombre es obligatorio.'); return; }
    setOcupado(true);
    try {
      await solicitudAdministrativa(token, `/api/materias${formulario.id ? `/${formulario.id}` : ''}`, {
        method: formulario.id ? 'PUT' : 'POST',
        body: JSON.stringify({ nombre: formulario.nombre.trim(), creditos: 3, profesorId: Number(formulario.profesorId) }),
      });
      setFormulario(null); setMensaje('Materia guardada correctamente.'); recargar();
    } catch (e) { setError(e.message); } finally { setOcupado(false); }
  }
  async function desactivar() {
    setOcupado(true); setError(''); setMensaje('');
    try {
      await solicitudAdministrativa(token, `/api/materias/${confirmacion.id}`, { method: 'DELETE' });
      setConfirmacion(null); setMensaje('Materia desactivada. Se conserva su historial.'); recargar();
    } catch (e) { setError(e.message); } finally { setOcupado(false); }
  }
  const bloqueado = ocupado || cargando;
  const visibles = (catalogo || []).filter(m => filtro === 'todas' || m.activo === (filtro === 'activas'));
  return <section>
    <h2>Materias</h2>
    <p>Cada materia tiene 3 créditos. Un profesor puede tener hasta 2 materias activas.</p>
    <p>Las materias con historial no se pueden modificar y las que tienen inscripciones activas no se pueden desactivar.</p>
    {error && <p className="error" role="alert">{error}</p>}
    {mensaje && <p className="mensaje-exito" role="status">{mensaje}</p>}
    <div className="admin-acciones">
      <button disabled={bloqueado || !catalogo || profesores.length === 0} onClick={() => abrir(null)}>Crear materia</button>
      <button className="boton-secundario" disabled={bloqueado} onClick={recargar}>Actualizar catálogo</button>
    </div>
    {!cargando && catalogo && profesores.length === 0 && <p>No hay profesores activos disponibles para asignar materias.</p>}
    {formulario && <form className="login-form admin-formulario" onSubmit={guardar}>
      <h3>{formulario.id ? 'Editar materia' : 'Crear materia'}</h3>
      <label htmlFor="materia-nombre">Nombre</label>
      <input ref={nombreRef} id="materia-nombre" required maxLength={200} value={formulario.nombre} disabled={bloqueado}
        onChange={e => setFormulario({ ...formulario, nombre: e.target.value })} />
      <label htmlFor="materia-creditos">Créditos</label>
      <input id="materia-creditos" value="3" readOnly />
      <label htmlFor="materia-profesor">Profesor</label>
      <select id="materia-profesor" required disabled={bloqueado} value={formulario.profesorId}
        onChange={e => setFormulario({ ...formulario, profesorId: e.target.value })}>
        <option value="">Selecciona un profesor</option>
        {formulario.profesorId && !profesores.some(p => String(p.id) === formulario.profesorId) &&
          <option value={formulario.profesorId} disabled>Profesor actual no disponible</option>}
        {profesores.map(p => <option key={p.id} value={p.id} disabled={cantidadProfesor(p.id) >= 2}>
          {p.nombre} — {cantidadProfesor(p.id)}/2 materias asignadas{cantidadProfesor(p.id) >= 2 ? ' (sin cupo)' : ''}
        </option>)}
      </select>
      <p>El cupo mostrado excluye la materia que estás editando. La API vuelve a validar al guardar.</p>
      <button type="submit" disabled={bloqueado || !formulario.profesorId}>Guardar materia</button>
      <button type="button" className="boton-secundario" disabled={ocupado} onClick={() => setFormulario(null)}>Cancelar edición</button>
    </form>}
    {confirmacion && <div className="admin-confirmacion" role="region" aria-label="Confirmar desactivación">
      <p>¿Desactivar <strong>{confirmacion.nombre}</strong>? Dejará de estar disponible para nuevas inscripciones; su historial se conservará.</p>
      <div className="admin-acciones">
        <button className="boton-cancelar" disabled={bloqueado} onClick={desactivar}>Confirmar desactivación</button>
        <button className="boton-secundario" disabled={ocupado} onClick={() => setConfirmacion(null)}>Cancelar desactivación</button>
      </div>
    </div>}
    <div className="paginacion-opciones">
      <label htmlFor="materia-filtro">Mostrar</label>
      <select id="materia-filtro" value={filtro} onChange={e => setFiltro(e.target.value)}>
        <option value="todas">Todas</option><option value="activas">Activas</option><option value="inactivas">Inactivas</option>
      </select>
    </div>
    {cargando ? <p role="status">Cargando catálogo...</p> : catalogo && <>
      <p>{visibles.length} materias visibles.</p>
      {visibles.length === 0 ? <p>No hay materias para este filtro.</p> : <div className="admin-tabla-contenedor">
        <table className="admin-tabla"><caption>Catálogo de materias</caption>
          <thead><tr><th>Nombre</th><th>Profesor</th><th>Créditos</th><th>Estado</th><th>Acciones</th></tr></thead>
          <tbody>{visibles.map(m => <tr key={m.id}>
            <td>{m.nombre}</td><td>{m.profesor}</td><td>{m.creditos}</td><td>{m.activo ? 'Activa' : 'Inactiva'}</td>
            <td><div className="admin-acciones">
              <button disabled={bloqueado || !m.activo} onClick={() => abrir(m)} aria-label={`Editar ${m.nombre}`}>Editar</button>
              <button className="boton-cancelar" disabled={bloqueado || !m.activo}
                aria-label={`Desactivar ${m.nombre}`} onClick={() => {
                  setConfirmacion(m); setFormulario(null); setError(''); setMensaje('');
                }}>Desactivar</button>
            </div></td>
          </tr>)}</tbody></table>
      </div>}
    </>}
  </section>;
}
