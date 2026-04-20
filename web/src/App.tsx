import { AppBar, Box, Container, CssBaseline, Toolbar, Typography } from '@mui/material';
import { ThemeProvider } from '@mui/material/styles';
import GroupsIcon from '@mui/icons-material/Groups';
import { theme } from './theme/theme';
import { ClientsPage } from './pages/ClientsPage';

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
        <AppBar position="static" color="primary" elevation={0}>
          <Toolbar>
            <GroupsIcon sx={{ mr: 1 }} />
            <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
              Client Search
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.8 }}>
              .NET 10 · Postgres · Elasticsearch · RabbitMQ
            </Typography>
          </Toolbar>
        </AppBar>
        <Container maxWidth="lg" disableGutters>
          <ClientsPage />
        </Container>
      </Box>
    </ThemeProvider>
  );
}

export default App;
