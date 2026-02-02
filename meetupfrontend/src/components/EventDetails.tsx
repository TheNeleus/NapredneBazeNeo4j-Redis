import type { MeetupEvent } from '../models/Event';
import ChatBox from './ChatBox';
import './EventDetails.css'; 

interface Props {
  event: MeetupEvent;
  currentUser: any;
  onClose: () => void;
}

const EventDetails = ({ event, currentUser, onClose }: Props) => {
  
  const isAttending = event.attendees && Array.isArray(event.attendees) 
    ? event.attendees.some((a: any) => {
        const attendeeId = typeof a === 'object' && a !== null ? a.id : a;
        return String(attendeeId).trim() === String(currentUser.id).trim();
      })
    : false;

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('sr-RS', {
      day: 'numeric', month: 'long', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  };

  return (
    <div className="details-overlay">
      <div className="details-content">
        <button onClick={onClose} className="close-btn">✖</button>

        <h2>{event.title}</h2>
        <span className="event-category">{event.category || 'Event'}</span>
        <p className="event-description">{event.description}</p>
        
        <div className="event-time">
          <strong>🕒 Time:</strong> {formatDate(event.date)}
        </div>
        
        <hr className="details-divider"/>

        <div className="chat-section-wrapper">
            {isAttending ? (
               <ChatBox eventId={event.id} currentUser={currentUser} />
            ) : (
               <div className="chat-locked-message">
                  <h3>Chat is locked</h3>
                  <p>Join this event on the dashboard to access the live chat!</p>
               </div>
            )}
        </div>
      </div>
    </div>
  );
};

export default EventDetails;