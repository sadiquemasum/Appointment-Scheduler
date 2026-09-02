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

export interface ConflictCheckResult {
  hasConflict: boolean;
  conflicts: Array<{ id: string; customerName: string; start: string; end: string }>;
}

export async function checkConflict(
  start: string,
  end: string,
  excludeId?: string
): Promise<ConflictCheckResult> {
  const response = await apiClient.get<ConflictCheckResult>('/api/appointments/check-conflict', {
    params: { start, end, excludeId },
  });
  return response.data;
}

export interface UpdateAppointmentPayload extends CreateAppointmentPayload {
  id: string;
}

export async function updateAppointment(payload: UpdateAppointmentPayload): Promise<Appointment> {
  const response = await apiClient.put<Appointment>(`/api/appointments/${payload.id}`, payload);
  return response.data;
}

export async function deleteAppointment(id: string): Promise<void> {
  await apiClient.delete(`/api/appointments/${id}`);
}

export interface ImportResult {
  imported: number;
  skippedDuplicate: number;
  skippedConflict: number;
  conflictDetails: string[];
}

export async function importAppointments(): Promise<ImportResult> {
  const response = await apiClient.post<ImportResult>('/api/appointments/import');
  return response.data;
}