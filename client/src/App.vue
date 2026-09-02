<script setup lang="ts">
import { ref } from 'vue';
import AppointmentList from './components/AppointmentList.vue';
import AppointmentForm from './components/AppointmentForm.vue';
import ImportButton from './components/ImportButton.vue';
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
    <div class="layout">
      <section class="workspace">
        <AppointmentForm
          :appointment="editingAppointment"
          @saved="onSaved"
          @cancelled="onCancelled"
        />
        <ImportButton />
      </section>
      <section class="records">
        <AppointmentList @edit="onEdit" />
      </section>
    </div>
  </main>
</template>

<style scoped>
main {
  max-width: 1100px;
  margin: 0 auto;
  padding: 32px 24px;
}

.layout {
  display: grid;
  grid-template-columns: 1fr;
  gap: 24px;
}

@media (min-width: 900px) {
  .layout {
    grid-template-columns: 380px 1fr;
    align-items: start;
  }
}

.workspace,
.records {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
</style>
