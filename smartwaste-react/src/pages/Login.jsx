import { useState } from 'react';
import { authAPI } from '../services/api'; // Ensure this path matches your project
import './Login.css';

export default function Login({ onLoginSuccess, onShowRegister }) {
  const [cnic, setCnic] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const result = await authAPI.login(cnic, password);

      if (result.success) {
        // Support all roles: Government (1), Citizen (2), and Operator (3)
        // Normalize 'roleName' to 'role' for App.jsx routing
        const roleName = result.user.roleName || result.user.role;
        const roleId = result.user.roleID || result.user.roleId;

        if (roleId === 1 || roleId === 2 || roleId === 3 ||
            roleName === 'Government' || roleName === 'Citizen' || roleName === 'Operator') {
          const userData = { ...result.user, role: roleName }; // Ensure 'role' property exists for App.jsx
          onLoginSuccess(userData);
        } else {
          setError(`Portal for ${roleName} (Role ${roleId}) is not yet implemented.`);
        }
      } else {
        setError(result.message || 'Invalid credentials');
      }
    } catch (err) {
      setError('Connection error. Please make sure the Web API is running.');
      console.error('Login error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-box">
        <h1>Smart Waste Management</h1>
        <h2>Login to your account</h2>

        <form onSubmit={handleLogin}>
          <div className="form-group">
            <label>CNIC</label>
            <input
              type="text"
              placeholder="xxxxx-xxxxxxx-x"
              value={cnic}
              onChange={(e) => setCnic(e.target.value)}
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label>Password</label>
            <input
              type="password"
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={loading}
            />
          </div>

          <button type="submit" className="login-btn" disabled={loading}>
            {loading ? 'Logging in...' : 'Login'}
          </button>

          {error && <div className="error-message">{error}</div>}
        </form>

        {/* --- UPDATED TEST CREDENTIALS --- */}
        <div className="test-credentials">
          <h3>Test Credentials:</h3>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px', textAlign: 'left' }}>
            <div>
              <p className="role-badge citizen">👤 Citizen</p>
              <p><strong>CNIC:</strong> 35201-0000001-0</p>
              <p><strong>Pass:</strong> password1</p>
            </div>
            <div>
              <p className="role-badge government">🏛️ Government</p>
              <p><strong>CNIC:</strong> (check DB)</p>
              <p><strong>Pass:</strong> (check DB)</p>
            </div>
            <div>
              <p className="role-badge operator">🚛 Operator</p>
              <p><strong>CNIC:</strong> 42000-0300001-0</p>
              <p><strong>Pass:</strong> oppass1</p>
            </div>
          </div>
          <p className="note" style={{marginTop: '10px'}}>
            <em>Note: Check your SQL database if these IDs don't match your generated data.</em>
          </p>
        </div>

        <div className="register-link">
          Don't have an account?{' '}
          <button onClick={onShowRegister} className="link-btn">
            Register here
          </button>
        </div>
      </div>
    </div>
  );
}