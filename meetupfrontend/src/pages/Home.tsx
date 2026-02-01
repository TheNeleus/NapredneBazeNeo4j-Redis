import { useState, useEffect } from 'react';
import Map from '../components/Map';
import { useNavigate } from 'react-router-dom';
import { getEvents, getFriendsEvents, getRecommendedEvents } from '../api/eventService';
import type { MeetupEvent } from '../models/Event';
import EventDetails from '../components/EventDetails';
import './Home.css';

const Home = () => {
  const navigate = useNavigate();
  const [user, setUser] = useState<any>(null);
  const [events, setEvents] = useState<MeetupEvent[]>([]);
  const [selectedEvent, setSelectedEvent] = useState<MeetupEvent | null>(null);
  const [filterMode, setFilterMode] = useState<'all' | 'recommended' | 'friends'>('all');

  const loadEvents = async (mode: string) => {
    try {
      let data: MeetupEvent[] = [];
      if (mode === 'all') data = await getEvents();
      else if (mode === 'friends') data = await getFriendsEvents();
      else if (mode === 'recommended') {
        const myLat = 44.7866;
        const myLng = 20.4489;
        data = await getRecommendedEvents(myLat, myLng, 50);
      }
      setEvents(data);
    } catch (error) {
      console.error("Greska:", error);
    }
  };

  useEffect(() => {
    const userData = localStorage.getItem('meetup_user');
    if (!userData) {
      navigate('/');
      return;
    }
    setUser(JSON.parse(userData));
    loadEvents('all');
  }, [navigate]);

  const handleFilterChange = (mode: 'all' | 'recommended' | 'friends') => {
    setFilterMode(mode);
    loadEvents(mode);
  };

  const handleLogout = () => {
    localStorage.removeItem('meetup_token');
    localStorage.removeItem('meetup_user');
    navigate('/');
  };

  return (
    <div className="home-container">
      <nav className="nav-bar">
        <h2 className="nav-title">Meetup App</h2>
        
        <div className="filter-group">
            <button 
                className={`filter-btn ${filterMode === 'all' ? 'active' : ''}`} 
                onClick={() => handleFilterChange('all')}>All Events
            </button>
            <button 
                className={`filter-btn ${filterMode === 'recommended' ? 'active' : ''}`} 
                onClick={() => handleFilterChange('recommended')}>For You
            </button>
            <button 
                className={`filter-btn ${filterMode === 'friends' ? 'active' : ''}`} 
                onClick={() => handleFilterChange('friends')}>Friends Going
            </button>
        </div>

        <div>
            <button onClick={() => navigate('/create-event')} className="new-event-btn">
               + New
            </button>
            <button onClick={handleLogout} className="logout-btn">
                Logout
            </button>
        </div>
      </nav>

      <div className="map-wrapper">
        <Map events={events} onEventSelect={(ev) => setSelectedEvent(ev)} />
        
        {selectedEvent && (
          <EventDetails 
            event={selectedEvent} 
            currentUser={user}
            onClose={() => setSelectedEvent(null)} 
          />
        )}
        
        <div className="info-panel">
            Showing: <strong>{events.length}</strong> events
        </div>
      </div>
    </div>
  );
};

export default Home;