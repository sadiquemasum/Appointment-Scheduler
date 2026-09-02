export interface Appointment {
  id: string;
  customerName: string;
  customerPhone: string | null;
  customerEmail: string | null;
  start: string;
  end: string;
  notes: string | null;
  externalId: string | null;
}

export interface CreateAppointmentPayload {
  customerName: string;
  customerPhone: string | null;
  customerEmail: string | null;
  start: string;
  end: string;
  notes: string | null;
}