import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getEnvironment } from '@/services/queries/settings';
import { getCounts } from '@/services/queries/count';
import { MenuItem } from '@/models';
import UsersIcon from '@/assets/icons/users.svg?react';
import PuzzlePieceIcon from '@/assets/icons/puzzle-piece.svg?react';
import MapPinIcon from '@/assets/icons/map-pin.svg?react';
import LeftRightArrowIcon from '@/assets/icons/left-right-arrow.svg?react';
import HomeIcon from '@/assets/icons/home.svg?react';
import CogIcon from '@/assets/icons/cog.svg?react';

export interface MenuItems {
  menuItems: MenuItem[];
}

export const menuItems: MenuItem[] = [
  { menuLabel: 'common.dashboard', path: '/', icon: HomeIcon },
  { menuLabel: 'common.games', path: '/games', icon: PuzzlePieceIcon },
  { menuLabel: 'common.players', path: '/players', icon: UsersIcon },
  { menuLabel: 'common.compare', path: '/compare', icon: LeftRightArrowIcon },
  { menuLabel: 'common.loans', path: '/loans', icon: LeftRightArrowIcon },
  { menuLabel: 'common.locations', path: '/locations', icon: MapPinIcon },
  { menuLabel: 'common.settings', path: '/settings', icon: CogIcon },
];

export const useBgtMenuBar = () => {
  const [environmentQuery, countsQuery] = useQueries({
    queries: [getEnvironment(), getCounts()],
  });

  const environment = useMemo(() => environmentQuery.data, [environmentQuery.data]);
  const counts = useMemo(() => countsQuery.data, [countsQuery.data]);

  return {
    environment,
    counts,
    menuItems,
  };
};
