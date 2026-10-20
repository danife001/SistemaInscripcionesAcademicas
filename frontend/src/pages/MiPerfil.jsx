import { useEffect, useState } from 'react';
import { actualizarMiPerfil, obtenerMiPerfil } from '../services/estudianteService';

function MiPerfil({ token }) {
  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [cargando, setCargando] = useState(true);
  const [guardando, setGuardando] = useState(false);
  const [perfilCargado, setPerfilCargado] = useState(false);
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');

  useEffect(() => {
    let vigente = true;

    async function cargarPerfil() {
      try {
        const perfil = await obtenerMiPerfil(token);
        if (vigente) {
          setNombre(perfil.nombre);
          setCorreo(perfil.correo);
          setPerfilCargado(true);
        }
      } catch (error) {
        if (vigente) setError(error.message);
      } finally {
        if (vigente) setCargando(false);
      }
    }

    cargarPerfil();
    return () => { vigente = false; };
  }, [token]);

  async function manejarSubmit(evento) {
    evento.preventDefault();
    setError('');
    setMensaje('');

    if (!nombre.trim()) {
      setError('El nombre es obligatorio.');
      return;
    }

    setGuardando(true);
    try {
      const perfil = await actualizarMiPerfil(token, {
        nombre: nombre.trim(),
        correo: correo.trim(),
      });
      setNombre(perfil.nombre);
      setCorreo(perfil.correo);
      setMensaje('Tus datos se actualizaron correctamente.');
    } catch (error) {
      setError(error.message);
    } finally {
      setGuardando(false);
    }
  }

  if (cargando) return <p>Cargando perfil...</p>;
  if (!perfilCargado) return <p className="error" role="alert">{error}</p>;

  return (
    <section>
      <h2>Mi perfil</h2>
      <p>Actualiza tus datos. Usa el correo guardado para iniciar sesión.</p>

      <form className="login-form" onSubmit={manejarSubmit}>
        <label htmlFor="perfil-nombre">Nombre completo</label>
        <input
          id="perfil-nombre"
          type="text"
          autoComplete="name"
          value={nombre}
          onChange={(evento) => {
            setNombre(evento.target.value);
            setMensaje('');
          }}
          maxLength={200}
          disabled={guardando}
          required
        />

        <label htmlFor="perfil-correo">Correo electrónico</label>
        <input
          id="perfil-correo"
          type="email"
          autoComplete="email"
          value={correo}
          onChange={(evento) => {
            setCorreo(evento.target.value);
            setMensaje('');
          }}
          maxLength={256}
          disabled={guardando}
          required
        />

        {error && <p className="error" role="alert">{error}</p>}
        {mensaje && <p className="mensaje-exito" role="status">{mensaje}</p>}

        <button type="submit" disabled={guardando}>
          {guardando ? 'Guardando...' : 'Guardar cambios'}
        </button>
      </form>
    </section>
  );
}

export default MiPerfil;
