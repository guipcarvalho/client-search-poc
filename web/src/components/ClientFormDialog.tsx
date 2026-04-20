import { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material';
import type { Client, CreateClientInput } from '../api/clients';

interface ClientFormDialogProps {
  open: boolean;
  initialValue?: Client | null;
  onClose: () => void;
  onSubmit: (input: CreateClientInput) => Promise<void> | void;
}

const emptyForm: CreateClientInput = {
  name: '',
  email: '',
  document: '',
  phone: '',
};

export function ClientFormDialog({ open, initialValue, onClose, onSubmit }: ClientFormDialogProps) {
  const [form, setForm] = useState<CreateClientInput>(emptyForm);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open) {
      setForm(
        initialValue
          ? {
              name: initialValue.name,
              email: initialValue.email,
              document: initialValue.document,
              phone: initialValue.phone ?? '',
            }
          : emptyForm,
      );
    }
  }, [open, initialValue]);

  const handleChange =
    (field: keyof CreateClientInput) => (event: React.ChangeEvent<HTMLInputElement>) => {
      setForm((prev) => ({ ...prev, [field]: event.target.value }));
    };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      await onSubmit({ ...form, phone: form.phone?.trim() || null });
      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{initialValue ? 'Edit client' : 'New client'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
          <TextField
            label="Name"
            value={form.name}
            onChange={handleChange('name')}
            fullWidth
            autoFocus
            required
          />
          <TextField
            label="Email"
            type="email"
            value={form.email}
            onChange={handleChange('email')}
            fullWidth
            required
          />
          <TextField
            label="Document"
            value={form.document}
            onChange={handleChange('document')}
            fullWidth
            required
          />
          <TextField
            label="Phone"
            value={form.phone ?? ''}
            onChange={handleChange('phone')}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={submitting}>
          Cancel
        </Button>
        <Button variant="contained" onClick={handleSubmit} disabled={submitting}>
          {initialValue ? 'Save' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
