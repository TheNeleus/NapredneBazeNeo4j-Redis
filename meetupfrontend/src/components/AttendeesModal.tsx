import { useEffect, useState } from 'react';
import { getEventAttendees, kickUser } from '../api/eventService';
import './AttendeesModal.css';

interface Props {
  eventId: string;
  onClose: () => void;
}

const AttendeesModal = ({ eventId, onClose }: Props) => {
  const [attendees, setAttendees] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadAttendees();
  }, [eventId]);

  const loadAttendees = async () => {
    try {
      const data = await getEventAttendees(eventId);
      setAttendees(data);
    } catch (error) {
      console.error("Failed to load attendees", error);
    } finally {
      setLoading(false);
    }
  };

  const handleKick = async (userId: string, userName: string) => {
    if (window.confirm(`Are you sure you want to kick ${userName}?`)) {
      try {
        await kickUser(eventId, userId);
        setAttendees(prev => prev.filter(u => u.id !== userId));
      } catch (error) {
        alert("Failed to kick user.");
      }
    }
  };

  return (
    <div className="attendees-overlay">
      <div className="attendees-modal">
        <div className="attendees-header">
            <h3>Event Attendees</h3>
            <button onClick={onClose} className="close-attendees-btn">✕</button>
        </div>
        
        {loading ? (
            <p>Loading...</p>
        ) : attendees.length === 0 ? (
            <p>No attendees yet.</p>
        ) : (
            <ul className="attendees-list">
                {attendees.map(user => (
                    <li key={user.id} className="attendee-item">
                        <div className="attendee-info">
                            <span className="attendee-name">{user.name}</span>
                            <span className="attendee-email">{user.email}</span>
                        </div>
                        <button 
                            className="kick-btn"
                            onClick={() => handleKick(user.id, user.name)}
                        >
                            Kick
                        </button>
                    </li>
                ))}
            </ul>
        )}
      </div>
    </div>
  );
};

export default AttendeesModal;