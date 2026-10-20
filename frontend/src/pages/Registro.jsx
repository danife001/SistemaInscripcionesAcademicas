
import { useState } from 'react';
import { registrarEstudiante } from '../services/estudianteService';

function Registro({ onVolverLogin }) {
  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [password, setPassword] = useState('');
  const [confirmarPassword, setConfirmarPassword] = useState('');

  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState('');
  const [registrado, setRegistrado] = useState(false);

  const manejarSubmit = async (evento) => {
    evento.preventDefault();

    setError('');

    if (password !== confirmarPassword) {
      setError('Las contraseñas no coinciden.');
      return;
    }

    setCargando(true);

    try {
      await registrarEstudiante({
        nombre,
        correo,
        password,
      });

      setRegistrado(true);
    } catch (error) {
      setError(error.message);
    } finally {
      setCargando(false);
    }
  };

  if (registrado) {
    return (
      <main className="login-container">
        <div className="login-form">
          <h1>Registro exitoso</h1>

          <p>
            Tu cuenta fue creada correctamente.
            Ahora puedes iniciar sesión.
          </p>

          <button type="button" onClick={onVolverLogin}>
            Ir al inicio de sesión
          </button>
        </div>
      </main>
    );
  }

  return (
    <main className="login-container">
      <form className="login-form" onSubmit={manejarSubmit}>
        <h1>Crear cuenta</h1>

        <p>Sistema de Inscripciones Académicas</p>

        <label htmlFor="nombre">Nombre completo</label>
        <input
          id="nombre"
          type="text"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
        />

        <label htmlFor="correo">Correo electrónico</label>
        <input
          id="correo"
          type="email"
          value={correo}
          onChange={(e) => setCorreo(e.target.value)}
          required
        />

        <label htmlFor="password">Contraseña</label>
        <input
          id="password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="new-password"
          required
        />

        <label htmlFor="confirmarPassword">
          Confirmar contraseña
        </label>
        <input
          id="confirmarPassword"
          type="password"
          value={confirmarPassword}
          onChange={(e) =>
            setConfirmarPassword(e.target.value)
          }
          autoComplete="new-password"
          required
        />

        {error && <p className="error">{error}</p>}

        <button type="submit" disabled={cargando}>
          {cargando ? 'Registrando...' : 'Registrarme'}
        </button>

        <button
          type="button"
          className="boton-secundario"
          onClick={onVolverLogin}
        >
          Ya tengo una cuenta
        </button>
      </form>
    </main>
  );
}

export default Registro;
