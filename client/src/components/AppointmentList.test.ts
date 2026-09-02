import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/vue';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import AppointmentList from './AppointmentList.vue';
import * as appointmentsApi from '../api/appointments';

vi.mock('../api/appointments');

const sampleAppointment = {
  id: 'appt-1',
  customerName: 'Jane Doe',
  customerPhone: null,
  customerEmail: null,
  start: '2026-09-01T10:00:00+02:00',
  end: '2026-09-01T10:30:00+02:00',
  notes: null,
  externalId: null,
};

function renderList() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(AppointmentList, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
    },
  });
}

describe('AppointmentList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(appointmentsApi.getAppointments).mockResolvedValue([sampleAppointment]);
  });

  it('does not call deleteAppointment when the confirmation dialog is cancelled', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    renderList();

    const deleteButton = await screen.findByRole('button', { name: /delete/i });
    await fireEvent.click(deleteButton);

    expect(window.confirm).toHaveBeenCalled();
    expect(appointmentsApi.deleteAppointment).not.toHaveBeenCalled();
  });

  it('calls deleteAppointment when the confirmation dialog is accepted', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    vi.mocked(appointmentsApi.deleteAppointment).mockResolvedValue(undefined);
    renderList();

    const deleteButton = await screen.findByRole('button', { name: /delete/i });
    await fireEvent.click(deleteButton);

    await waitFor(() => {
      expect(appointmentsApi.deleteAppointment).toHaveBeenCalledTimes(1);
    });

    const callArgs = vi.mocked(appointmentsApi.deleteAppointment).mock.calls[0];
    expect(callArgs[0]).toBe('appt-1');
  });

  it('calls getAppointments with from/to values when the date filters are set', async () => {
    renderList();
    await screen.findByText('Jane Doe');

    const fromInput = screen.getByLabelText(/^from/i);
    const toInput = screen.getByLabelText(/^to/i);

    await fireEvent.update(fromInput, '2026-09-01');
    await fireEvent.update(toInput, '2026-09-03');

    await waitFor(() => {
      const lastCall = vi.mocked(appointmentsApi.getAppointments).mock.calls.at(-1);
      expect(lastCall?.[0]).toContain('2026-09-01');
      expect(lastCall?.[1]).toContain('2026-09-03');
    });
  });
});
