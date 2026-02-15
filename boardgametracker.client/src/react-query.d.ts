import '@tanstack/react-query';

import { ApiError } from '@/models';

declare module '@tanstack/react-query' {
  interface Register {
    defaultError: ApiError;
  }
}
