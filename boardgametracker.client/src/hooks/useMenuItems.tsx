import { MenuItem } from '../models';

import UsersIcon from '@/assets/icons/users.svg?react';
import PuzzlePieceIcon from '@/assets/icons/puzzle-piece.svg?react';
import MapPinIcon from '@/assets/icons/map-pin.svg?react';
import HomeIcon from '@/assets/icons/home.svg?react';
import CogIcon from '@/assets/icons/cog.svg?react';

const menuItems: MenuItem[] = [
  { menuLabel: 'common.dashboard', pageLabel: 'common.dashboard', path: 'home', icon: <HomeIcon className="size-5" /> },
  { menuLabel: 'common.games', pageLabel: 'games.title', path: 'games', icon: <PuzzlePieceIcon className="size-5" /> },
  { menuLabel: 'common.players', pageLabel: 'common.players', path: 'players', icon: <UsersIcon className="size-5" /> },
  {
    menuLabel: 'common.locations',
    pageLabel: 'common.locations',
    path: 'locations',
    icon: <MapPinIcon className="size-5" />,
  },
  {
    menuLabel: 'common.settings',
    pageLabel: 'common.settings',
    path: 'settings',
    icon: <CogIcon className="size-5" />,
  },
];

export interface MenuItems {
  menuItems: MenuItem[];
}

export const useMenuItems = (): MenuItems => {
  return {
    menuItems,
  };
};
