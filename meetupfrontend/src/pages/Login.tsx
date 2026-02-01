import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { login } from '../api/authService';
import './Login.css';

const Login = () => {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const data = await login(email);
      localStorage.setItem('meetup_token', data.token);
      localStorage.setItem('meetup_user', JSON.stringify(data.user));
      navigate('/home');
    } catch (err) {
      console.error(err);
      setError('Login failed. Check if email exists in database.');
    }
  };

  return (
    <div className="login-container">
      <h1>Welcome Back</h1>
      <form onSubmit={handleLogin} className="login-form">
        <input
          type="email"
          placeholder="Enter your email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="login-input"
        />
        <button type="submit" className="login-button">
          Log In
        </button>
      </form>
      {error && <p className="error-msg">{error}</p>}
      
      <div className="register-link">
        Don't have an account? <Link to="/register">Register here</Link>
      </div>
    </div>
  );
};

export default Login;