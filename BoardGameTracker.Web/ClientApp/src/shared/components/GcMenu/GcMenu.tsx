import {Layout, Menu, theme} from 'antd';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useLocation} from 'react-router-dom';

import {getMenuItems} from './MenuItems';

export const GcMenu = () => {
  const { Sider } = Layout;
  const location = useLocation();
  const { t } = useTranslation();
  const [collapsed, setCollapsed] = useState(false);
  const result = getMenuItems(t).find(item => location.pathname.startsWith(`/${item.label.props.to}`));
  const { token: { colorBgContainer } } = theme.useToken();

  return (
    <Sider
      style={{ background: colorBgContainer }}
      collapsible
      collapsed={collapsed}
      onCollapse={(value) => setCollapsed(value)}>
      <Menu
        style={{ height: '100%' }}
        defaultSelectedKeys={[result?.key?.toString() ?? "0"]}
        mode="inline"
        items={getMenuItems(t)} />
    </Sider>
  )
}
