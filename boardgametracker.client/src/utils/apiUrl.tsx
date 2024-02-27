let baseUrl = '/api/';
if (process.env.NODE_ENV === 'development') {
  baseUrl = 'http://localhost:6554/api/';
}

export const apiUrl = baseUrl