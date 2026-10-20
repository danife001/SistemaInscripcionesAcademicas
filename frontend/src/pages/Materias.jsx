
import { useEffect, useState } from 'react';
import { obtenerMaterias } from '../services/materiaService';
import {
  obtenerInscripciones,
  inscribirMateria,
} from '../services/inscripcionService';

function Materias({ token }) {
  const [materias, setMaterias] = useState([]);
  const [inscripciones, setInscripciones] = useState([]);
  const [cargando, setCargando] = useState(true);
  const [inscribiendoId, setInscribiendoId] = useState(null);
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');

  useEffect(() => {
    async function cargarDatos() {
      try {
        const [datosMaterias, datosInscripciones] =
          await Promise.all([
            obtenerMaterias(token),
            obtenerInscripciones(token),
          ]);

        setMaterias(datosMaterias);
        setInscripciones(datosInscripciones);
      } catch (error) {
        setError(error.message);
      } finally {
        setCargando(false);
      }
    }

    cargarDatos();
  }, [token]);

  const manejarInscripcion = async (materia) => {
    setError('');
    setMensaje('');
    setInscribiendoId(materia.id);

    try {
      await inscribirMateria(token, materia.id);

      const nuevasInscripciones =
        await obtenerInscripciones(token);

      setInscripciones(nuevasInscripciones);

      setMensaje(
        `Te inscribiste correctamente en ${materia.nombre}.`
      );
    } catch (error) {
      setError(error.message);
    } finally {
      setInscribiendoId(null);
    }
  };

  const estaInscrita = (materiaId) =>
    inscripciones.some(
      (inscripcion) => inscripcion.materiaId === materiaId
    );

  const creditosInscritos = inscripciones.reduce(
    (total, inscripcion) => total + inscripcion.creditos,
    0
  );

  if (cargando) {
    return <p>Cargando materias...</p>;
  }

  return (
    <section>
      <h2>Materias disponibles</h2>

      <p>
        Puedes inscribirte en un máximo de 3 materias.
      </p>

      <div className="resumen-inscripciones">
        <strong>Materias inscritas:</strong> {inscripciones.length} / 3
        <span>
          <strong>Créditos:</strong> {creditosInscritos} / 9
        </span>
      </div>

      {error && <p className="error">{error}</p>}

      {mensaje && (
        <p className="mensaje-exito">{mensaje}</p>
      )}

      {materias.length === 0 ? (
        <p>No hay materias disponibles.</p>
      ) : (
        <div className="materias-grid">
          {materias.map((materia) => {
            const inscrita = estaInscrita(materia.id);
            const procesando = inscribiendoId !== null;

            return (
              <article
                className="materia-card"
                key={materia.id}
              >
                <h3>{materia.nombre}</h3>

                <p>
                  <strong>Profesor:</strong> {materia.profesor}
                </p>

                <p>
                  <strong>Créditos:</strong> {materia.creditos}
                </p>

                <button
                  type="button"
                  disabled={inscrita || procesando}
                  onClick={() => manejarInscripcion(materia)}
                >
                  {inscrita
                    ? 'Ya inscrita'
                    : inscribiendoId === materia.id
                    ? 'Inscribiendo...'
                    : 'Inscribirme'}
                </button>
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}

export default Materias;
