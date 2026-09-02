<script setup lang="ts">
import { ref, computed } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { getAppointments, deleteAppointment } from '../api/appointments';
import type { Appointment } from '../types/appointment';

const emit = defineEmits<{ edit: [appointment: Appointment] }>();

const queryClient = useQueryClient();

const fromDate = ref<string>('');
const toDate = ref<string>('');

const fromIso = computed(() => (fromDate.value ? new Date(fromDate.value).toISOString() : undefined));
const toIso = computed(() => {
  if (!toDate.value) return undefined;
  // Treat the "To" date as inclusive of the entire day, not just
  // midnight at its start - otherwise appointments later that same
  // day get incorrectly excluded from the range.
  const endOfDay = new Date(toDate.value);
  endOfDay.setHours(23, 59, 59, 999);
  return endOfDay.toISOString();
});

const { data: appointments, isLoading, isError, error } = useQuery({
  queryKey: ['appointments', fromIso, toIso],
  queryFn: () => getAppointments(fromIso.value, toIso.value),
});

const deleteMutation = useMutation({
  mutationFn: deleteAppointment,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['appointments'] });
  },
});

function onDelete(appointment: Appointment) {
  const confirmed = window.confirm(
    `Delete the appointment for ${appointment.customerName}? This cannot be undone.`
  );
  if (confirmed) {
    deleteMutation.mutate(appointment.id);
  }
}

function clearFilters() {
  fromDate.value = '';
  toDate.value = '';
}
</script>

<template>
  <div>
    <h2>Appointments</h2>

    <div class="filters-box">
      <span class="filters-label">Filter by date</span>
      <div class="filters">
        <label>
          From
          <input type="date" v-model="fromDate" />
        </label>
        <label>
          To
          <input type="date" v-model="toDate" />
        </label>
        <button v-if="fromDate || toDate" @click="clearFilters" type="button">Clear filters</button>
      </div>
    </div>

    <p v-if="isLoading">Loading appointments...</p>
    <p v-else-if="isError">Failed to load appointments: {{ (error as Error)?.message }}</p>

    <ul v-else-if="appointments && appointments.length > 0" class="appointment-list">
<li v-for="appointment in appointments" :key="appointment.id">
  <div class="details">
    <div class="detail-row">
      <span class="label">Name:</span>
      <strong>{{ appointment.customerName }}</strong>
    </div>
    <div class="detail-row">
      <span class="label">Booked:</span>
      <span>{{ new Date(appointment.start).toLocaleString() }} – {{ new Date(appointment.end).toLocaleTimeString() }}</span>
    </div>
    <div class="detail-row">
      <span class="label">Phone:</span>
      <span>{{ appointment.customerPhone || '—' }}</span>
    </div>
    <div class="detail-row">
      <span class="label">Email:</span>
      <span>{{ appointment.customerEmail || '—' }}</span>
    </div>
    <div class="detail-row">
      <span class="label">Notes:</span>
      <span>{{ appointment.notes || '—' }}</span>
    </div>
  </div>
  <div class="actions">
    <button @click="emit('edit', appointment)">Edit</button>
    <button @click="onDelete(appointment)" :disabled="deleteMutation.isPending.value">
      Delete
    </button>
  </div>
</li>
    </ul>

    <p v-else>No appointments in this range.</p>
  </div>
</template>

<style scoped>
.filters-box {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 14px 16px;
  margin-bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.filters-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--text);
}

.filters {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.filters label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: var(--text-muted);
}

.filters input {
  width: auto;
}

.appointment-list {
  list-style: none;
  margin: 0;
  padding: 0;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  overflow: hidden;
}

.appointment-list li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 12px 14px;
}

.appointment-list li:not(:last-child) {
  border-bottom: 1px solid var(--border);
}

.details {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
  flex: 1;
}

.detail-row {
  display: grid;
  grid-template-columns: 70px 1fr;
  gap: 8px;
  font-size: 13px;
}

.detail-row .label {
  color: var(--text-muted);
  font-weight: 500;
}

.detail-row strong {
  font-size: 14px;
}

.time {
  font-size: 13px;
  color: var(--text-muted);
}

.actions {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

.actions button {
  padding: 4px 10px;
  font-size: 13px;
  min-width: 64px;
}

.primary-line {
  display: flex;
  align-items: baseline;
  gap: 10px;
  flex-wrap: wrap;
}

.secondary-line {
  font-size: 13px;
  color: var(--text-muted);
}

.notes {
  font-size: 13px;
  color: var(--text);
  margin: 4px 0 0;
  max-width: 60ch;
}

.label {
  color: var(--text-muted);
  font-weight: 500;
}
</style>