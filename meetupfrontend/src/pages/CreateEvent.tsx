import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LocationPicker from '../components/LocationPicker';
import { createEvent } from '../api/eventService';
import './CreateEvent.css';

const CreateEvent = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    category: 'Tech',
    date: '',
    time: ''
  });
  const [location, setLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!location) {
      alert("Please click on the map to select a location!");
      return;
    }

    try {
      setLoading(true);
      const fullDate = new Date(`${formData.date}T${formData.time}`);

      await createEvent({
        title: formData.title,
        description: formData.description,
        category: formData.category,
        date: fullDate.toISOString(),
        latitude: location.lat,
        longitude: location.lng
      });

      alert("Event created successfully!");
      navigate('/home');
    } catch (error) {
      console.error(error);
      alert("Failed to create event.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="create-event-container">
      <h1>Create New Event</h1>
      
      <form onSubmit={handleSubmit} className="create-form">
        <input 
          type="text" placeholder="Event Title" required 
          value={formData.title}
          onChange={e => setFormData({...formData, title: e.target.value})}
          className="form-input"
        />

        <textarea 
          placeholder="Description" required rows={4}
          value={formData.description}
          onChange={e => setFormData({...formData, description: e.target.value})}
          className="form-textarea"
        />

        <div className="date-time-row">
          <input 
            type="date" required 
            value={formData.date}
            onChange={e => setFormData({...formData, date: e.target.value})}
            className="form-input"
            style={{ flex: 1 }}
          />
          <input 
            type="time" required 
            value={formData.time}
            onChange={e => setFormData({...formData, time: e.target.value})}
            className="form-input"
            style={{ flex: 1 }}
          />
        </div>

        <select 
          value={formData.category}
          onChange={e => setFormData({...formData, category: e.target.value})}
          className="form-select"
        >
          <option value="Tech">Tech</option>
          <option value="Music">Music</option>
          <option value="Sport">Sport</option>
          <option value="Social">Social</option>
        </select>

        <div className="location-box">
            <p style={{margin: '0 0 10px 0'}}>Pick Location (Click on map):</p>
            <LocationPicker onLocationSelect={(lat, lng) => setLocation({ lat, lng })} />
        </div>

        <button type="submit" disabled={loading} className="submit-btn">
          {loading ? 'Creating...' : 'Create Event'}
        </button>
        
        <button type="button" onClick={() => navigate('/home')} className="cancel-btn">
          Cancel
        </button>
      </form>
    </div>
  );
};

export default CreateEvent;