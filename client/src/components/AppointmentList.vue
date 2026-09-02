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

    <p v-if="isLoading">Loading appointments...</p>
    <p v-else-if="isError">Failed to load appointments: {{ (error as Error)?.message }}</p>

    <ul v-else-if="appointments && appointments.length > 0">
      <li v-for="appointment in appointments" :key="appointment.id">
        <strong>{{ appointment.customerName }}</strong> —
        {{ new Date(appointment.start).toLocaleString() }} to
        {{ new Date(appointment.end).toLocaleTimeString() }}
        <button @click="emit('edit', appointment)">Edit</button>
        <button @click="onDelete(appointment)" :disabled="deleteMutation.isPending.value">
          Delete
        </button>
      </li>
    </ul>

    <p v-else>No appointments in this range.</p>
  </div>
</template>