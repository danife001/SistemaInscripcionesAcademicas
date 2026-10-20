
import { useEffect, useState } from 'react';
import {
  obtenerInscripciones,
  cancelarInscripcion,
  obtenerCompas,
} from '../services/inscripcionService';

function MisInscripciones({ token }) {
  const [inscripciones, setInscripciones] = useState([]);
  const [cargando, setCargando] = useState(true);
  const [procesandoId, setProcesandoId] = useState(null);
  const [compas, setCompas] = useState({});
  const [cargandoCompasId, setCargandoCompasId] = useState(null);
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');

  useEffect(() => {
    async function cargarInscripciones() {
      try {
        const datos = await obtenerInscripciones(token);
        setInscripciones(datos);
      } catch (error) {
        setError(error.message);
      } finally {
        setCargando(false);
      }
    }

    cargarInscripciones();
  }, [token]);

  const manejarCancelacion = async (inscripcion) => {
    const confirmar = window.confirm(
      `¿Deseas cancelar la inscripción en ${inscripcion.materia}?`
    );

    if (!confirmar) return;

    setError('');
    setMensaje('');
    setProcesandoId(inscripcion.id);

    try {
      await cancelarInscripcion(token, inscripcion.id);

      const datos = await obtenerInscripciones(token);
      setInscripciones(datos);

      setMensaje(
        `Cancelaste correctamente la inscripción en ${inscripcion.materia}.`
      );
    } catch (error) {
      setError(error.message);
    } finally {
      setProcesandoId(null);
    }
  };

  const manejarCompas = async (materiaId) => {
    setError('');
    setCargandoCompasId(materiaId);

    try {
      const datos = await obtenerCompas(token, materiaId);

      setCompas((anteriores) => ({
        ...anteriores,
        [materiaId]: datos,
      }));
    } catch (error) {
      setError(error.message);
    } finally {
      setCargandoCompasId(null);
    }
  };

  const totalCreditos = inscripciones.reduce(
    (total, inscripcion) => total + inscripcion.creditos,
    0
  );

  if (cargando) {
    return <p>Cargando inscripciones...</p>;
  }

  return (
    <section>
      <h2>Mis inscripciones</h2>

      <div className="resumen-inscripciones">
        <strong>
          Materias inscritas: {inscripciones.length} / 3
        </strong>

        <span>
          <strong>Créditos:</strong> {totalCreditos} / 9
        </span>
      </div>

      {error && <p className="error">{error}</p>}

      {mensaje && (
        <p className="mensaje-exito">{mensaje}</p>
      )}

      {inscripciones.length === 0 ? (
        <p>Todavía no tienes materias inscritas.</p>
      ) : (
        <div className="materias-grid">
          {inscripciones.map((inscripcion) => (
            <article
              key={inscripcion.id}
              className="materia-card"
            >
              <h3>{inscripcion.materia}</h3>

              <p>
                <strong>Profesor:</strong> {inscripcion.profesor}
              </p>

              <p>
                <strong>Créditos:</strong> {inscripcion.creditos}
              </p>

              <div className="acciones-inscripcion">
                <button
                  type="button"
                  onClick={() =>
                    manejarCompas(inscripcion.materiaId)
                  }
                  disabled={cargandoCompasId !== null}
                >
                  {cargandoCompasId === inscripcion.materiaId
                    ? 'Consultando...'
                    : 'Ver compañeros'}
                </button>

                <button
                  type="button"
                  className="boton-cancelar"
                  onClick={() => manejarCancelacion(inscripcion)}
                  disabled={procesandoId !== null}
                >
                  {procesandoId === inscripcion.id
                    ? 'Cancelando...'
                    : 'Cancelar inscripción'}
                </button>
              </div>

              {compas[inscripcion.materiaId] && (
                <div className="lista-compas">
                  <h4>Compañeros de esta materia</h4>

                  {compas[inscripcion.materiaId].length === 0 ? (
                    <p>No hay otros estudiantes inscritos.</p>
                  ) : (
                    <ul>
                      {compas[inscripcion.materiaId].map(
                        (compa, indice) => (
                          <li key={indice}>
                            {compa.nombre}
                          </li>
                        )
                      )}
                    </ul>
                  )}
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

export default MisInscripciones;
