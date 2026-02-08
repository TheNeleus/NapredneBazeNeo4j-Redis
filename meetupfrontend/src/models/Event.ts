export interface MeetupEvent {
  id: string;
  title: string;
  description: string;
  latitude: number;
  longitude: number;
  date: string;
  category: string; 
  attendees: string[]; 
  creatorId: string;   
  
  // Optional fields
  distanceKm?: number;
  friendsGoing?: number;
}

export interface CreateEventDto {
  title: string;
  description: string;
  date: string;     
  category: string;
  latitude: number;
  longitude: number;
}

export interface UpdateEventDto {
  title?: string;
  description?: string;
  date?: string;
  category?: string;
  latitude?: number;
  longitude?: number;
}