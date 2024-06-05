import { useLocation } from 'react-router-dom';

import { MenuItem } from '../models';

import { useMenuItems } from './useMenuItems';

export interface Props {
  activePage: string;
  pageTitle: string;
}

const extractPathName = (pathname: string): string => {
  const parts = pathname.split('/');
  const secondPart = parts[1];
  return secondPart ? secondPart : 'home';
};

const getPageTitle = (items: MenuItem[], currentPath: string): string => {
  const index = items.findIndex((x: MenuItem) => x.path === extractPathName(currentPath));
  if (index !== -1) {
    return items[index].pageLabel;
  }

  return '';
};

export const usePage = (): Props => {
  const location = useLocation();
  const { menuItems } = useMenuItems();

  const activePagePath = extractPathName(location.pathname);
  const pageTitle = getPageTitle(menuItems, location.pathname);

  return {
    activePage: activePagePath,
    pageTitle,
  };
};
