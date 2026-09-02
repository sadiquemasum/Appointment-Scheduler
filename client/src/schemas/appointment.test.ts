import { describe, it, expect } from 'vitest';
import { createAppointmentSchema } from './appointment';

describe('createAppointmentSchema', () => {
  it('fails when customerName is empty', () => {
    const result = createAppointmentSchema.safeParse({
      customerName: '',
      customerPhone: null,
      customerEmail: null,
      start: '2026-09-01T10:00',
      end: '2026-09-01T10:30',
      notes: null,
    });

    expect(result.success).toBe(false);
  });

  it('fails when end is before start', () => {
    const result = createAppointmentSchema.safeParse({
      customerName: 'Jane Doe',
      customerPhone: null,
      customerEmail: null,
      start: '2026-09-01T10:30',
      end: '2026-09-01T10:00',
      notes: null,
    });

    expect(result.success).toBe(false);
  });

  it('fails when email format is invalid', () => {
    const result = createAppointmentSchema.safeParse({
      customerName: 'Jane Doe',
      customerPhone: null,
      customerEmail: 'not-an-email',
      start: '2026-09-01T10:00',
      end: '2026-09-01T10:30',
      notes: null,
    });

    expect(result.success).toBe(false);
  });

  it('succeeds with valid data', () => {
    const result = createAppointmentSchema.safeParse({
      customerName: 'Jane Doe',
      customerPhone: '+46701234567',
      customerEmail: 'jane@example.com',
      start: '2026-09-01T10:00',
      end: '2026-09-01T10:30',
      notes: 'Some notes',
    });

    expect(result.success).toBe(true);
  });

  it('succeeds when optional fields are empty strings', () => {
    const result = createAppointmentSchema.safeParse({
      customerName: 'Jane Doe',
      customerPhone: '',
      customerEmail: '',
      start: '2026-09-01T10:00',
      end: '2026-09-01T10:30',
      notes: '',
    });

    expect(result.success).toBe(true);
  });
});