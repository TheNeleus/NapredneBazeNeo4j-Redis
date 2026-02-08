import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LocationPicker from '../components/LocationPicker'; 
import { createEvent } from '../api/eventService'; 
import './CreateEvent.css';

const CATEGORIES = [
  'Tech',
  'Sport',
  'Music',
  'Art',
  'Travel',
  'Food',
  'Gaming',
  'Social'
];

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

      if (isNaN(fullDate.getTime())) {
         alert("Invalid date/time format");
         setLoading(false);
         return;
      }

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

  const handleInputChange = (field: string, value: string) => {
      setFormData(prev => ({ ...prev, [field]: value }));
  };

  return (
    <div className="create-event-container">
      <h1>Create New Event</h1>
      
      <form onSubmit={handleSubmit} className="create-form">
        <input 
          type="text" 
          placeholder="Event Title" 
          required 
          value={formData.title}
          onChange={e => handleInputChange('title', e.target.value)}
          className="form-input"
        />

        <textarea 
          placeholder="Description" 
          required 
          rows={4}
          value={formData.description}
          onChange={e => handleInputChange('description', e.target.value)}
          className="form-textarea"
        />

        <div className="date-time-row">
          <input 
            type="date" 
            required 
            value={formData.date}
            onChange={e => handleInputChange('date', e.target.value)}
            className="form-input date-time-input" 
          />
          <input 
            type="time" 
            required 
            value={formData.time}
            onChange={e => handleInputChange('time', e.target.value)}
            className="form-input date-time-input"
          />
        </div>

        <label className="category-label">Category</label>
        <select 
          value={formData.category}
          onChange={e => handleInputChange('category', e.target.value)}
          className="form-select"
        >
          {CATEGORIES.map((cat) => (
            <option key={cat} value={cat}>{cat}</option>
          ))}
        </select>

        <div className="location-box">
            <p className="location-label">Pick Location (Click on map):</p>
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