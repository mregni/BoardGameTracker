import { FunctionComponent, SVGProps } from 'react';

export interface MenuItem {
  menuLabel: string;
  path: string;
  icon: FunctionComponent<SVGProps<SVGSVGElement>>;
  mobileVisible: boolean;
}
