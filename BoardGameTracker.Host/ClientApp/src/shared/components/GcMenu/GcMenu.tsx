import {Layout, Menu, theme} from 'antd';
import {useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useLocation} from 'react-router-dom';

import {getMenuItems} from './MenuItems';

export const GcMenu = () => {
  const { Sider } = Layout;
  const location = useLocation();
  const { t } = useTranslation();
  const [collapsed, setCollapsed] = useState(false);
  const [openPage, setOpenPage] = useState("0");

  useEffect(() => {
    const result = getMenuItems(t).find(item => location.pathname.startsWith(`/${item.label.props.to}`));
    setOpenPage(result?.key?.toString() ?? "0");
  }, [location.pathname, t]);
  
  const { token: { colorBgContainer } } = theme.useToken();

  return (
    <Sider
      style={{ background: colorBgContainer }}
      collapsible
      collapsed={collapsed}
      onCollapse={(value) => setCollapsed(value)}>
      <Menu
        style={{ height: '100%' }}
        selectedKeys={[openPage]}
        mode="inline"
        items={getMenuItems(t)} />
    </Sider>
  )
}
