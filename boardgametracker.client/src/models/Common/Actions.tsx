import { MouseEventHandler } from 'react';

export interface Actions {
  variant?: 'ghost' | 'classic' | 'solid' | 'soft' | 'surface' | 'outline';
  onClick: MouseEventHandler<HTMLButtonElement>;
  content: string;
}
