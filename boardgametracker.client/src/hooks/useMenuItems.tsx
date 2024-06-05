import { Cog8ToothIcon, HomeIcon, InformationCircleIcon, MapPinIcon, PuzzlePieceIcon, UserGroupIcon } from '@heroicons/react/24/outline';

import { MenuItem } from '../models';

const menuItems: MenuItem[] = [
  { menuLabel: 'common.dashboard', pageLabel: 'common.dashboard', path: 'home', icon: <HomeIcon /> },
  { menuLabel: 'common.games', pageLabel: 'games.title', path: 'games', icon: <PuzzlePieceIcon /> },
  { menuLabel: 'common.players', pageLabel: 'common.players', path: 'players', icon: <UserGroupIcon /> },
  { menuLabel: 'common.locations', pageLabel: 'common.locations', path: 'locations', icon: <MapPinIcon /> },
  { menuLabel: 'common.settings', pageLabel: 'common.settings', path: 'settings', icon: <Cog8ToothIcon /> },
  { menuLabel: 'Info for nerds', pageLabel: 'Info for nerds', path: 'info', icon: <InformationCircleIcon /> },
];

const bottomMenuItems: MenuItem[] = [
  { menuLabel: 'common.info', pageLabel: 'common.info', path: 'info', icon: <InformationCircleIcon /> },
  { menuLabel: 'common.settings', pageLabel: 'common.settings', path: 'settings', icon: <Cog8ToothIcon /> },
];

export interface MenuItems {
  menuItems: MenuItem[];
  bottomMenuItems: MenuItem[];
}

export const useMenuItems = (): MenuItems => {
  return {
    menuItems,
    bottomMenuItems,
  };
};
