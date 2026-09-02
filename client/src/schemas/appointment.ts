import { z } from 'zod';

export const createAppointmentSchema = z
  .object({
    customerName: z.string().min(1, 'Customer name is required').max(200),
    customerPhone: z.string().optional().nullable(),
    customerEmail: z.string().email('Invalid email address').optional().nullable().or(z.literal('')),
    start: z.string().min(1, 'Start time is required'),
    end: z.string().min(1, 'End time is required'),
    notes: z.string().optional().nullable(),
  })
  .refine((data) => new Date(data.end) > new Date(data.start), {
    message: 'End time must be after start time',
    path: ['end'],
  });

export type CreateAppointmentFormValues = z.infer<typeof createAppointmentSchema>;