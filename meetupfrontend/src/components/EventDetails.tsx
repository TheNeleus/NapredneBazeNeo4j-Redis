import { attendEvent } from '../api/eventService';
import type { MeetupEvent } from '../models/Event';
import { useState } from 'react';
import ChatBox from './ChatBox';
import './EventDetails.css'; 

interface Props {
  event: MeetupEvent;
  currentUser: any;
  onClose: () => void;
}

const EventDetails = ({ event, currentUser, onClose }: Props) => {
  const [loading, setLoading] = useState(false);

  const handleAttend = async () => {
    try {
      setLoading(true);
      await attendEvent(event.id);
      alert("You are now attending this event! 🎉");
      // onClose(); // Zakomentarisano da ne izadje odmah
    } catch (error) {
      console.error(error);
      alert("Failed to join event.");
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <div className="details-overlay">
      <div className="details-content">
        <button onClick={onClose} className="close-btn">✖</button>

        <h2 style={{ marginTop: 0 }}>{event.title}</h2>
        <span className="event-category">
          {event.category || 'Event'}
        </span>

        <p className="event-description">{event.description}</p>
        
        <div className="event-time">
          <strong>🕒 Time:</strong> {formatDate(event.date)}
        </div>

        <div className="details-actions">
          <button onClick={handleAttend} disabled={loading} className="action-btn btn-join">
            {loading ? 'Joining...' : 'I\'m Going! 🚀'}
          </button>
          
          <button onClick={onClose} className="action-btn btn-close">
            Close
          </button>
        </div>
        
        <ChatBox eventId={event.id} currentUser={currentUser} />

      </div>
    </div>
  );
};

export default EventDetails;