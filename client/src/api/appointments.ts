import { apiClient } from './client';
import type { Appointment, CreateAppointmentPayload } from '../types/appointment';

export async function getAppointments(from?: string, to?: string): Promise<Appointment[]> {
  const response = await apiClient.get<Appointment[]>('/api/appointments', {
    params: { from, to },
  });
  return response.data;
}

export async function createAppointment(payload: CreateAppointmentPayload): Promise<Appointment> {
  const response = await apiClient.post<Appointment>('/api/appointments', payload);
  return response.data;
}