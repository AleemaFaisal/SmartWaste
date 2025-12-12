import { useState } from 'react';
import { authAPI } from '../services/api';
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
        if (result.user.roleID === 2 || result.user.roleID === 1) {
          // Citizen role - proceed
          onLoginSuccess(result.user);
        } else {
          setError(`${result.user.roleName} portal is not yet implemented. Only Citizen portal is available.`);
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

        <div className="test-credentials">
          <h3>Test Credentials (Citizen Portal Only):</h3>
          <p><strong>CNIC:</strong> 35201-0000001-0</p>
          <p><strong>Password:</strong> password1</p>
          <p className="note">Note: Operator and Government portals are not yet implemented.</p>
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
