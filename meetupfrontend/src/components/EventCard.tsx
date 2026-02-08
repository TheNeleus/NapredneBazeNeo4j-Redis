import { useState } from 'react';
import type { MeetupEvent } from '../models/Event';
import AttendeesModal from './AttendeesModal';
import './EventCard.css';

interface Props {
  event: MeetupEvent;
  currentUser: any; 
  onClick: () => void;
  onAttend: (isAttending: boolean) => void;
  onDelete?: (eventId: string) => void;
  onEdit?: (event: MeetupEvent) => void; 
}

const EventCard = ({ event, currentUser, onClick, onAttend, onDelete, onEdit }: Props) => {
  
  const [showAttendees, setShowAttendees] = useState(false);

  const isCreator = currentUser?.id === (event as any).creatorId;
  const isAdmin = currentUser?.role === 'Admin';
  
  const canModify = isCreator || isAdmin;

  const isAttending = event.attendees && Array.isArray(event.attendees) 
    ? event.attendees.some((a: any) => {
        const attendeeId = typeof a === 'object' && a !== null ? a.id : a;
        return String(attendeeId).trim() === String(currentUser?.id).trim();
      })
    : false;

  const renderAdminButton = () => {
    if (!isAdmin) return null;
    return (
      <button 
        className="admin-users-btn"
        onClick={(e) => {
            e.stopPropagation();
            setShowAttendees(true);
        }}
        title="Manage Attendees"
      >
        👥
      </button>
    );
  };

  return (
    <>
      <div className="event-card" onClick={onClick}>

        {renderAdminButton()}

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
          
          <div className="meta-row">
            <p className="event-date">
              📅 {new Date(event.date).toLocaleString('sr-RS', {
                  day: 'numeric',
                  month: 'numeric',
                  year: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit'
              })}
            </p>
            
            {event.distanceKm != null && (
              <p className="event-distance">📏 {Math.round(event.distanceKm * 10) / 10} km</p>
            )}
            {event.friendsGoing != null && event.friendsGoing > 0 && (
              <p className="event-friends">👥 {event.friendsGoing} friends</p>
            )}
          </div>

          <p className="event-desc-short">
            {event.description.substring(0, 60)}...
          </p>
          
          <button 
            className={isAttending ? "leave-btn-small" : "attend-btn-small"}
            onClick={(e) => {
              e.stopPropagation();
              onAttend(isAttending); 
            }}
          >
            {isAttending ? "✕ Leave Event" : "✓ Attend"}
          </button>
        </div>
      </div>

      {showAttendees && (
        <AttendeesModal 
            eventId={event.id} 
            onClose={() => setShowAttendees(false)} 
        />
      )}
    </>
  );
};

export default EventCard;