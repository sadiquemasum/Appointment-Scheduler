<script setup lang="ts">
import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { importAppointments } from '../api/appointments';

const queryClient = useQueryClient();

const mutation = useMutation({
  mutationFn: importAppointments,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['appointments'] });
  },
});

function onImport() {
  mutation.mutate();
}
</script>

<template>
  <div class="import-section">
    <button @click="onImport" :disabled="mutation.isPending.value">
      {{ mutation.isPending.value ? 'Importing...' : 'Import from External Calendar' }}
    </button>

    <div v-if="mutation.isSuccess.value" class="import-result">
      <p>
        Imported: {{ mutation.data.value!.imported }} — Skipped (duplicate):
        {{ mutation.data.value!.skippedDuplicate }} — Skipped (conflict):
        {{ mutation.data.value!.skippedConflict }}
      </p>
      <ul v-if="mutation.data.value!.conflictDetails.length > 0">
        <li v-for="(detail, i) in mutation.data.value!.conflictDetails" :key="i">
          {{ detail }}
        </li>
      </ul>
    </div>

    <p v-if="mutation.isError.value" class="submit-error">Import failed. Please try again.</p>
  </div>
</template>

<style scoped>
.import-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.import-result {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 12px 14px;
  font-size: 13px;
}

.import-result ul {
  margin: 8px 0 0;
  padding-left: 18px;
}

.submit-error {
  background: var(--danger-bg);
  color: var(--danger);
  padding: 10px 12px;
  border-radius: var(--radius);
  font-size: 13px;
  margin: 0;
}
</style>
