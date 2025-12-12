import { useState, useEffect } from 'react';
import { citizenAPI } from '../services/api';
import './Register.css';

export default function Register({ onRegisterSuccess, onBackToLogin }) {
  const [formData, setFormData] = useState({
    cnic: '',
    fullName: '',
    phoneNumber: '',
    areaID: '',
    address: '',
    password: '',
    confirmPassword: ''
  });

  const [areas, setAreas] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadAreas();
  }, []);

  const loadAreas = async () => {
    try {
      const data = await citizenAPI.getAreas();
      setAreas(data);
    } catch (err) {
      console.error('Failed to load areas:', err);
    }
  };

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const validateCNIC = (cnic) => {
    // CNIC format: XXXXX-XXXXXXX-X
    const cnicRegex = /^\d{5}-\d{7}-\d{1}$/;
    return cnicRegex.test(cnic);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Validation
    if (!validateCNIC(formData.cnic)) {
      setError('Invalid CNIC format. Use: XXXXX-XXXXXXX-X');
      return;
    }

    if (formData.password.length < 6) {
      setError('Password must be at least 6 characters');
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (!formData.areaID) {
      setError('Please select an area');
      return;
    }

    setLoading(true);

    try {
      const result = await citizenAPI.register({
        cnic: formData.cnic,
        fullName: formData.fullName,
        phoneNumber: formData.phoneNumber,
        areaID: parseInt(formData.areaID),
        address: formData.address,
        password: formData.password
      });

      if (result.success) {
        alert('Registration successful! You can now login.');
        onBackToLogin();
      } else {
        setError(result.message || 'Registration failed');
      }
    } catch (err) {
      setError('Registration error: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-container">
      <div className="register-card">
        <h1>SmartWaste Registration</h1>
        <p className="subtitle">Create your citizen account</p>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>CNIC</label>
            <input
              type="text"
              name="cnic"
              placeholder="35201-0000001-0"
              value={formData.cnic}
              onChange={handleChange}
              required
            />
            <small>Format: XXXXX-XXXXXXX-X</small>
          </div>

          <div className="form-group">
            <label>Full Name</label>
            <input
              type="text"
              name="fullName"
              placeholder="John Doe"
              value={formData.fullName}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label>Phone Number</label>
            <input
              type="tel"
              name="phoneNumber"
              placeholder="0300-1234567"
              value={formData.phoneNumber}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label>Area</label>
            <select
              name="areaID"
              value={formData.areaID}
              onChange={handleChange}
              required
            >
              <option value="">Select your area...</option>
              {areas.map(area => (
                <option key={area.areaID} value={area.areaID}>
                  {area.areaName}, {area.city}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Address</label>
            <textarea
              name="address"
              placeholder="House # 123, Street 45..."
              value={formData.address}
              onChange={handleChange}
              rows="2"
              required
            />
          </div>

          <div className="form-group">
            <label>Password</label>
            <input
              type="password"
              name="password"
              placeholder="Minimum 6 characters"
              value={formData.password}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label>Confirm Password</label>
            <input
              type="password"
              name="confirmPassword"
              placeholder="Re-enter password"
              value={formData.confirmPassword}
              onChange={handleChange}
              required
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <button type="submit" className="register-btn" disabled={loading}>
            {loading ? 'Registering...' : 'Register'}
          </button>
        </form>

        <div className="back-to-login">
          Already have an account?{' '}
          <button onClick={onBackToLogin} className="link-btn">
            Login here
          </button>
        </div>
      </div>
    </div>
  );
}
