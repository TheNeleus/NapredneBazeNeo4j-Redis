import { useState } from 'react';
import { addFriendByEmail, updateUserProfile } from '../api/userService';
import './UserSettingsModal.css';

interface Props {
  user: any;
  onClose: () => void;
  onUpdateUser: (updatedUser: any) => void;
  initialTab?: 'profile' | 'friends';
}

const UserSettingsModal = ({ user, onClose, onUpdateUser, initialTab = 'profile' }: Props) => {
  const [activeTab, setActiveTab] = useState<'profile' | 'friends'>(initialTab);
  
  const [name, setName] = useState(user.name || '');
  const [bio, setBio] = useState(user.bio || ''); 
  const [interests, setInterests] = useState<string[]>(user.interests || []);
  const [isSaving, setIsSaving] = useState(false);

  const [friendEmail, setFriendEmail] = useState('');
  const [friendStatus, setFriendStatus] = useState('');

  const availableInterests = ['Tech', 'Sport', 'Music', 'Art', 'Travel', 'Food', 'Gaming'];

  const toggleInterest = (interest: string) => {
    setInterests(prev => 
      prev.includes(interest) ? prev.filter(i => i !== interest) : [...prev, interest]
    );
  };

  const handleSaveProfile = async () => {
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
      
      onUpdateUser(updatedUser);
      onClose();
      alert("Profile updated successfully!");
    } catch (err) {
      console.error(err);
      alert("Failed to update profile.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleAddFriend = async () => {
    if(!friendEmail) return;
    try {
        setFriendStatus('Adding...');
        await addFriendByEmail(user.id, friendEmail);
        setFriendStatus('✅ Friend added successfully!');
        setFriendEmail('');
    } catch (error: any) {
        if(error.response?.status === 404) setFriendStatus('❌ User not found.');
        else if(error.response?.status === 400) setFriendStatus(`❌ ${error.response.data}`);
        else setFriendStatus('❌ Error adding friend.');
    }
  };

  return (
    <div className="settings-overlay">
      <div className="settings-modal">
        <button className="close-btn" onClick={onClose}>✕</button>
        
        <h2>{activeTab === 'profile' ? 'Edit Profile' : 'Add Friend'}</h2>

        <div className="settings-tabs">
            <button 
                className={activeTab === 'profile' ? 'active' : ''} 
                onClick={() => setActiveTab('profile')}>
                Profile
            </button>
            <button 
                className={activeTab === 'friends' ? 'active' : ''} 
                onClick={() => setActiveTab('friends')}>
                Add Friends
            </button>
        </div>

        <div className="settings-content">
            {activeTab === 'profile' ? (
                <div className="profile-form">
                    <label>Full Name</label>
                    <input value={name} onChange={e => setName(e.target.value)} />

                    <label>Bio</label>
                    <textarea 
                        value={bio} 
                        onChange={e => setBio(e.target.value)} 
                        rows={3} 
                        placeholder="Tell us about yourself..."
                    />

                    <label>Interests</label>
                    <div className="interests-grid">
                        {availableInterests.map(tag => (
                            <div 
                                key={tag} 
                                className={`interest-tag ${interests.includes(tag) ? 'selected' : ''}`}
                                onClick={() => toggleInterest(tag)}
                            >
                                {tag}
                            </div>
                        ))}
                    </div>

                    <button onClick={handleSaveProfile} disabled={isSaving} className="save-btn">
                        {isSaving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            ) : (
                <div className="friends-form">
                    <p>Enter email to add a friend:</p>
                    <div className="add-friend-box">
                        <input 
                            type="email" 
                            placeholder="friend@email.com" 
                            value={friendEmail}
                            onChange={e => setFriendEmail(e.target.value)}
                        />
                        <button onClick={handleAddFriend} className="add-btn">Add</button>
                    </div>
                    {friendStatus && <p className="status-msg">{friendStatus}</p>}
                </div>
            )}
        </div>
      </div>
    </div>
  );
};

export default UserSettingsModal;