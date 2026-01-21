import { useQueries } from '@tanstack/react-query';

import { getEnvironment, getVersionInfo } from '@/services/queries/settings';
import { getCounts } from '@/services/queries/count';
import { MenuItem } from '@/models';
import UsersIcon from '@/assets/icons/users.svg?react';
import TrendUp from '@/assets/icons/trend-up.svg?react';
import PuzzlePieceIcon from '@/assets/icons/puzzle-piece.svg?react';
import PlusIcon from '@/assets/icons/plus.svg?react';
import MapPinIcon from '@/assets/icons/map-pin.svg?react';
import LeftRightArrowIcon from '@/assets/icons/left-right-arrow.svg?react';
import HomeIcon from '@/assets/icons/home.svg?react';
import CogIcon from '@/assets/icons/cog.svg?react';

export interface MenuItems {
  menuItems: MenuItem[];
}

export const menuItems: MenuItem[] = [
  { menuLabel: 'common.dashboard', path: '/', icon: HomeIcon, mobileVisible: true },
  { menuLabel: 'common.new-session', path: '/sessions/new', icon: PlusIcon, mobileVisible: true },
  { menuLabel: 'common.games', path: '/games', icon: PuzzlePieceIcon, mobileVisible: true },
  { menuLabel: 'common.players', path: '/players', icon: UsersIcon, mobileVisible: true },
  { menuLabel: 'common.compare', path: '/compare', icon: TrendUp, mobileVisible: false },
  { menuLabel: 'common.loans', path: '/loans', icon: LeftRightArrowIcon, mobileVisible: false },
  { menuLabel: 'common.locations', path: '/locations', icon: MapPinIcon, mobileVisible: false },
  { menuLabel: 'common.settings', path: '/settings', icon: CogIcon, mobileVisible: false },
];

export const useBgtMenuBar = () => {
  const [versionInfoQuery, countsQuery] = useQueries({
    queries: [getVersionInfo(), getCounts()],
  });

  return {
    versionInfo: versionInfoQuery.data,
    counts: countsQuery.data,
    menuItems,
  };
};
