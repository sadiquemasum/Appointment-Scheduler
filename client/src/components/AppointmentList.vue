<script setup lang="ts">
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { getAppointments, deleteAppointment } from '../api/appointments';
import type { Appointment } from '../types/appointment';

const emit = defineEmits<{ edit: [appointment: Appointment] }>();

const queryClient = useQueryClient();

const { data: appointments, isLoading, isError, error } = useQuery({
  queryKey: ['appointments'],
  queryFn: () => getAppointments(),
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
</script>

<template>
  <div>
    <h2>Appointments</h2>

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

    <p v-else>No appointments scheduled.</p>
  </div>
</template>