<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query';
import { getAppointments } from '../api/appointments';

const { data: appointments, isLoading, isError, error } = useQuery({
  queryKey: ['appointments'],
  queryFn: () => getAppointments(),
});
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
      </li>
    </ul>

    <p v-else>No appointments scheduled.</p>
  </div>
</template>