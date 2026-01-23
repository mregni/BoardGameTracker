import { MouseEventHandler } from 'react';

export interface Actions {
  variant?: 'primary' | 'error' | 'cancel' | 'text';
  onClick: MouseEventHandler<HTMLButtonElement>;
  content: string | React.ReactNode;
  smallContent?: string | React.ReactNode;
}
