import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#6750a4',
    },
    secondary: {
      main: '#625b71',
    },
    background: {
      default: '#f6f5fb',
    },
  },
  shape: {
    borderRadius: 12,
  },
  typography: {
    fontFamily: [
      'Inter',
      '-apple-system',
      'BlinkMacSystemFont',
      '"Segoe UI"',
      'Roboto',
      '"Helvetica Neue"',
      'Arial',
      'sans-serif',
    ].join(','),
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
  },
});
