import { AppBar, Box, Button, Container, CssBaseline, Stack, Toolbar, Typography } from '@mui/material';
import { ThemeProvider } from '@mui/material/styles';
import GroupsIcon from '@mui/icons-material/Groups';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
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
            <Stack direction="row" spacing={1}>
              <Button
                color="inherit"
                size="small"
                href="http://localhost:15672"
                target="_blank"
                rel="noopener noreferrer"
                endIcon={<OpenInNewIcon fontSize="small" />}
              >
                RabbitMQ
              </Button>
              <Button
                color="inherit"
                size="small"
                href="http://localhost:5601"
                target="_blank"
                rel="noopener noreferrer"
                endIcon={<OpenInNewIcon fontSize="small" />}
              >
                Kibana
              </Button>
            </Stack>
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
