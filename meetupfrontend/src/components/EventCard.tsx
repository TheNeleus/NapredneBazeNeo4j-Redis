import type { MeetupEvent } from '../models/Event';
import './EventCard.css';

interface Props {
  event: MeetupEvent;
  currentUser: any; 
  onClick: () => void;
  onAttend: (e: any) => void;
  onDelete?: (eventId: string) => void;
  onEdit?: (event: MeetupEvent) => void; 
}

const EventCard = ({ event, currentUser, onClick, onAttend, onDelete, onEdit }: Props) => {
  const isCreator = currentUser?.id === (event as any).creatorId;
  const isAdmin = currentUser?.role === 'Admin';
  const canModify = isCreator || isAdmin;

  return (
    <div className="event-card" onClick={onClick}>
      
      {canModify && (
        <>
            {onEdit && (
                <button 
                    className="edit-card-btn"
                    onClick={(e) => {
                        e.stopPropagation(); 
                        onEdit(event);
                    }}
                    title="Edit Event"
                >
                    ✏️
                </button>
            )}

            {onDelete && (
                <button 
                    className="delete-card-btn"
                    onClick={(e) => {
                        e.stopPropagation();
                        if(window.confirm(`Delete "${event.title}"?`)) {
                            onDelete(event.id);
                        }
                    }}
                    title="Delete Event"
                >
                    🗑️
                </button>
            )}
        </>
      )}

      <div className="card-image-placeholder">
        {event.category || 'Event'}
      </div>
      
      <div className="card-content">
        <h3 className="card-title">{event.title}</h3>
        <p className="event-date">
          📅 {new Date(event.date).toLocaleDateString('sr-RS')}
        </p>
        <p className="event-desc-short">
          {event.description.substring(0, 60)}...
        </p>
        
        <button 
          className="attend-btn-small"
          onClick={(e) => {
            e.stopPropagation();
            onAttend(e);
          }}
        >
          Attend
        </button>
      </div>
    </div>
  );
};

export default EventCard;