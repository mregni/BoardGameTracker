import { Badge } from '../';

export interface Player {
  id: string;
  name: string;
  image: string | null;
  badges: Badge[];
}