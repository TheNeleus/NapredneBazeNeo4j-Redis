import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { register } from '../api/authService';
import './Register.css'; 

const Register = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    bio: ''
  });
  
  const [selectedInterests, setSelectedInterests] = useState<string[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const availableInterests = [
    'Tech', 
    'Sport', 
    'Music', 
    'Art', 
    'Travel', 
    'Food', 
    'Gaming', 
    'Social'
  ];

  const handleCheckboxChange = (interest: string) => {
    if (selectedInterests.includes(interest)) {
      setSelectedInterests(prev => prev.filter(i => i !== interest));
    } else {
      setSelectedInterests(prev => [...prev, interest]);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      await register({
        id: '',
        name: formData.name,
        email: formData.email,
        interests: selectedInterests,
        role: 'User',
        bio: formData.bio
      });

      alert('Registration successful! Please log in.');
      navigate('/'); 
    } catch (err) {
      console.error(err);
      setError('Registration failed. Email might be taken.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-container">
      <h1>Create Account</h1>
      
      <form onSubmit={handleRegister} className="register-form">
        <input
          type="text"
          placeholder="Full Name"
          value={formData.name}
          onChange={(e) => setFormData({...formData, name: e.target.value})}
          required
          className="register-input"
        />

        <input
          type="email"
          placeholder="Email Address"
          value={formData.email}
          onChange={(e) => setFormData({...formData, email: e.target.value})}
          required
          className="register-input"
        />

        <textarea
          placeholder="Tell us a bit about yourself (Bio)..."
          value={formData.bio}
          onChange={(e) => setFormData({...formData, bio: e.target.value})}
          rows={3}
          className="register-textarea"
        />

        <div className="interests-section">
          <p className="interests-title">Your Interests:</p>
          <div className="checkbox-group">
            {availableInterests.map(interest => (
              <label key={interest} className="checkbox-label">
                <input 
                  type="checkbox" 
                  checked={selectedInterests.includes(interest)}
                  onChange={() => handleCheckboxChange(interest)}
                />
                {interest}
              </label>
            ))}
          </div>
        </div>

        <button type="submit" disabled={loading} className="register-button">
          {loading ? 'Creating...' : 'Register'}
        </button>
      </form>

      {error && <p className="error-msg">{error}</p>}

      <div className="login-link">
        Already have an account? <Link to="/">Log in here</Link>
      </div>
    </div>
  );
};

export default Register;