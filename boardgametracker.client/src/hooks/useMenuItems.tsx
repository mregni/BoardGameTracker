import {
  Cog8ToothIcon, HomeIcon, InformationCircleIcon, MapPinIcon, PuzzlePieceIcon, UserGroupIcon,
} from '@heroicons/react/24/outline';

import {MenuItem} from '../models';

const menuItems: MenuItem[] = [
  { label: 'common.dashboard', path: 'home', icon: <HomeIcon /> },
  { label: 'common.games', path: 'games', icon: <PuzzlePieceIcon /> },
  { label: 'common.players', path: 'players', icon: <UserGroupIcon /> },
  { label: 'common.locations', path: 'locations', icon: <MapPinIcon /> },
  { label: 'common.settings', path: 'settings', icon: <Cog8ToothIcon /> },
  { label: 'Info for nerds', path: 'info', icon: <InformationCircleIcon /> },
]

const bottomMenuItems: MenuItem[] = [
  { label: 'common.info', path: 'info', icon: <InformationCircleIcon /> },
  { label: 'common.settings', path: 'settings', icon: <Cog8ToothIcon /> },
]

export interface MenuItems {
  menuItems: MenuItem[],
  bottomMenuItems: MenuItem[]
}

export const useMenuItems = (): MenuItems => {
  return {
    menuItems,
    bottomMenuItems
  }
}

