import {Layout, Menu} from 'antd';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useLocation} from 'react-router-dom';

import {getMenuItems} from './MenuItems';

export const GcMenu = () => {
  const { Sider } = Layout;
  const location = useLocation();
  const {t} = useTranslation();
  const [collapsed, setCollapsed] = useState(false);
  const result = getMenuItems(t).find(item => location.pathname.startsWith(`/${item.label.props.to}`));
  
  return (
    <Sider collapsible collapsed={collapsed} theme="light" onCollapse={(value) => setCollapsed(value)}>
      <Menu defaultSelectedKeys={[result?.key?.toString() ?? "0"]} mode="inline" items={getMenuItems(t)} />
    </Sider>
  )
}
