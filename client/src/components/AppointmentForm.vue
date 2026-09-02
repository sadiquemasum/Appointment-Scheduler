<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { createAppointment, updateAppointment, checkConflict } from '../api/appointments';
import { createAppointmentSchema, type CreateAppointmentFormValues } from '../schemas/appointment';
import type { Appointment } from '../types/appointment';

const props = defineProps<{ appointment?: Appointment | null }>();
const emit = defineEmits<{ saved: []; cancelled: [] }>();

const isEditMode = computed(() => !!props.appointment);

const queryClient = useQueryClient();

function toLocalDateTimeInput(isoString: string): string {
  const date = new Date(isoString);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

const { handleSubmit, defineField, errors, resetForm, values, setValues } =
  useForm<CreateAppointmentFormValues>({
    validationSchema: toTypedSchema(createAppointmentSchema),
    initialValues: props.appointment
      ? {
          customerName: props.appointment.customerName,
          customerPhone: props.appointment.customerPhone ?? '',
          customerEmail: props.appointment.customerEmail ?? '',
          start: toLocalDateTimeInput(props.appointment.start),
          end: toLocalDateTimeInput(props.appointment.end),
          notes: props.appointment.notes ?? '',
        }
      : undefined,
  });

// If the prop changes (user clicks Edit on a different appointment while
// this form is already mounted), re-populate the form.
watch(
  () => props.appointment,
  (newAppointment) => {
    if (newAppointment) {
      setValues({
        customerName: newAppointment.customerName,
        customerPhone: newAppointment.customerPhone ?? '',
        customerEmail: newAppointment.customerEmail ?? '',
        start: toLocalDateTimeInput(newAppointment.start),
        end: toLocalDateTimeInput(newAppointment.end),
        notes: newAppointment.notes ?? '',
      });
    }
  }
);

const [customerName, customerNameAttrs] = defineField('customerName');
const [customerPhone, customerPhoneAttrs] = defineField('customerPhone');
const [customerEmail, customerEmailAttrs] = defineField('customerEmail');
const [start, startAttrs] = defineField('start');
const [end, endAttrs] = defineField('end');
const [notes, notesAttrs] = defineField('notes');

const conflictWarning = ref<string | null>(null);
const submitError = ref<string | null>(null);

watch([() => values.start, () => values.end], async ([newStart, newEnd]) => {
  conflictWarning.value = null;
  if (!newStart || !newEnd) return;
  if (new Date(newEnd) <= new Date(newStart)) return;

  try {
    const result = await checkConflict(
      new Date(newStart).toISOString(),
      new Date(newEnd).toISOString(),
      props.appointment?.id
    );
    if (result.hasConflict) {
      const conflict = result.conflicts[0];
      conflictWarning.value = `Conflicts with ${conflict.customerName}'s appointment at ${new Date(conflict.start).toLocaleTimeString()}`;
    }
  } catch {
    // Live hint only - ignore failures here
  }
});

const createMutation = useMutation({
  mutationFn: createAppointment,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['appointments'] });
    resetForm();
    submitError.value = null;
    emit('saved');
  },
  onError: handleMutationError,
});

const updateMutation = useMutation({
  mutationFn: updateAppointment,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['appointments'] });
    submitError.value = null;
    emit('saved');
  },
  onError: handleMutationError,
});

function handleMutationError(error: any) {
  if (error.response?.status === 409) {
    const conflicts = error.response.data.conflicts;
    submitError.value = `Time conflict: overlaps with ${conflicts[0]?.customerName}'s appointment.`;
  } else if (error.response?.status === 404) {
    submitError.value = 'This appointment no longer exists. It may have been deleted.';
  } else {
    submitError.value = 'Failed to save appointment. Please try again.';
  }
}

const isPending = computed(() => createMutation.isPending.value || updateMutation.isPending.value);

const onSubmit = handleSubmit((formValues) => {
  submitError.value = null;
  const payload = {
    customerName: formValues.customerName,
    customerPhone: formValues.customerPhone || null,
    customerEmail: formValues.customerEmail || null,
    start: new Date(formValues.start).toISOString(),
    end: new Date(formValues.end).toISOString(),
    notes: formValues.notes || null,
  };

  if (isEditMode.value && props.appointment) {
    updateMutation.mutate({ id: props.appointment.id, ...payload });
  } else {
    createMutation.mutate(payload);
  }
});

function onCancel() {
  resetForm();
  emit('cancelled');
}
</script>

<template>
  <form @submit="onSubmit" class="appointment-form">
    <h2>{{ isEditMode ? 'Edit Appointment' : 'New Appointment' }}</h2>

    <div class="field">
      <label for="customerName">Customer Name *</label>
      <input
        id="customerName"
        v-model="customerName"
        v-bind="customerNameAttrs"
        type="text"
        :disabled="isPending"
      />
      <span class="error" v-if="errors.customerName">{{ errors.customerName }}</span>
    </div>

    <div class="field">
      <label for="customerPhone">Phone</label>
      <input
        id="customerPhone"
        v-model="customerPhone"
        v-bind="customerPhoneAttrs"
        type="text"
        :disabled="isPending"
      />
    </div>

    <div class="field">
      <label for="customerEmail">Email</label>
      <input
        id="customerEmail"
        v-model="customerEmail"
        v-bind="customerEmailAttrs"
        type="email"
        :disabled="isPending"
      />
      <span class="error" v-if="errors.customerEmail">{{ errors.customerEmail }}</span>
    </div>

    <div class="field">
      <label for="start">Start *</label>
      <input
        id="start"
        v-model="start"
        v-bind="startAttrs"
        type="datetime-local"
        :disabled="isPending"
      />
      <span class="error" v-if="errors.start">{{ errors.start }}</span>
    </div>

    <div class="field">
      <label for="end">End *</label>
      <input id="end" v-model="end" v-bind="endAttrs" type="datetime-local" :disabled="isPending" />
      <span class="error" v-if="errors.end">{{ errors.end }}</span>
    </div>

    <p class="conflict-warning" v-if="conflictWarning">⚠️ {{ conflictWarning }}</p>

    <div class="field">
      <label for="notes">Notes</label>
      <textarea id="notes" v-model="notes" v-bind="notesAttrs" :disabled="isPending"></textarea>
    </div>

    <p class="submit-error" v-if="submitError">{{ submitError }}</p>

    <div class="form-actions">
      <button type="submit" :disabled="isPending">
        {{ isPending ? 'Saving...' : isEditMode ? 'Save Changes' : 'Create Appointment' }}
      </button>
      <button v-if="isEditMode" type="button" @click="onCancel">Cancel</button>
    </div>
  </form>
</template>

<style scoped>
.appointment-form {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.field label {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-muted);
}

.error {
  font-size: 13px;
  color: var(--danger);
}

.conflict-warning {
  background: var(--warning-bg);
  color: var(--warning);
  padding: 10px 12px;
  border-radius: var(--radius);
  font-size: 13px;
  margin: 0;
}

.submit-error {
  background: var(--danger-bg);
  color: var(--danger);
  padding: 10px 12px;
  border-radius: var(--radius);
  font-size: 13px;
  margin: 0;
}

.form-actions {
  display: flex;
  gap: 8px;
}
</style>
