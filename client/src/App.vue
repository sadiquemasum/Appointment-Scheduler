<script setup lang="ts">
import { ref } from 'vue';
import AppointmentList from './components/AppointmentList.vue';
import AppointmentForm from './components/AppointmentForm.vue';
import type { Appointment } from './types/appointment';

const editingAppointment = ref<Appointment | null>(null);

function onEdit(appointment: Appointment) {
  editingAppointment.value = appointment;
}

function onSaved() {
  editingAppointment.value = null;
}

function onCancelled() {
  editingAppointment.value = null;
}
</script>

<template>
  <main>
    <h1>Telenor Appointment Scheduler</h1>
    <AppointmentForm :appointment="editingAppointment" @saved="onSaved" @cancelled="onCancelled" />
    <AppointmentList @edit="onEdit" />
  </main>
</template>