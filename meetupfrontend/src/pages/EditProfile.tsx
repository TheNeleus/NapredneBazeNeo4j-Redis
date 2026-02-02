import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { updateUserProfile } from '../api/userService';
import './EditProfile.css'; 

const EditProfile = () => {
  const navigate = useNavigate();
  const [user, setUser] = useState<any>(null);


  const [name, setName] = useState('');
  const [bio, setBio] = useState('');
  const [interests, setInterests] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const availableInterests = ['Tech', 'Sport', 'Music', 'Art', 'Travel', 'Food', 'Gaming'];

  useEffect(() => {
    const storedUser = sessionStorage.getItem('meetup_user');
    if (!storedUser) {
      navigate('/');
      return;
    }
    const parsedUser = JSON.parse(storedUser);
    setUser(parsedUser);
    setName(parsedUser.name || '');
    setBio(parsedUser.bio || '');
    setInterests(parsedUser.interests || []);
  }, [navigate]);

  const toggleInterest = (interest: string) => {
    setInterests(prev => 
      prev.includes(interest) ? prev.filter(i => i !== interest) : [...prev, interest]
    );
  };

  const handleSaveProfile = async () => {
    if (!user) return;
    setIsSaving(true);
    try {
      await updateUserProfile(user.id, { 
          id: user.id,
          name, 
          email: user.email, 
          bio, 
          interests 
      });
      
      const updatedUser = { ...user, name, bio, interests };
      sessionStorage.setItem('meetup_user', JSON.stringify(updatedUser));
      setUser(updatedUser);
      
      alert("Profile updated successfully!");
    } catch (err) {
      console.error(err);
      alert("Failed to update profile.");
    } finally {
      setIsSaving(false);
    }
  };

  if (!user) return <div>Loading...</div>;

  return (
    <div className="edit-profile-container">
      <div className="profile-card">
        <button className="ep-back-btn" onClick={() => navigate('/home')}>← Back to Home</button>

        <h2 className="ep-title">Edit Profile</h2>

        <div className="ep-form">
            <label>Full Name</label>
            <input 
                value={name} 
                onChange={e => setName(e.target.value)} 
                className="ep-input"
            />

            <label>Bio</label>
            <textarea 
                value={bio} 
                onChange={e => setBio(e.target.value)} 
                rows={4} 
                placeholder="Tell us about yourself..."
                className="ep-textarea"
            />

            <label>Interests</label>
            <div className="ep-tags-grid">
                {availableInterests.map(tag => (
                    <div 
                        key={tag} 
                        className={`ep-tag ${interests.includes(tag) ? 'selected' : ''}`}
                        onClick={() => toggleInterest(tag)}
                    >
                        {tag}
                    </div>
                ))}
            </div>

            <button onClick={handleSaveProfile} disabled={isSaving} className="ep-save-btn">
                {isSaving ? 'Saving...' : 'Save Changes'}
            </button>
        </div>
      </div>
    </div>
  );
};

export default EditProfile;