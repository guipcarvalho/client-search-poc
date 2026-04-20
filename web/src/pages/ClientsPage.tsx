import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  InputAdornment,
  Paper,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import SearchIcon from '@mui/icons-material/Search';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/DeleteOutlined';
import RefreshIcon from '@mui/icons-material/Refresh';
import { clientsApi, type Client, type CreateClientInput } from '../api/clients';
import { ClientFormDialog } from '../components/ClientFormDialog';

export function ClientsPage() {
  const [clients, setClients] = useState<Client[]>([]);
  const [loading, setLoading] = useState(false);
  const [query, setQuery] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Client | null>(null);

  const isSearching = useMemo(() => query.trim().length > 0, [query]);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = isSearching ? await clientsApi.search(query.trim()) : await clientsApi.list();
      setClients(data);
    } catch (err) {
      console.error(err);
      setError('Failed to load clients. Is the API running?');
    } finally {
      setLoading(false);
    }
  }, [query, isSearching]);

  useEffect(() => {
    const handle = window.setTimeout(load, isSearching ? 300 : 0);
    return () => window.clearTimeout(handle);
  }, [load, isSearching]);

  const handleCreate = () => {
    setEditing(null);
    setFormOpen(true);
  };

  const handleEdit = (client: Client) => {
    setEditing(client);
    setFormOpen(true);
  };

  const handleSubmit = async (input: CreateClientInput) => {
    try {
      if (editing) {
        await clientsApi.update(editing.id, input);
      } else {
        await clientsApi.create(input);
      }
      await load();
    } catch (err) {
      console.error(err);
      setError(editing ? 'Failed to update client' : 'Failed to create client');
    }
  };

  const handleDelete = async (client: Client) => {
    if (!window.confirm(`Delete ${client.name}?`)) return;
    try {
      await clientsApi.remove(client.id);
      await load();
    } catch (err) {
      console.error(err);
      setError('Failed to delete client');
    }
  };

  return (
    <Box sx={{ py: 4, px: { xs: 2, md: 4 } }}>
      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', md: 'row' },
          gap: 2,
          alignItems: { md: 'center' },
          justifyContent: 'space-between',
          mb: 3,
        }}
      >
        <Box>
          <Typography variant="h5">Clients</Typography>
          <Typography variant="body2" color="text.secondary">
            {isSearching ? 'Searching via Elasticsearch' : 'Recent clients from Postgres'}
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Search name, email, document…"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" />
                  </InputAdornment>
                ),
              },
            }}
            sx={{ minWidth: 280 }}
          />
          <Tooltip title="Refresh">
            <IconButton onClick={load} disabled={loading}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
          <Button variant="contained" startIcon={<AddIcon />} onClick={handleCreate}>
            New client
          </Button>
        </Box>
      </Box>

      <Paper elevation={0} variant="outlined" sx={{ overflow: 'hidden' }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Document</TableCell>
                <TableCell>Phone</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 6 }}>
                    <CircularProgress size={24} />
                  </TableCell>
                </TableRow>
              )}

              {!loading && clients.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 6 }}>
                    <Typography color="text.secondary">
                      {isSearching ? 'No matches.' : 'No clients yet. Create the first one.'}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}

              {!loading &&
                clients.map((client) => (
                  <TableRow key={client.id} hover>
                    <TableCell>
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                        <Typography sx={{ fontWeight: 500 }}>{client.name}</Typography>
                        {client.updatedAt && <Chip size="small" label="updated" />}
                      </Box>
                    </TableCell>
                    <TableCell>{client.email}</TableCell>
                    <TableCell>{client.document}</TableCell>
                    <TableCell>{client.phone ?? '—'}</TableCell>
                    <TableCell>{new Date(client.createdAt).toLocaleString()}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="Edit">
                        <IconButton size="small" onClick={() => handleEdit(client)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete">
                        <IconButton size="small" onClick={() => handleDelete(client)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <ClientFormDialog
        open={formOpen}
        initialValue={editing}
        onClose={() => setFormOpen(false)}
        onSubmit={handleSubmit}
      />

      <Snackbar
        open={!!error}
        autoHideDuration={5000}
        onClose={() => setError(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="error" onClose={() => setError(null)} variant="filled">
          {error}
        </Alert>
      </Snackbar>
    </Box>
  );
}
