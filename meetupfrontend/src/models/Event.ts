export interface MeetupEvent {
  id: string;
  title: string;
  description: string;
  latitude: number;
  longitude: number;
  date: string;
  category?: string;
}

export interface CreateEventDto {
  title: string;
  description: string;
  date: string;     // ISO string
  category: string;
  latitude: number;
  longitude: number;
}