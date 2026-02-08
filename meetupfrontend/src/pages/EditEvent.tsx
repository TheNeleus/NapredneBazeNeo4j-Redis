import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { updateEvent } from '../api/eventService';
import './EditEvent.css'; 

const CATEGORIES = ['Tech', 'Sport', 'Music', 'Art', 'Travel', 'Food', 'Gaming', 'Social'];

const EditEvent = () => {
  const navigate = useNavigate();
  const location = useLocation();
  
  const eventToEdit = location.state?.event;

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [date, setDate] = useState('');
  const [category, setCategory] = useState('Tech');
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (!eventToEdit) {
      alert("No event data found.");
      navigate('/home');
      return;
    }
    
    setTitle(eventToEdit.title);
    setDescription(eventToEdit.description);
    setCategory(eventToEdit.category);
    
    const d = new Date(eventToEdit.date);
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    setDate(d.toISOString().slice(0, 16));

  }, [eventToEdit, navigate]);

  const handleUpdate = async () => {
    if (!title || !date || !description) return alert("All fields are required");

    setIsSaving(true);
    try {
      await updateEvent(eventToEdit.id, {
        ...eventToEdit,
        title,
        description,
        date: new Date(date).toISOString(),
        category
      });
      
      alert("Event updated successfully!");
      navigate('/home');
    } catch (error) {
      console.error("Update failed", error);
      alert("Failed to update event.");
    } finally {
      setIsSaving(false);
    }
  };

  if (!eventToEdit) return null;

  return (
    <div className="edit-event-container">
      <div className="edit-event-card">
        <button className="ee-back-btn" onClick={() => navigate('/home')}>← Back to Home</button>
        
        <div className="ee-header">
            <h2 className="ee-title">Edit Event</h2>
        </div>

        <div className="ee-form-group">
            <label className="ee-label">Event Title</label>
            <input 
                className="ee-input" 
                value={title} 
                onChange={e => setTitle(e.target.value)} 
            />
        </div>

        <div className="ee-form-group">
            <label className="ee-label">Category</label>
            <select className="ee-select" value={category} onChange={e => setCategory(e.target.value)}>
                {CATEGORIES.map(cat => (
                    <option key={cat} value={cat}>{cat}</option>
                ))}
            </select>
        </div>

        <div className="ee-form-group">
            <label className="ee-label">Date & Time</label>
            <input 
                type="datetime-local" 
                className="ee-input" 
                value={date} 
                onChange={e => setDate(e.target.value)} 
            />
        </div>

        <div className="ee-form-group">
            <label className="ee-label">Description</label>
            <textarea 
                className="ee-textarea" 
                rows={5} 
                value={description} 
                onChange={e => setDescription(e.target.value)} 
            />
        </div>

        <button onClick={handleUpdate} disabled={isSaving} className="ee-save-btn">
            {isSaving ? 'Saving Changes...' : 'Save Changes'}
        </button>
      </div>
    </div>
  );
};

export default EditEvent;