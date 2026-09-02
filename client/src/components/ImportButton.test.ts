import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/vue';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import ImportButton from './ImportButton.vue';
import * as appointmentsApi from '../api/appointments';

vi.mock('../api/appointments');

function renderImportButton() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(ImportButton, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
    },
  });
}

describe('ImportButton', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the import result summary after a successful import', async () => {
    vi.mocked(appointmentsApi.importAppointments).mockResolvedValue({
      imported: 2,
      skippedDuplicate: 1,
      skippedConflict: 1,
      conflictDetails: ['Jane Doe (2026-09-01 10:00) conflicts with Jane Doe'],
    });

    renderImportButton();

    const button = screen.getByRole('button', { name: /import from external calendar/i });
    await fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText(/imported: 2/i)).toBeInTheDocument();
      expect(screen.getByText(/skipped \(duplicate\): 1/i)).toBeInTheDocument();
      expect(screen.getByText(/conflicts with jane doe/i)).toBeInTheDocument();
    });
  });

  it('shows an error message when the import request fails', async () => {
    vi.mocked(appointmentsApi.importAppointments).mockRejectedValue(new Error('Network error'));

    renderImportButton();

    const button = screen.getByRole('button', { name: /import from external calendar/i });
    await fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText(/import failed/i)).toBeInTheDocument();
    });
  });
});
