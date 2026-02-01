import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import type { LatLngExpression } from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { MeetupEvent } from '../models/Event';
import L from 'leaflet';
import icon from 'leaflet/dist/images/marker-icon.png';
import iconShadow from 'leaflet/dist/images/marker-shadow.png';
import './Map.css';

let DefaultIcon = L.icon({
    iconUrl: icon,
    shadowUrl: iconShadow,
    iconSize: [25, 41],
    iconAnchor: [12, 41]
});
L.Marker.prototype.options.icon = DefaultIcon;

interface MapProps {
  events: MeetupEvent[];
  onEventSelect: (event: MeetupEvent) => void;
}

const Map = ({ events, onEventSelect }: MapProps) => {
  const center: LatLngExpression = [44.7866, 20.4489]; 
  
  return (
    <MapContainer center={center} zoom={13} className="map-view">
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
      
      {events.map((event) => (
        <Marker key={event.id} position={[event.latitude, event.longitude]}>
          <Popup>
            <h3 className="popup-title">{event.title}</h3>
            <button 
                onClick={() => onEventSelect(event)}
                className="popup-btn"
            >
                View Details
            </button>
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
};

export default Map;