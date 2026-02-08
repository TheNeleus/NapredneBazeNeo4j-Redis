import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getEvents, getFriendsEvents, getRecommendedEvents, attendEvent, deleteEvent, leaveEvent } from '../api/eventService';
import type { MeetupEvent } from '../models/Event';
import EventDetails from '../components/EventDetails';
import EventCard from '../components/EventCard';
import Map from '../components/Map';
import ChatBox from '../components/ChatBox';
import './Home.css';

const Home = () => {
  const navigate = useNavigate();
  
  const [user, setUser] = useState<any>(null);
  const [events, setEvents] = useState<MeetupEvent[]>([]);
  const [filteredEvents, setFilteredEvents] = useState<MeetupEvent[]>([]);
  
  const [selectedEvent, setSelectedEvent] = useState<MeetupEvent | null>(null);
  const [activeChatEvent, setActiveChatEvent] = useState<MeetupEvent | null>(null);
  const [filterMode, setFilterMode] = useState<'all' | 'recommended' | 'friends'>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [viewMode, setViewMode] = useState<'list' | 'map'>('list');

  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const [radius, setRadius] = useState<number>(10);
  const [userLocation, setUserLocation] = useState<{lat: number, lng: number} | null>(null);
  const [locationStatus, setLocationStatus] = useState<string>('Locating...');

  useEffect(() => {
    const userData = sessionStorage.getItem('meetup_user');
    if (!userData) {
      navigate('/');
      return;
    }
    setUser(JSON.parse(userData));

    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setUserLocation({
            lat: position.coords.latitude,
            lng: position.coords.longitude
          });
          setLocationStatus('Location found ✓');
        },
        (error) => {
          console.error("Error getting location:", error);
          setUserLocation({ lat: 43.316, lng: 21.894 });
          setLocationStatus('Using default (Nis)');
        }
      );
    } else {
      setUserLocation({ lat: 43.316, lng: 21.894 });
      setLocationStatus('Geolocation not supported');
    }
  }, [navigate]);

  useEffect(() => {
    if (filterMode === 'recommended' && !userLocation) return; 
    loadEvents(filterMode);
  }, [filterMode, radius, userLocation]); 

  useEffect(() => {
    let results = events;

    if (searchTerm) {
        results = results.filter(ev => 
            ev.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
            ev.description.toLowerCase().includes(searchTerm.toLowerCase())
        );
    }
    setFilteredEvents(results);
  }, [searchTerm, events]);

  const loadEvents = async (mode: 'all' | 'recommended' | 'friends') => {
    try {
      let data: MeetupEvent[] = [];
      if (mode === 'all') data = await getEvents();
      else if (mode === 'friends') data = await getFriendsEvents();
      else if (mode === 'recommended') {
        if (userLocation) {
            data = await getRecommendedEvents(userLocation.lat, userLocation.lng, radius);
        }
      }
      setEvents(data);
    } catch (error) {
      console.error("Greška pri učitavanju događaja:", error);
    }
  };

  const handleToggleAttend = async (event: MeetupEvent, isAttending: boolean) => {
  try {
    if (isAttending) {
      if (window.confirm(`Are you sure you want to leave "${event.title}"?`)) {
          await leaveEvent(event.id);
      } else {
          return;
      }
    } else {
        await attendEvent(event.id);
    }
    loadEvents(filterMode); 
    } catch (error) {
      console.error("Action failed:", error);
      alert("Failed to update attendance.");
  }
};

  const handleFilterChange = (mode: 'all' | 'recommended' | 'friends') => {
    setFilterMode(mode);
    setActiveChatEvent(null);
  };

  const handleAttendFromCard = async (event: MeetupEvent) => {
    try {
      await attendEvent(event.id);
      alert(`Uspešno ste se prijavili za: ${event.title}`);
      loadEvents(filterMode); 
    } catch (error) {
      alert("Greška pri prijavi na događaj.");
    }
  };

  const handleDeleteEvent = async (eventId: string) => {
    try {
        await deleteEvent(eventId);
        loadEvents(filterMode); 
        alert("Event deleted!");
    } catch (error) {
        console.error("Failed to delete", error);
        alert("Failed to delete event.");
    }
  };

  const handleEditEvent = (event: MeetupEvent) => {
      navigate('/edit-event', { state: { event } });
  };

  const handleEventClick = (event: MeetupEvent) => {
    const isAttending = event.attendees && Array.isArray(event.attendees) 
    ? event.attendees.some((a: any) => {
        const attendeeId = typeof a === 'object' && a !== null ? a.id : a;
        return String(attendeeId).trim() === String(user?.id).trim();
      })
    : false;

    if (isAttending) {
      setActiveChatEvent(event);
      setSelectedEvent(null);
    } else {
      setSelectedEvent(event);
      setActiveChatEvent(null);
    }
  };

  const handleLogout = () => {
    sessionStorage.removeItem('meetup_token');
    sessionStorage.removeItem('meetup_user');
    navigate('/');
  };

  const openAddFriend = () => {
    navigate('/add-friend');
  };

  const openSettings = () => {
    navigate('/settings');
  };  

  return (
    <div className="home-container">
      <nav className="top-nav">
        <div className="nav-left">
          <h2 className="logo" onClick={() => {setActiveChatEvent(null); setViewMode('list')}}>
            MeetApp
          </h2>
        </div>
        
        <div className="nav-right">
          <button onClick={openAddFriend} className="add-friend-nav-btn">
            + Add Friend
          </button>

          <button onClick={() => navigate('/create-event')} className="create-btn">+ Create Event</button>
          
          <div 
            className="user-menu" 
            onClick={() => setIsMenuOpen(!isMenuOpen)} 
          >
            <div className="user-avatar">{user?.name?.charAt(0).toUpperCase() || 'U'}</div>
            
            {isMenuOpen && (
              <div className="user-dropdown">
                <span onClick={(e) => {
                    e.stopPropagation(); 
                    openSettings();
                }}>Settings</span>
                
                <span onClick={(e) => {
                    e.stopPropagation();
                    handleLogout();
                }} className="logout-text">Log out</span>
              </div>
            )}
          </div>
        </div>
      </nav>

      <div className="main-content">
        <aside className="sidebar">
          <div className="user-welcome-card">
            <div className="big-avatar">{user?.name?.charAt(0).toUpperCase()}</div>
            <h3>{user?.name}</h3>
            {user?.bio && <p className="user-bio-small">{user.bio}</p>}
            <p className="location-status-text">📍 {locationStatus}</p>
          </div>

          <div className="search-box">
            <label>Find events</label>
            <div className="input-wrapper">
              <span className="search-icon">🔍</span>
              <input 
                type="text" 
                placeholder="Search events..." 
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
          </div>

          <div className="sidebar-filters">
            <h4>Filters</h4>
            <button className={`side-btn ${filterMode === 'all' ? 'active' : ''}`} onClick={() => handleFilterChange('all')}>All Events</button>
            <button className={`side-btn ${filterMode === 'recommended' ? 'active' : ''}`} onClick={() => handleFilterChange('recommended')}>Recommended For You</button>
            
            {filterMode === 'recommended' && (
                <div className="radius-control">
                    <label>Distance: <strong>{radius} km</strong></label>
                    <input type="range" min="1" max="1000" value={radius} onChange={(e) => setRadius(Number(e.target.value))} className="radius-slider" />
                </div>
            )}
            
            <button className={`side-btn ${filterMode === 'friends' ? 'active' : ''}`} onClick={() => handleFilterChange('friends')}>Friends Going</button>
          </div>

          <div className="view-toggle">
            <button onClick={() => setViewMode('list')} className={viewMode === 'list' ? 'active' : ''}>List View</button>
            <button onClick={() => setViewMode('map')} className={viewMode === 'map' ? 'active' : ''}>Map View</button>
          </div>
        </aside>

        <main className="feed-area">
          {activeChatEvent ? (
            <div className="chat-view-container">
              <div className="chat-header-redesigned">
                <div className="header-content-centered">
                    <h2 className="chat-event-title">{activeChatEvent.title}</h2>
                    <span className="chat-event-time">
                      🕒 {new Date(activeChatEvent.date).toLocaleString('sr-RS', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' })}
                    </span>
                </div>
                <button onClick={() => setActiveChatEvent(null)} className="close-chat-x-btn">✕</button>
              </div>
              <ChatBox eventId={activeChatEvent.id} currentUser={user} />
            </div>
          ) : viewMode === 'map' ? (
            <div className="map-wrapper-embedded">
              <Map events={filteredEvents} onEventSelect={handleEventClick} />
            </div>
          ) : (
            <>
              <div className="feed-header">
                <h2>
                     {filterMode === 'recommended' ? `Events within ${radius}km` : 
                     filterMode === 'friends' ? 'Events Friends Are Going To' : 'All Upcoming Events'}
                </h2>
                <span className="results-count">{filteredEvents.length} events found</span>
              </div>

              <div className="events-grid">
                {filteredEvents.map(event => (
                  <EventCard 
                    key={event.id} 
                    event={event} 
                    currentUser={user} 
                    onClick={() => handleEventClick(event)}
                    onAttend={(isAttending) => handleToggleAttend(event, isAttending)}
                    onDelete={() => handleDeleteEvent(event.id)} 
                    onEdit={handleEditEvent}
                  />
                ))}
                {filteredEvents.length === 0 && (
                  <div className="empty-state">
                      {filterMode === 'recommended' ? `No events found within ${radius}km.` : "No events found."}
                  </div>
                )}
              </div>
            </>
          )}
        </main>
      </div>

      {selectedEvent && (
        <EventDetails 
          event={selectedEvent} 
          currentUser={user}
          onClose={() => setSelectedEvent(null)} 
        />
      )}
    </div>
  );
};

export default Home;