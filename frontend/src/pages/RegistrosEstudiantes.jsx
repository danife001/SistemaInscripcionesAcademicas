import { useEffect, useState } from 'react';
import { obtenerRegistrosEstudiantes } from '../services/estudianteService';

function RegistrosEstudiantes({ token }) {
  const [consulta, setConsulta] = useState({ pagina: 1, tamanoPagina: 10 });
  const [respuesta, setRespuesta] = useState(null);
  const { pagina, tamanoPagina } = consulta;
  const vigente = respuesta?.consulta === consulta && respuesta?.token === token;
  const cargando = !vigente;
  const datos = vigente ? respuesta.datos : null;
  const error = vigente ? respuesta.error : '';
  const registros = datos?.items ?? [];

  useEffect(() => {
    const controller = new AbortController();

    async function cargarRegistros() {
      try {
        const datos = await obtenerRegistrosEstudiantes(
          token, consulta.pagina, consulta.tamanoPagina, controller.signal
        );
        if (controller.signal.aborted) return;

        // Si se eliminaron registros, vuelve a la última página disponible.
        const ultimaPagina = Math.max(1, datos.totalPaginas);
        if (consulta.pagina > ultimaPagina) {
          setConsulta({ ...consulta, pagina: ultimaPagina });
          return;
        }

        setRespuesta({ consulta, token, datos, error: '' });
      } catch (error) {
        if (!controller.signal.aborted) {
          setRespuesta({ consulta, token, datos: null, error: error.message });
        }
      }
    }

    cargarRegistros();
    return () => controller.abort();
  }, [token, consulta]);

  return (
    <section aria-busy={cargando}>
      <h2>Registros de estudiantes</h2>
      <p>Consulta las materias inscritas por otros estudiantes.</p>

      <div className="paginacion-opciones">
        <label htmlFor="registros-por-pagina">Estudiantes por página</label>
        <select
          id="registros-por-pagina"
          value={tamanoPagina}
          disabled={cargando}
          onChange={(evento) => setConsulta({
            pagina: 1, tamanoPagina: Number(evento.target.value),
          })}
        >
          {[5, 10, 20, 50, 100].map((cantidad) => (
            <option key={cantidad} value={cantidad}>{cantidad}</option>
          ))}
        </select>
      </div>

      {cargando && <p role="status">Cargando registros de estudiantes...</p>}
      {error && (
        <div>
          <p className="error" role="alert">{error}</p>
          <button onClick={() => setConsulta({ ...consulta })}>Reintentar</button>
        </div>
      )}

      {datos && (
        <>
          <p role="status" aria-live="polite">
            {datos.totalRegistros === 0
              ? 'No hay otros estudiantes registrados.'
              : `Mostrando ${(pagina - 1) * tamanoPagina + 1}–${
                (pagina - 1) * tamanoPagina + registros.length
              } de ${datos.totalRegistros} estudiantes.`}
          </p>

          <div className="materias-grid">
            {registros.map((registro, indice) => (
              <article key={`${pagina}-${indice}`} className="materia-card">
                <h3>{registro.nombre}</h3>
                <h4>Materias inscritas</h4>
                {registro.materias.length === 0 ? (
                  <p>No tiene materias inscritas actualmente.</p>
                ) : (
                  <ul>
                    {registro.materias.map((materia, materiaIndice) => (
                      <li key={materiaIndice}>{materia}</li>
                    ))}
                  </ul>
                )}
              </article>
            ))}
          </div>
        </>
      )}

      <nav className="paginacion" aria-label="Paginación de estudiantes">
        <button
          disabled={cargando || pagina <= 1}
          onClick={() => setConsulta({ ...consulta, pagina: pagina - 1 })}
        >
          Anterior
        </button>
        <span>
          {datos && datos.totalPaginas === 0
            ? 'Sin resultados'
            : `Página ${pagina}${datos ? ` de ${datos.totalPaginas}` : ''}`}
        </span>
        <button
          disabled={cargando || !datos?.haySiguiente}
          onClick={() => setConsulta({ ...consulta, pagina: pagina + 1 })}
        >
          Siguiente
        </button>
      </nav>
    </section>
  );
}

export default RegistrosEstudiantes;
