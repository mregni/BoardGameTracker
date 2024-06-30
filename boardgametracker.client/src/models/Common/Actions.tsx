import { MouseEventHandler } from 'react';

export interface Actions {
  variant?: 'solid' | 'outline' | undefined;
  onClick: MouseEventHandler<HTMLButtonElement>;
  content: string;
}
