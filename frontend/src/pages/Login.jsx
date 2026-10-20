
import { useState } from 'react';
import { iniciarSesion } from '../services/authService';

function Login({ onLogin, onMostrarRegistro }) {
  const [correo, setCorreo] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(false);

  const manejarSubmit = async (evento) => {
    evento.preventDefault();

    setError('');
    setCargando(true);

    try {
      const datos = await iniciarSesion(correo, password);

      onLogin(datos.accessToken);
    } catch (error) {
      setError(error.message);
    } finally {
      setCargando(false);
    }
  };

  return (
    <main className="login-container">
      <form className="login-form" onSubmit={manejarSubmit}>
        <h1>Iniciar sesión</h1>

        <p>
          Sistema de Inscripciones Académicas
        </p>

        <label htmlFor="correo">Correo electrónico</label>

        <input
          id="correo"
          type="email"
          value={correo}
          onChange={(e) => setCorreo(e.target.value)}
          placeholder="correo@ejemplo.com"
          autoComplete="email"
          required
        />

        <label htmlFor="password">Contraseña</label>

        <input
          id="password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />

        {error && <p className="error">{error}</p>}

        <button type="submit" disabled={cargando}>
          {cargando ? 'Ingresando...' : 'Ingresar'}
        </button>

        <button
  type="button"
  className="boton-secundario"
  onClick={onMostrarRegistro}
>
  Crear una cuenta
</button>
      </form>
    </main>
  );
}

export default Login;
