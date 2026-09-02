<script setup lang="ts">
import { ref, watch } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { createAppointment, checkConflict } from '../api/appointments';
import { createAppointmentSchema, type CreateAppointmentFormValues } from '../schemas/appointment';

const emit = defineEmits<{ created: [] }>();

const queryClient = useQueryClient();

const { handleSubmit, defineField, errors, resetForm, values } = useForm<CreateAppointmentFormValues>({
  validationSchema: toTypedSchema(createAppointmentSchema),
});

const [customerName, customerNameAttrs] = defineField('customerName');
const [customerPhone, customerPhoneAttrs] = defineField('customerPhone');
const [customerEmail, customerEmailAttrs] = defineField('customerEmail');
const [start, startAttrs] = defineField('start');
const [end, endAttrs] = defineField('end');
const [notes, notesAttrs] = defineField('notes');

const conflictWarning = ref<string | null>(null);
const submitError = ref<string | null>(null);

// Live conflict check as the user picks start/end times, before they submit
watch([() => values.start, () => values.end], async ([newStart, newEnd]) => {
  conflictWarning.value = null;
  if (!newStart || !newEnd) return;
  if (new Date(newEnd) <= new Date(newStart)) return;

  try {
    const result = await checkConflict(
      new Date(newStart).toISOString(),
      new Date(newEnd).toISOString()
    );
    if (result.hasConflict) {
      const conflict = result.conflicts[0];
      conflictWarning.value = `Conflicts with ${conflict.customerName}'s appointment at ${new Date(conflict.start).toLocaleTimeString()}`;
    }
  } catch {
    // Silently ignore - this is just a live hint, not a hard validation gate
  }
});

const mutation = useMutation({
  mutationFn: createAppointment,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['appointments'] });
    resetForm();
    submitError.value = null;
    emit('created');
  },
  onError: (error: any) => {
    if (error.response?.status === 409) {
      const conflicts = error.response.data.conflicts;
      submitError.value = `Time conflict: overlaps with ${conflicts[0]?.customerName}'s appointment.`;
    } else {
      submitError.value = 'Failed to create appointment. Please try again.';
    }
  },
});

const onSubmit = handleSubmit((formValues) => {
  submitError.value = null;
  mutation.mutate({
    customerName: formValues.customerName,
    customerPhone: formValues.customerPhone || null,
    customerEmail: formValues.customerEmail || null,
    start: new Date(formValues.start).toISOString(),
    end: new Date(formValues.end).toISOString(),
    notes: formValues.notes || null,
  });
});
</script>

<template>
  <form @submit="onSubmit" class="appointment-form">
    <h2>New Appointment</h2>

    <div class="field">
      <label for="customerName">Customer Name *</label>
      <input id="customerName" v-model="customerName" v-bind="customerNameAttrs" type="text" />
      <span class="error" v-if="errors.customerName">{{ errors.customerName }}</span>
    </div>

    <div class="field">
      <label for="customerPhone">Phone</label>
      <input id="customerPhone" v-model="customerPhone" v-bind="customerPhoneAttrs" type="text" />
    </div>

    <div class="field">
      <label for="customerEmail">Email</label>
      <input id="customerEmail" v-model="customerEmail" v-bind="customerEmailAttrs" type="email" />
      <span class="error" v-if="errors.customerEmail">{{ errors.customerEmail }}</span>
    </div>

    <div class="field">
      <label for="start">Start *</label>
      <input id="start" v-model="start" v-bind="startAttrs" type="datetime-local" />
      <span class="error" v-if="errors.start">{{ errors.start }}</span>
    </div>

    <div class="field">
      <label for="end">End *</label>
      <input id="end" v-model="end" v-bind="endAttrs" type="datetime-local" />
      <span class="error" v-if="errors.end">{{ errors.end }}</span>
    </div>

    <p class="conflict-warning" v-if="conflictWarning">⚠️ {{ conflictWarning }}</p>

    <div class="field">
      <label for="notes">Notes</label>
      <textarea id="notes" v-model="notes" v-bind="notesAttrs"></textarea>
    </div>

    <p class="submit-error" v-if="submitError">{{ submitError }}</p>

    <button type="submit" :disabled="mutation.isPending.value">
      {{ mutation.isPending.value ? 'Creating...' : 'Create Appointment' }}
    </button>
  </form>
</template>