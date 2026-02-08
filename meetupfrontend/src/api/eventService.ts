import apiClient from './axiosClient';
import type { MeetupEvent, CreateEventDto, UpdateEventDto } from '../models/Event';



export const getEvents = async (): Promise<MeetupEvent[]> => {
  const response = await apiClient.get<MeetupEvent[]>('/events');
  return response.data;
};

export const createEvent = async (eventData: CreateEventDto): Promise<void> => {
  await apiClient.post('/events', eventData);
};

export const getFriendsEvents = async (): Promise<MeetupEvent[]> => {
  const response = await apiClient.get<MeetupEvent[]>('/events/friendsrecommendations');
  return response.data;
};

export const getRecommendedEvents = async (lat: number, lng: number, radius: number = 10): Promise<MeetupEvent[]> => {
  const response = await apiClient.get<MeetupEvent[]>('/events/recommendations', {
    params: { latitude: lat, longitude: lng, radius }
  });
  return response.data;
};

export const attendEvent = async (eventId: string): Promise<void> => {
  await apiClient.post(`/events/${eventId}/attend`);
};

export const leaveEvent = async (eventId: string): Promise<void> => {
  await apiClient.post(`/events/${eventId}/leave`);
};

export const deleteEvent = async (eventId: string): Promise<void> => {
  await apiClient.delete(`/events/${eventId}`);
};

export const updateEvent = async (eventId: string, eventData: UpdateEventDto) => {
  const response = await apiClient.put(`/events/${eventId}`, eventData);
  return response.data;
};

export const getEventAttendees = async (eventId: string): Promise<any[]> => {
  const response = await apiClient.get<any[]>(`/events/${eventId}/attendees`);
  return response.data;
};

export const kickUser = async (eventId: string, userId: string): Promise<void> => {
  await apiClient.post(`/events/${eventId}/kick/${userId}`);
};