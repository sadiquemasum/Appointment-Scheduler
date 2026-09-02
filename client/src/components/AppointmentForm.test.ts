import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/vue';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import AppointmentForm from './AppointmentForm.vue';
import * as appointmentsApi from '../api/appointments';

vi.mock('../api/appointments');

function renderForm() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(AppointmentForm, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
    },
  });
}

describe('AppointmentForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows validation error when name is empty', async () => {
    renderForm();

    const submitButton = screen.getByRole('button', { name: /create appointment/i });
    await fireEvent.click(submitButton);

    await waitFor(() => {
      const nameInput = screen.getByLabelText(/customer name/i);
      const fieldContainer = nameInput.closest('.field');
      expect(fieldContainer?.querySelector('.error')).toHaveTextContent(/required/i);
    });
  });

  it('shows conflict warning when checkConflict returns true', async () => {
    vi.mocked(appointmentsApi.checkConflict).mockResolvedValue({
      hasConflict: true,
      conflicts: [
        {
          id: 'existing-id',
          customerName: 'Jane Doe',
          start: '2026-09-01T10:00:00+02:00',
          end: '2026-09-01T10:30:00+02:00',
        },
      ],
    });

    renderForm();

    const startInput = screen.getByLabelText(/^start/i);
    const endInput = screen.getByLabelText(/^end/i);

    await fireEvent.update(startInput, '2026-09-01T10:15');
    await fireEvent.update(endInput, '2026-09-01T10:45');

    await waitFor(() => {
      expect(screen.getByText(/conflicts with jane doe/i)).toBeInTheDocument();
    });

    expect(appointmentsApi.checkConflict).toHaveBeenCalled();
  });

  it('pre-fills fields and shows Edit Appointment heading when appointment prop is provided', async () => {
    const existingAppointment = {
      id: 'existing-id',
      customerName: 'Erik Svensson',
      customerPhone: '+46709876543',
      customerEmail: 'erik@example.com',
      start: '2026-09-02T15:00:00+02:00',
      end: '2026-09-02T15:30:00+02:00',
      notes: 'Rescheduled',
      externalId: null,
    };

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });

    render(AppointmentForm, {
      props: { appointment: existingAppointment },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }]],
      },
    });

    expect(screen.getByText(/edit appointment/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/customer name/i)).toHaveValue('Erik Svensson');
    expect(screen.getByLabelText(/phone/i)).toHaveValue('+46709876543');
    expect(screen.getByRole('button', { name: /save changes/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
  });
});
