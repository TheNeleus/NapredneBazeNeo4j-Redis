import apiClient from './axiosClient';
import type { MeetupEvent, CreateEventDto } from '../models/Event';


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