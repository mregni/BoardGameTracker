import {Menu, MenuProps} from 'antd';
import {TFunction} from 'i18next';
import {Link} from 'react-router-dom';

import Icon, {GlobalOutlined, UserOutlined} from '@ant-design/icons';

import {ReactComponent as DiceIcon} from '../../../assets/icons/dice.svg';

type MenuItem = Required<MenuProps>['items'][number];

function getItem(
  label: string,
  key: React.Key,
  path: string,
  icon?: React.ReactNode,
  children?: MenuItem[],
): MenuItem {
  return {
    key,
    icon,
    children,
    label: <Link to={path}>{label}</Link>,
  } as MenuItem;
}

export const getMenuItems = (t: TFunction<"translation", undefined>): MenuItem[] =>{
  return [
    getItem(t('common.games'), '0', "games", <Icon component={DiceIcon} />),
    getItem(t('common.players'), '1', "players", <UserOutlined />),
    getItem(t('common.locations'), '2', "locations", <GlobalOutlined />),
  ];
} 
