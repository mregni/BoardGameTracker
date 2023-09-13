import {Layout, Space} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';

import {GcSmallMenu} from '../GcMenu/GcSmallMenu';

export const GcHeader = () => {
  const { Header } = Layout;
  const screens = useBreakpoint();

  return (
    <Header style={{ display: 'flex', alignItems: 'center', paddingInline: screens.lg ? 20 : 10 }}>
      <Space>
        {!screens.lg && (<GcSmallMenu />)}
        Test header
      </Space>
    </Header>
  )
}