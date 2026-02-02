import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { addFriendByEmail } from '../api/userService';
import './AddFriend.css'; // Koristićemo zajednički CSS fajl za lep izgled

const AddFriend = () => {
  const navigate = useNavigate();
  const [user, setUser] = useState<any>(null);
  const [friendEmail, setFriendEmail] = useState('');
  const [friendStatus, setFriendStatus] = useState('');

  useEffect(() => {
    const storedUser = sessionStorage.getItem('meetup_user');
    if (!storedUser) {
      navigate('/');
      return;
    }
    setUser(JSON.parse(storedUser));
  }, [navigate]);

  const handleAddFriend = async () => {
    if(!friendEmail || !user) return;
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
    <div className="add-friend-container">
      <div className="add-friend-card">
        <button className="back-link" onClick={() => navigate('/home')}>← Back to Home</button>
        
        <h2 className="af-title">Add Friend</h2>
        <p className="af-subtitle">Connect with your friends to see their events.</p>

        <div className="af-input-group">
            <input 
                type="email" 
                placeholder="Enter friend's email..." 
                value={friendEmail}
                onChange={e => setFriendEmail(e.target.value)}
                className="af-input"
            />
            <button onClick={handleAddFriend} className="af-btn">Add</button>
        </div>
            
        {friendStatus && <p className="af-status">{friendStatus}</p>}
      </div>
    </div>
  );
};

export default AddFriend;