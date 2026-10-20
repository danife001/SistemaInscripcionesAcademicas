
import { useEffect, useState } from 'react';

import Administracion from './pages/Administracion';
import AdministracionMaterias from './pages/AdministracionMaterias';
import { obtenerSesion } from './services/authService';
import Login from './pages/Login';
import Registro from './pages/Registro';
import Materias from './pages/Materias';
import MisInscripciones from './pages/MisInscripciones';
import RegistrosEstudiantes from './pages/RegistrosEstudiantes';
import MiPerfil from './pages/MiPerfil';

function App() {
  const [token, setToken] = useState(() =>
    sessionStorage.getItem('token')
  );

  const [sesion, setSesion] = useState(null);
  const [errorSesion, setErrorSesion] = useState('');
  useEffect(() => {
    let vigente = true;
    if (token) obtenerSesion(token).then(datos => {
      if (vigente) setSesion(datos);
    }).catch(error => { if (vigente) setErrorSesion(error.message); });
    return () => { vigente = false; };
  }, [token]);

  const [vistaAuth, setVistaAuth] = useState('login');
  const [pagina, setPagina] = useState('materias');
  const [paginaAdmin, setPaginaAdmin] = useState('estudiantes');

  const manejarLogin = (nuevoToken) => {
    setSesion(null);
    setErrorSesion('');
    sessionStorage.setItem('token', nuevoToken);
    setToken(nuevoToken);
    setPagina('materias');
    setPaginaAdmin('estudiantes');
  };

  const cerrarSesion = () => {
    setSesion(null);
    setErrorSesion('');
    sessionStorage.removeItem('token');
    setToken(null);
    setVistaAuth('login');
    setPagina('materias');
    setPaginaAdmin('estudiantes');
  };

  if (!token) {
    if (vistaAuth === 'registro') {
      return (
        <Registro
          onVolverLogin={() => setVistaAuth('login')}
        />
      );
    }

    return (
      <Login
        onLogin={manejarLogin}
        onMostrarRegistro={() => setVistaAuth('registro')}
      />
    );
  }

  if (!sesion) return <main className="inicio-container">
    <p role="status">{errorSesion || 'Verificando sesión...'}</p>
    <button onClick={cerrarSesion}>Volver al inicio de sesión</button>
  </main>;

  if (sesion.roles.includes('Administrador')) return <main className="inicio-container">
    <header className="app-header"><div><h1>Administración académica</h1>
      <p>{sesion.nombre}</p></div><button onClick={cerrarSesion}>Cerrar sesión</button></header>
    <nav className="app-nav" aria-label="Administración">
      <button className={paginaAdmin === 'estudiantes' ? 'activo' : ''}
        aria-current={paginaAdmin === 'estudiantes' ? 'page' : undefined}
        onClick={() => setPaginaAdmin('estudiantes')}>Estudiantes</button>
      <button className={paginaAdmin === 'materias' ? 'activo' : ''}
        aria-current={paginaAdmin === 'materias' ? 'page' : undefined}
        onClick={() => setPaginaAdmin('materias')}>Materias</button>
    </nav>
    {paginaAdmin === 'estudiantes' ? <Administracion token={token} /> : <AdministracionMaterias token={token} />}
  </main>;

  return (
    <main className="inicio-container">
      <header className="app-header">
        <div>
          <h1>Sistema de Inscripciones Académicas</h1>
          <p>Portal del estudiante</p>
        </div>

        <button onClick={cerrarSesion}>
          Cerrar sesión
        </button>
      </header>

      <nav className="app-nav">
        <button
          className={pagina === 'materias' ? 'activo' : ''}
          onClick={() => setPagina('materias')}
        >
          Materias disponibles
        </button>

        <button
          className={
            pagina === 'inscripciones' ? 'activo' : ''
          }
          onClick={() => setPagina('inscripciones')}
        >
          Mis inscripciones
        </button>

        <button
          className={pagina === 'registros' ? 'activo' : ''}
          onClick={() => setPagina('registros')}
        >
          Registros de estudiantes
        </button>
        <button
          className={pagina === 'perfil' ? 'activo' : ''}
          onClick={() => setPagina('perfil')}
        >
          Mi perfil
        </button>
      </nav>

      {pagina === 'materias' && (
        <Materias token={token} />
      )}

      {pagina === 'inscripciones' && (
        <MisInscripciones token={token} />
      )}

      {pagina === 'registros' && (
        <RegistrosEstudiantes token={token} />
      )}

      {pagina === 'perfil' && (
        <MiPerfil token={token} />
      )}
    </main>
  );
}

export default App;
